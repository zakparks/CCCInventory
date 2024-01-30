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
                    OrderNumber = 1,
                    Details = "cake test",
                    CustName = "Jasper",
                    CustEmail = "asdf@gmail.com"
                },
                new Order
                {
                    OrderNumber = 2,
                    Details = "another cake test",
                    CustName = "Katniss",
                    CustEmail = "hjkl@gmail.com"
                }
            };
        }
    }
}
