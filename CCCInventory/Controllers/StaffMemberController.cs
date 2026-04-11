using CCCInventory.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CCCInventory.Controllers
{
    [Route("api/staffmembers")]
    [ApiController]
    [Authorize]
    public class StaffMemberController : ControllerBase
    {
        private readonly DataContext _context;

        public StaffMemberController(DataContext context)
        {
            _context = context;
        }

        // DTO: never exposes the PIN field
        public record StaffMemberDto(int Id, string Name, bool IsActive);

        public record ValidatePinRequest(string Pin);

        [HttpGet]
        public async Task<ActionResult<List<StaffMemberDto>>> GetAll()
        {
            return await _context.StaffMembers
                .OrderBy(s => s.Name)
                .Select(s => new StaffMemberDto(s.Id, s.Name, s.IsActive))
                .ToListAsync();
        }

        [HttpPost("validate-pin")]
        public async Task<IActionResult> ValidatePin([FromBody] ValidatePinRequest request)
        {
            var member = await _context.StaffMembers
                .Where(s => s.IsActive && s.Pin == request.Pin)
                .FirstOrDefaultAsync();

            if (member == null)
                return Unauthorized(new { message = "Incorrect PIN." });

            return Ok(new { id = member.Id, name = member.Name });
        }

        [HttpPost]
        public async Task<ActionResult<StaffMemberDto>> Create([FromBody] StaffMember member)
        {
            if (string.IsNullOrWhiteSpace(member.Name))
                return BadRequest(new { message = "Name is required." });
            if (string.IsNullOrWhiteSpace(member.Pin) || member.Pin.Length != 4 || !member.Pin.All(char.IsDigit))
                return BadRequest(new { message = "PIN must be exactly 4 digits." });

            _context.StaffMembers.Add(member);
            await _context.SaveChangesAsync();
            return CreatedAtAction(nameof(GetAll), new { id = member.Id },
                new StaffMemberDto(member.Id, member.Name, member.IsActive));
        }

        [HttpPut("{id}")]
        public async Task<ActionResult<StaffMemberDto>> Update(int id, [FromBody] StaffMember updated)
        {
            var member = await _context.StaffMembers.FindAsync(id);
            if (member == null) return NotFound();

            if (string.IsNullOrWhiteSpace(updated.Name))
                return BadRequest(new { message = "Name is required." });

            // Only validate and update PIN if one was provided
            if (!string.IsNullOrWhiteSpace(updated.Pin))
            {
                if (updated.Pin.Length != 4 || !updated.Pin.All(char.IsDigit))
                    return BadRequest(new { message = "PIN must be exactly 4 digits." });
                member.Pin = updated.Pin;
            }

            member.Name = updated.Name;
            member.IsActive = updated.IsActive;
            await _context.SaveChangesAsync();
            return Ok(new StaffMemberDto(member.Id, member.Name, member.IsActive));
        }

        [HttpDelete("{id}")]
        public async Task<ActionResult<StaffMemberDto>> Delete(int id)
        {
            var member = await _context.StaffMembers.FindAsync(id);
            if (member == null) return NotFound();

            member.IsActive = false;
            await _context.SaveChangesAsync();
            return Ok(new StaffMemberDto(member.Id, member.Name, member.IsActive));
        }
    }
}
