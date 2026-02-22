using CCCInventory.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CCCInventory.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class OptionController : ControllerBase
    {
        private readonly DataContext _context;

        public OptionController(DataContext context)
        {
            _context = context;
        }

        // GET /api/option  — all items grouped by category
        [HttpGet]
        public async Task<ActionResult<List<OptionItem>>> GetAll()
        {
            return Ok(await _context.OptionItems
                .OrderBy(o => o.Category).ThenBy(o => o.SortOrder).ThenBy(o => o.Value)
                .ToListAsync());
        }

        // GET /api/option/{category}
        [HttpGet("{category}")]
        public async Task<ActionResult<List<OptionItem>>> GetByCategory(string category)
        {
            return Ok(await _context.OptionItems
                .Where(o => o.Category == category)
                .OrderBy(o => o.SortOrder).ThenBy(o => o.Value)
                .ToListAsync());
        }

        [HttpPost]
        public async Task<ActionResult<OptionItem>> Create(OptionItem item)
        {
            _context.OptionItems.Add(item);
            await _context.SaveChangesAsync();
            return Ok(item);
        }

        [HttpPut("{id:int}")]
        public async Task<ActionResult<OptionItem>> Update(int id, OptionItem item)
        {
            var existing = await _context.OptionItems.FindAsync(id);
            if (existing == null) return NotFound();

            existing.Value = item.Value;
            existing.IsActive = item.IsActive;
            existing.SortOrder = item.SortOrder;
            await _context.SaveChangesAsync();
            return Ok(existing);
        }

        [HttpDelete("{id:int}")]
        public async Task<ActionResult> Delete(int id)
        {
            var existing = await _context.OptionItems.FindAsync(id);
            if (existing == null) return NotFound();

            // Fast path: skip the full scan if we already know this item is in use.
            // Use the /recheck endpoint to refresh this flag after deleting relevant orders.
            if (existing.IsInUse)
                return Conflict(new { error = $"'{existing.Value}' is used in one or more orders and cannot be deleted. Click Recheck to verify if this is still the case." });

            bool inUse = await IsOptionInUseAsync(existing);

            if (inUse)
            {
                existing.IsInUse = true;
                await _context.SaveChangesAsync();
                return Conflict(new { error = $"'{existing.Value}' is used in one or more orders and cannot be deleted." });
            }

            _context.OptionItems.Remove(existing);
            await _context.SaveChangesAsync();
            return Ok();
        }

        // POST /api/option/{id}/recheck
        // Re-scans all orders and updates the IsInUse cache flag. Returns the updated item.
        [HttpPost("{id:int}/recheck")]
        public async Task<ActionResult<OptionItem>> Recheck(int id)
        {
            var existing = await _context.OptionItems.FindAsync(id);
            if (existing == null) return NotFound();

            existing.IsInUse = await IsOptionInUseAsync(existing);
            await _context.SaveChangesAsync();
            return Ok(existing);
        }

        private async Task<bool> IsOptionInUseAsync(OptionItem item)
        {
            switch (item.Category)
            {
                case "CakeTierSize":
                    return await _context.Cakes.AnyAsync(c => c.TierSize == item.Value);
                case "CakeShape":
                    return await _context.Cakes.AnyAsync(c => c.CakeShape == item.Value);
                case "Flavor":
                    return await _context.Cakes.AnyAsync(c => c.CakeFlavor == item.Value)
                        || await _context.Cupcakes.AnyAsync(c => c.CupcakeFlavor == item.Value);
                case "FillingFlavor":
                    return await _context.Cakes.AnyAsync(c => c.FillingFlavor == item.Value)
                        || await _context.Cupcakes.AnyAsync(c => c.FillingFlavor == item.Value);
                case "IcingFlavor":
                    return await _context.Cakes.AnyAsync(c => c.IcingFlavor == item.Value)
                        || await _context.Cupcakes.AnyAsync(c => c.IcingFlavor == item.Value);
                case "CupcakeSize":
                    return await _context.Cupcakes.AnyAsync(c => c.CupcakeSize == item.Value);
                case "CookieType":
                    return await _context.Cookies.AnyAsync(c => c.CookieType == item.Value);
                default:
                    return false;
            }
        }
    }
}
