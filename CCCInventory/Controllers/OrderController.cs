using CCCInventory.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Reflection;

namespace CCCInventory.Controllers
{
    // TODO - change the routes after the tutorial to make a little more sense in the URL
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
        public async Task<ActionResult<List<Order>>> GetOrders()
        {
            return Ok(await _context.Orders.Where(order => !order.DeleteFlag).ToListAsync());
        }

        [HttpGet("{orderNumber:int}")]
        public async Task<ActionResult<Order>> GetOrder(int orderNumber)
        {
            return Ok(await _context.Orders.FirstOrDefaultAsync(order => order.OrderNumber == orderNumber));
        }

        [HttpGet("newOrderNumber")]
        public async Task<ActionResult<int>> GetNewOrderNumber()
        {
            return Ok(await _context.Orders.MaxAsync(order => order.OrderNumber) + 1);
        }

        [HttpPost]
        // returns the orderNumber of the newest order in the database
        public async Task<ActionResult<int>> CreateOrder(Order order)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            _context.Orders.Add(order);
            await _context.SaveChangesAsync();
            int latestOrderNumber = await _context.Orders.MaxAsync(order => order.OrderNumber);

            if (order.OrderNumber != latestOrderNumber)
            {
                return BadRequest($"Order {order.OrderNumber} not created");
            }

            return Ok(latestOrderNumber);
        }

        [HttpPut]
        public async Task<ActionResult<int>> UpdateOrder(Order order)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var dbOrder = await _context.Orders.FindAsync(order.OrderNumber);
            if (dbOrder == null)
            {
                return BadRequest($"Order {order.OrderNumber} not found");
            }

            // assign all of the updated properties of the incoming order to the new dbOrder to be saved
            foreach (PropertyInfo property in typeof(Order).GetProperties().Where(p => p.CanWrite))
            {
                property.SetValue(dbOrder, property.GetValue(order, null), null);
            }

            if (await _context.SaveChangesAsync() == 0)
            {
                return BadRequest($"Order {order.OrderNumber} not created");
            }

            return Ok(order.OrderNumber);
        }

        [HttpDelete("{OrderNumber}")]
        public async Task<ActionResult<int>> DeleteOrder(int OrderNumber)
        {
            var dbOrder = await _context.Orders.FindAsync(OrderNumber);
            if (dbOrder == null)
            {
                return BadRequest($"Order {OrderNumber} not found");
            }

            _context.Orders.Remove(dbOrder);

            if (await _context.SaveChangesAsync() == 0)
            { 
                return BadRequest($"Order {OrderNumber} not marked as deleted");
            }

            return Ok(OrderNumber);
        }
    }
}
