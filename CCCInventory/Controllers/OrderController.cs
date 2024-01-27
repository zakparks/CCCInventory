using Microsoft.AspNetCore.Mvc;

namespace CCCInventory.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class OrderController : ControllerBase
    {
        [HttpGet]
        public async Task<ActionResult<List<Order>>> GetOrders()
        {
            return new List<Order> 
            { 
                new Order
                {
                    OrderNumber = 2,
                    Details = "cake test",
                    CustName = "Zak",
                    CustEmail = "zakparks@gmail.com"
                }
            };
        }
    }
}
