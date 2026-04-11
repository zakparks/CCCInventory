using CCCInventory.Data;
using Microsoft.AspNetCore.Authorization;
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
    [Authorize]
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

            // Calculate Thursday–Wednesday of the bake week containing the given date
            var dayOfWeek = (int)date.DayOfWeek;          // 0=Sun … 4=Thu … 6=Sat
            var daysFromThursday = (dayOfWeek - 4 + 7) % 7;
            var thursday = date.AddDays(-daysFromThursday).Date;
            var wednesday = thursday.AddDays(6).Date;
            var endOfWednesday = wednesday.AddDays(1);

            var orders = await _context.Orders
                .Include(o => o.Cakes)
                .Include(o => o.Cupcakes)
                .Include(o => o.Cookies)
                .Include(o => o.Pupcakes)
                .Include(o => o.OtherItems)
                .Where(o => !o.CancelledFlag &&
                            o.OrderDateTime >= thursday &&
                            o.OrderDateTime < endOfWednesday)
                .OrderBy(o => o.OrderDateTime)
                .ToListAsync();

            return Ok(new BakeSheetResponse
            {
                WeekStart = thursday,
                WeekEnd = wednesday,
                Orders = orders
            });
        }
    }
}
