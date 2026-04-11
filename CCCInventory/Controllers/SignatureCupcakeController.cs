using CCCInventory.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CCCInventory.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class SignatureCupcakeController : ControllerBase
    {
        private readonly DataContext _context;

        public SignatureCupcakeController(DataContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<ActionResult<List<SignatureCupcake>>> GetAll()
        {
            return Ok(await _context.SignatureCupcakes
                .OrderBy(s => s.SortOrder).ThenBy(s => s.Name)
                .ToListAsync());
        }

        [HttpPost]
        public async Task<ActionResult<SignatureCupcake>> Create(SignatureCupcake item)
        {
            _context.SignatureCupcakes.Add(item);
            await _context.SaveChangesAsync();
            return Ok(item);
        }

        [HttpPut("{id:int}")]
        public async Task<ActionResult<SignatureCupcake>> Update(int id, SignatureCupcake item)
        {
            var existing = await _context.SignatureCupcakes.FindAsync(id);
            if (existing == null) return NotFound();

            existing.Name = item.Name;
            existing.CupcakeFlavor = item.CupcakeFlavor;
            existing.FillingFlavor = item.FillingFlavor;
            existing.IcingFlavor = item.IcingFlavor;
            existing.IsActive = item.IsActive;
            existing.SortOrder = item.SortOrder;
            await _context.SaveChangesAsync();
            return Ok(existing);
        }

        [HttpDelete("{id:int}")]
        public async Task<ActionResult> Delete(int id)
        {
            var existing = await _context.SignatureCupcakes.FindAsync(id);
            if (existing == null) return NotFound();

            _context.SignatureCupcakes.Remove(existing);
            await _context.SaveChangesAsync();
            return Ok();
        }
    }
}
