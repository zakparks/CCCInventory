using CCCInventory.Data;
using CCCInventory.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Reflection;

namespace CCCInventory.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class OrderController : ControllerBase
    {
        private readonly DataContext _context;
        private readonly AuditService _audit;

        public OrderController(DataContext context, AuditService audit)
        {
            _context = context;
            _audit = audit;
        }

        private async Task<int?> GetStaffMemberIdAsync()
        {
            var header = Request.Headers["X-Staff-Member-Id"].FirstOrDefault();
            if (!int.TryParse(header, out var id)) return null;
            var exists = await _context.StaffMembers.AnyAsync(s => s.Id == id && s.IsActive);
            return exists ? id : null;
        }

        [HttpGet]
        public async Task<ActionResult<List<Order>>> GetOrders([FromQuery] string? status)
        {
            var now = DateTime.Now;
            var query = _context.Orders
                .Include(o => o.Cakes)
                .Include(o => o.Cupcakes)
                .Include(o => o.Cookies)
                .Include(o => o.Pupcakes)
                .Include(o => o.OtherItems)
                .AsQueryable();

            query = (status?.ToLower()) switch
            {
                "cancelled"     => query.Where(o => o.CancelledFlag && o.OrderDateTime > now),
                "archived"      => query.Where(o => o.OrderDateTime <= now),
                "readyforpickup"=> query.Where(o => o.IsReadyForPickup && !o.CancelledFlag && o.OrderDateTime > now),
                "incomplete"    => query.Where(o => !o.CancelledFlag
                                        && (o.OrderDateTime == null || o.OrderDateTime > now)
                                        && (
                                            string.IsNullOrEmpty(o.Title) ||
                                            string.IsNullOrEmpty(o.CustName) ||
                                            (string.IsNullOrEmpty(o.CustPhone) && string.IsNullOrEmpty(o.CustEmail)) ||
                                            string.IsNullOrEmpty(o.OrderType) ||
                                            o.OrderDateTime == null ||
                                            (!o.Cakes!.Any() && !o.Cupcakes!.Any() && !o.Cookies!.Any()
                                                && !o.Pupcakes!.Any() && !o.OtherItems!.Any()) ||
                                            o.Cakes!.Any(c => c.TierSize == null || c.TierSize == "" ||
                                                c.NumTierLayers == 0 ||
                                                c.CakeShape == null || c.CakeShape == "" ||
                                                c.CakeFlavor == null || c.CakeFlavor == "" ||
                                                c.IcingFlavor == null || c.IcingFlavor == "") ||
                                            o.Cupcakes!.Any(c => c.CupcakeSize == null || c.CupcakeSize == "" ||
                                                c.CupcakeQuantity == 0 ||
                                                c.CupcakeFlavor == null || c.CupcakeFlavor == "" ||
                                                c.IcingFlavor == null || c.IcingFlavor == "") ||
                                            o.Pupcakes!.Any(p => p.PupcakeQuantity == 0) ||
                                            o.Cookies!.Any(c => c.CookieQuantity == 0)
                                        )),
                _               => query.Where(o => !o.CancelledFlag && o.OrderDateTime > now)
            };

            return Ok(await query.ToListAsync());
        }

        [HttpGet("{orderNumber:int}")]
        public async Task<ActionResult<Order>> GetOrder(int orderNumber)
        {
            var order = await _context.Orders
                .Include(o => o.Cakes)
                .Include(o => o.Cupcakes)
                .Include(o => o.Cookies)
                .Include(o => o.Pupcakes)
                .Include(o => o.OtherItems)
                .FirstOrDefaultAsync(o => o.OrderNumber == orderNumber);

            if (order == null)
                return NotFound($"Order {orderNumber} not found");

            return Ok(order);
        }

        [HttpGet("{orderNumber:int}/created-by")]
        public async Task<IActionResult> GetCreatedBy(int orderNumber)
        {
            var log = await _context.AuditLogs
                .Include(a => a.StaffMember)
                .Where(a => a.EntityType == "Order" && a.EntityId == orderNumber && a.Action == "Create")
                .OrderBy(a => a.Timestamp)
                .FirstOrDefaultAsync();

            return Ok(new { staffName = log?.StaffMember?.Name });
        }

        [HttpGet("newOrderNumber")]
        public async Task<ActionResult<int>> GetNewOrderNumber()
        {
            int? max = await _context.Orders.MaxAsync(order => (int?)order.OrderNumber);
            return Ok((max ?? 0) + 1);
        }

        [HttpPost]
        public async Task<ActionResult<int>> CreateOrder(Order order)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            _context.Orders.Add(order);
            await _context.SaveChangesAsync(); // EF assigns OrderNumber here

            _audit.PrepareLog(await GetStaffMemberIdAsync(), "Order", order.OrderNumber, "Create");
            await _context.SaveChangesAsync();

            return Ok(order.OrderNumber);
        }

        [HttpPut]
        public async Task<ActionResult<int>> UpdateOrder(Order order)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var dbOrder = await _context.Orders
                .FirstOrDefaultAsync(o => o.OrderNumber == order.OrderNumber);

            if (dbOrder == null)
                return NotFound($"Order {order.OrderNumber} not found");

            using var transaction = await _context.Database.BeginTransactionAsync();

            foreach (PropertyInfo property in typeof(Order).GetProperties()
                .Where(p => p.CanWrite && !IsNavigationCollection(p)))
            {
                property.SetValue(dbOrder, property.GetValue(order, null), null);
            }

            int oNum = order.OrderNumber;
            await _context.Cakes      .Where(c => EF.Property<int?>(c, "OrderNumber") == oNum).ExecuteDeleteAsync();
            await _context.Cupcakes   .Where(c => EF.Property<int?>(c, "OrderNumber") == oNum).ExecuteDeleteAsync();
            await _context.Cookies    .Where(c => EF.Property<int?>(c, "OrderNumber") == oNum).ExecuteDeleteAsync();
            await _context.Pupcakes   .Where(c => EF.Property<int?>(c, "OrderNumber") == oNum).ExecuteDeleteAsync();
            await _context.OtherItems .Where(c => EF.Property<int?>(c, "OrderNumber") == oNum).ExecuteDeleteAsync();

            dbOrder.Cakes      = order.Cakes?      .Select(c => { c.Id = 0; return c; }).ToList();
            dbOrder.Cupcakes   = order.Cupcakes?   .Select(c => { c.Id = 0; return c; }).ToList();
            dbOrder.Cookies    = order.Cookies?    .Select(c => { c.Id = 0; return c; }).ToList();
            dbOrder.Pupcakes   = order.Pupcakes?   .Select(c => { c.Id = 0; return c; }).ToList();
            dbOrder.OtherItems = order.OtherItems? .Select(c => { c.Id = 0; return c; }).ToList();

            _audit.PrepareLog(await GetStaffMemberIdAsync(), "Order", order.OrderNumber, "Update");
            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            return Ok(order.OrderNumber);
        }

        [HttpPut("{orderNumber:int}/cancel")]
        public async Task<ActionResult<int>> CancelOrder(int orderNumber, [FromBody] CancelOrderRequest request)
        {
            var dbOrder = await _context.Orders.FindAsync(orderNumber);
            if (dbOrder == null)
                return NotFound($"Order {orderNumber} not found");

            dbOrder.CancelledFlag = true;
            dbOrder.CancellationReason = request.CancellationReason;
            dbOrder.CancelledAt = DateTime.Now;
            _audit.PrepareLog(await GetStaffMemberIdAsync(), "Order", orderNumber, "Archive", request.CancellationReason);
            await _context.SaveChangesAsync();
            return Ok(orderNumber);
        }

        [HttpPut("{orderNumber:int}/restore")]
        public async Task<ActionResult<int>> RestoreOrder(int orderNumber)
        {
            var dbOrder = await _context.Orders.FindAsync(orderNumber);
            if (dbOrder == null)
                return NotFound($"Order {orderNumber} not found");

            dbOrder.CancelledFlag = false;
            dbOrder.CancellationReason = null;
            dbOrder.CancelledAt = null;
            _audit.PrepareLog(await GetStaffMemberIdAsync(), "Order", orderNumber, "Restore");
            await _context.SaveChangesAsync();
            return Ok(orderNumber);
        }

        [HttpDelete("{orderNumber:int}")]
        public async Task<ActionResult<int>> DeleteOrder(int orderNumber)
        {
            var dbOrder = await _context.Orders.FindAsync(orderNumber);
            if (dbOrder == null)
                return NotFound($"Order {orderNumber} not found");

            _context.Orders.Remove(dbOrder);
            await _context.SaveChangesAsync();

            return Ok(orderNumber);
        }

        private static bool IsNavigationCollection(PropertyInfo p) =>
            p.PropertyType.IsGenericType &&
            p.PropertyType.GetGenericTypeDefinition() == typeof(List<>);
    }

    public class CancelOrderRequest
    {
        public string? CancellationReason { get; set; }
    }
}
