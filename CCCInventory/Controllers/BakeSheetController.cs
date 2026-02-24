using CCCInventory.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CCCInventory.Controllers
{
    public class BakeSheetResponse
    {
        public DateTime WeekStart { get; set; }
        public DateTime WeekEnd { get; set; }
        public List<Order> Orders { get; set; } = [];
    }

    [Route("api/[controller]")]
    [ApiController]
    public class BakeSheetController : ControllerBase
    {
        private readonly DataContext _context;

        public BakeSheetController(DataContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<ActionResult<BakeSheetResponse>> GetBakeSheet([FromQuery] string? weekOf)
        {
            var date = string.IsNullOrEmpty(weekOf)
                ? DateTime.Today
                : DateTime.Parse(weekOf);

            // Calculate Monday–Sunday of the given week
            var dayOfWeek = (int)date.DayOfWeek;
            var monday = date.AddDays(-(dayOfWeek == 0 ? 6 : dayOfWeek - 1)).Date;
            var sunday = monday.AddDays(6).Date;
            var endOfSunday = sunday.AddDays(1);

            var orders = await _context.Orders
                .Include(o => o.Cakes)
                .Include(o => o.Cupcakes)
                .Include(o => o.Cookies)
                .Include(o => o.Pupcakes)
                .Where(o => !o.CancelledFlag &&
                            o.OrderDateTime >= monday &&
                            o.OrderDateTime < endOfSunday)
                .OrderBy(o => o.OrderDateTime)
                .ToListAsync();

            return Ok(new BakeSheetResponse
            {
                WeekStart = monday,
                WeekEnd = sunday,
                Orders = orders
            });
        }
    }
}
