using CCCInventory.Data;
using CCCInventory.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CCCInventory.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class WeddingContractController : ControllerBase
    {
        private readonly DataContext _context;
        private readonly WeddingContractService _contracts;
        private readonly AuditService _audit;
        private readonly ILogger<WeddingContractController> _logger;
        private readonly string _attachmentsRoot;

        public WeddingContractController(DataContext context, WeddingContractService contracts,
            AuditService audit, ILogger<WeddingContractController> logger, IWebHostEnvironment env)
        {
            _context = context;
            _contracts = contracts;
            _audit = audit;
            _logger = logger;
            _attachmentsRoot = AttachmentController.AttachmentsRoot(env);
        }

        public class GenerateRequest
        {
            // "overwrite" (replace the current contract) or "revision" (add a REVISED copy).
            // Ignored when the order has no contract yet.
            public string? Mode { get; set; }
        }

        [HttpPost("{orderNumber:int}")]
        public async Task<IActionResult> Generate(int orderNumber, [FromBody] GenerateRequest request, CancellationToken ct)
        {
            var order = await _context.Orders
                .Include(o => o.Cakes)
                .Include(o => o.Cupcakes)
                .Include(o => o.WeddingDetails)
                .FirstOrDefaultAsync(o => o.OrderNumber == orderNumber, ct);

            if (order == null)
                return NotFound(new { message = $"Order {orderNumber} not found" });
            if (!order.IsWedding)
                return BadRequest(new { message = "Only wedding orders have a contract." });

            var hasContract = !string.IsNullOrEmpty(order.WeddingDetails?.ContractDocId);
            var mode = !hasContract ? "new" : request.Mode?.ToLowerInvariant();
            if (mode is not ("new" or "overwrite" or "revision"))
                return BadRequest(new { message = "Choose whether to overwrite the current contract or create a revision." });

            ContractResult result;
            try
            {
                result = await _contracts.GenerateAsync(order, mode, await LoadPhotosAsync(order, ct), ct);
            }
            catch (ContractException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (Google.GoogleApiException ex)
            {
                _logger.LogError(ex, "Google API error generating contract for order {OrderNumber}", orderNumber);
                return BadRequest(new { message = $"Google API error: {ex.Message}" });
            }

            if (order.WeddingDetails == null)
            {
                order.WeddingDetails = new WeddingDetails { OrderNumber = orderNumber };
            }
            order.WeddingDetails.ContractDocId = result.DocId;
            order.WeddingDetails.ContractDocUrl = result.Url;
            order.WeddingDetails.ContractDocName = result.Name;
            order.WeddingDetails.ContractGeneratedAt = DateTime.Now;

            _audit.PrepareLog(await GetStaffMemberIdAsync(), "Order", orderNumber, "Contract",
                $"{(mode == "new" ? "Created" : mode == "overwrite" ? "Overwrote" : "Revised")}: {result.Name}");
            await _context.SaveChangesAsync(ct);

            return Ok(new
            {
                docId = result.DocId,
                url = result.Url,
                name = result.Name,
                generatedAt = order.WeddingDetails.ContractGeneratedAt,
                warnings = result.Warnings
            });
        }

        // The values each template token would receive for this order (no Google calls).
        [HttpGet("{orderNumber:int}/preview")]
        public async Task<IActionResult> Preview(int orderNumber, CancellationToken ct)
        {
            var order = await _context.Orders
                .Include(o => o.Cakes)
                .Include(o => o.Cupcakes)
                .Include(o => o.WeddingDetails)
                .FirstOrDefaultAsync(o => o.OrderNumber == orderNumber, ct);
            if (order == null)
                return NotFound(new { message = $"Order {orderNumber} not found" });

            return Ok(new
            {
                fileName = WeddingContractService.BuildFileName(order, revised: false),
                values = WeddingContractService.BuildValues(order),
                rows = WeddingContractService.BuildRows(order),
                photos = (await LoadPhotosAsync(order, ct)).ToDictionary(p => p.Token, p => p.FileName)
            });
        }

        // The order's chosen inspiration photos (attachments of this order only)
        private async Task<List<ContractPhoto>> LoadPhotosAsync(Order order, CancellationToken ct)
        {
            var w = order.WeddingDetails;
            var chosen = new Dictionary<string, int?>
            {
                ["cake_photo"] = w?.CakePhotoAttachmentId,
                ["cupcake_photo"] = w?.CupcakePhotoAttachmentId
            };
            var ids = chosen.Values.OfType<int>().ToList();
            if (ids.Count == 0) return [];

            var attachments = await _context.OrderAttachments
                .Where(a => a.OrderNumber == order.OrderNumber && ids.Contains(a.Id))
                .ToDictionaryAsync(a => a.Id, ct);
            var photos = new List<ContractPhoto>();
            foreach (var (token, id) in chosen)
            {
                if (id is int i && attachments.TryGetValue(i, out var a))
                    photos.Add(new ContractPhoto(token, AttachmentController.FilePath(_attachmentsRoot, a), a.ContentType, a.FileName));
            }
            return photos;
        }

        private async Task<int?> GetStaffMemberIdAsync()
        {
            var header = Request.Headers["X-Staff-Member-Id"].FirstOrDefault();
            if (!int.TryParse(header, out var id)) return null;
            var exists = await _context.StaffMembers.AnyAsync(s => s.Id == id && s.IsActive);
            return exists ? id : null;
        }
    }
}
