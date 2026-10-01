using CCCInventory.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CCCInventory.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class AttachmentController : ControllerBase
    {
        private readonly DataContext _context;
        private readonly string _attachmentsRoot;

        private static readonly string[] AllowedContentTypes =
        [
            "image/jpeg", "image/png", "image/gif", "image/webp", "image/heic",
            "application/pdf"
        ];

        public AttachmentController(DataContext context, IWebHostEnvironment env)
        {
            _context = context;
            _attachmentsRoot = AttachmentsRoot(env);
        }

        public static string AttachmentsRoot(IWebHostEnvironment env) => Path.Combine(env.ContentRootPath, "attachments");

        public static string FilePath(string attachmentsRoot, OrderAttachment attachment) =>
            Path.Combine(attachmentsRoot, attachment.OrderNumber.ToString(), attachment.StoredFileName);

        // GET /api/attachment/{orderNumber}
        [HttpGet("{orderNumber:int}")]
        public async Task<ActionResult<List<OrderAttachment>>> GetAttachments(int orderNumber)
        {
            return Ok(await _context.OrderAttachments
                .Where(a => a.OrderNumber == orderNumber)
                .OrderBy(a => a.UploadedAt)
                .ToListAsync());
        }

        // POST /api/attachment/{orderNumber}
        [HttpPost("{orderNumber:int}")]
        public async Task<ActionResult<List<OrderAttachment>>> UploadAttachments(int orderNumber, List<IFormFile> files)
        {
            if (files == null || files.Count == 0)
                return BadRequest("No files provided.");

            var orderExists = await _context.Orders.AnyAsync(o => o.OrderNumber == orderNumber);
            if (!orderExists)
                return NotFound($"Order {orderNumber} not found.");

            var folder = Path.Combine(_attachmentsRoot, orderNumber.ToString());
            Directory.CreateDirectory(folder);

            var results = new List<OrderAttachment>();
            foreach (var file in files)
            {
                if (!AllowedContentTypes.Contains(file.ContentType))
                    return BadRequest($"File type '{file.ContentType}' is not allowed. Only images and PDFs are accepted.");

                var storedFileName = $"{Guid.NewGuid()}{Path.GetExtension(file.FileName)}";
                var filePath = Path.Combine(folder, storedFileName);

                await using var stream = System.IO.File.Create(filePath);
                await file.CopyToAsync(stream);

                var attachment = new OrderAttachment
                {
                    OrderNumber = orderNumber,
                    FileName = file.FileName,
                    StoredFileName = storedFileName,
                    ContentType = file.ContentType,
                    UploadedAt = DateTime.UtcNow
                };
                _context.OrderAttachments.Add(attachment);
                results.Add(attachment);
            }

            await _context.SaveChangesAsync();
            return Ok(results);
        }

        // DELETE /api/attachment/{id}
        [HttpDelete("{id:int}")]
        public async Task<ActionResult> DeleteAttachment(int id)
        {
            var attachment = await _context.OrderAttachments.FindAsync(id);
            if (attachment == null)
                return NotFound();

            var filePath = FilePath(_attachmentsRoot, attachment);
            if (System.IO.File.Exists(filePath))
                System.IO.File.Delete(filePath);

            _context.OrderAttachments.Remove(attachment);

            // An attachment chosen as a contract inspiration photo is no longer chosen
            var details = await _context.WeddingDetails
                .Where(w => w.CakePhotoAttachmentId == id || w.CupcakePhotoAttachmentId == id)
                .ToListAsync();
            foreach (var w in details)
            {
                if (w.CakePhotoAttachmentId == id) w.CakePhotoAttachmentId = null;
                if (w.CupcakePhotoAttachmentId == id) w.CupcakePhotoAttachmentId = null;
            }

            await _context.SaveChangesAsync();
            return NoContent();
        }

        // GET /api/attachment/{id}/file
        [HttpGet("{id:int}/file")]
        public async Task<ActionResult> GetFile(int id)
        {
            var attachment = await _context.OrderAttachments.FindAsync(id);
            if (attachment == null)
                return NotFound();

            var filePath = FilePath(_attachmentsRoot, attachment);
            if (!System.IO.File.Exists(filePath))
                return NotFound("File not found on disk.");

            var stream = System.IO.File.OpenRead(filePath);
            return File(stream, attachment.ContentType, attachment.FileName);
        }
    }
}
