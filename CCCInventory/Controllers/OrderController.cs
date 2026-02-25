using CCCInventory.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Reflection;

namespace CCCInventory.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class OrderController : ControllerBase
    {
        private readonly DataContext _context;

        public OrderController(DataContext context)
        {
            _context = context;
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

        [HttpGet("newOrderNumber")]
        public async Task<ActionResult<int>> GetNewOrderNumber()
        {
            // Cast to nullable int so MaxAsync returns null on an empty table instead of throwing
            int? max = await _context.Orders.MaxAsync(order => (int?)order.OrderNumber);
            return Ok((max ?? 0) + 1);
        }

        [HttpPost]
        public async Task<ActionResult<int>> CreateOrder(Order order)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            _context.Orders.Add(order);
            await _context.SaveChangesAsync();

            return Ok(order.OrderNumber);
        }

        [HttpPut]
        public async Task<ActionResult<int>> UpdateOrder(Order order)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            // Load WITHOUT Include — using Include causes EF to track the existing child entities,
            // and assigning the incoming collection (which carries the same IDs) produces an
            // identity-conflict exception from the change tracker.
            var dbOrder = await _context.Orders
                .FirstOrDefaultAsync(o => o.OrderNumber == order.OrderNumber);

            if (dbOrder == null)
                return NotFound($"Order {order.OrderNumber} not found");

            using var transaction = await _context.Database.BeginTransactionAsync();

            // Update scalar properties on the tracked Order row
            foreach (PropertyInfo property in typeof(Order).GetProperties()
                .Where(p => p.CanWrite && !IsNavigationCollection(p)))
            {
                property.SetValue(dbOrder, property.GetValue(order, null), null);
            }

            // Delete old child rows directly (bypasses the change tracker — no identity conflict)
            int oNum = order.OrderNumber;
            await _context.Cakes      .Where(c => EF.Property<int?>(c, "OrderNumber") == oNum).ExecuteDeleteAsync();
            await _context.Cupcakes   .Where(c => EF.Property<int?>(c, "OrderNumber") == oNum).ExecuteDeleteAsync();
            await _context.Cookies    .Where(c => EF.Property<int?>(c, "OrderNumber") == oNum).ExecuteDeleteAsync();
            await _context.Pupcakes   .Where(c => EF.Property<int?>(c, "OrderNumber") == oNum).ExecuteDeleteAsync();
            await _context.OtherItems .Where(c => EF.Property<int?>(c, "OrderNumber") == oNum).ExecuteDeleteAsync();

            // Assign incoming collections — reset Ids to 0 so EF generates new primary keys
            dbOrder.Cakes      = order.Cakes?      .Select(c => { c.Id = 0; return c; }).ToList();
            dbOrder.Cupcakes   = order.Cupcakes?   .Select(c => { c.Id = 0; return c; }).ToList();
            dbOrder.Cookies    = order.Cookies?    .Select(c => { c.Id = 0; return c; }).ToList();
            dbOrder.Pupcakes   = order.Pupcakes?   .Select(c => { c.Id = 0; return c; }).ToList();
            dbOrder.OtherItems = order.OtherItems? .Select(c => { c.Id = 0; return c; }).ToList();

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            return Ok(order.OrderNumber);
        }

        [HttpGet("cancelled")]
        public async Task<ActionResult<List<Order>>> GetCancelledOrders()
        {
            var now = DateTime.Now;
            return Ok(await _context.Orders
                .Where(order => order.CancelledFlag && order.OrderDateTime > now)
                .Include(o => o.Cakes)
                .Include(o => o.Cupcakes)
                .Include(o => o.Cookies)
                .Include(o => o.Pupcakes)
                .Include(o => o.OtherItems)
                .ToListAsync());
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
            await _context.SaveChangesAsync();
            return Ok(orderNumber);
        }

        [HttpDelete("{orderNumber:int}")]
        public async Task<ActionResult<int>> DeleteOrder(int orderNumber)
        {
            var dbOrder = await _context.Orders.FindAsync(orderNumber);
            if (dbOrder == null)
                return NotFound($"Order {orderNumber} not found");

            // Hard delete — permanent removal
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
