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
        public async Task<ActionResult<List<Order>>> GetOrders()
        {
            return Ok(await _context.Orders.ToListAsync());
        }

        [HttpPost]
        public async Task<ActionResult<List<Order>>> CreateOrder(Order order)
        {
            _context.Orders.Add(order);
            await _context.SaveChangesAsync();
            return Ok(await _context.Orders.ToListAsync());
        }

        [HttpPut]
        public async Task<ActionResult<List<Order>>> UpdateOrder(Order order)
        {
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

            await _context.SaveChangesAsync();
            return Ok(await _context.Orders.ToListAsync());
        }

        [HttpDelete("{OrderNumber}")]
        public async Task<ActionResult<List<Order>>> DeleteOrder(int OrderNumber)
        {
            var dbOrder = await _context.Orders.FindAsync(OrderNumber);
            if (dbOrder == null)
            {
                return BadRequest($"Order {OrderNumber} not found");
            }

            _context.Orders.Remove(dbOrder);
            await _context.SaveChangesAsync();
            return Ok(await _context.Orders.ToListAsync());
        }
    }
}
