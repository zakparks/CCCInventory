using CCCInventory.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CCCInventory.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class CustomerController : ControllerBase
    {
        private readonly DataContext _context;

        public CustomerController(DataContext context)
        {
            _context = context;
        }

        public record CustomerListDto(int CustomerId, string FirstName, string LastName, string? Email, string? Phone, int OrderCount);
        public record CustomerSearchDto(int CustomerId, string FirstName, string LastName, string? Email, string? Phone);

        [HttpGet]
        public async Task<ActionResult<List<CustomerListDto>>> GetCustomers()
        {
            var customers = await _context.Customers
                .OrderBy(c => c.LastName)
                .ThenBy(c => c.FirstName)
                .Select(c => new CustomerListDto(
                    c.CustomerId,
                    c.FirstName,
                    c.LastName,
                    c.Email,
                    c.Phone,
                    c.Orders.Count))
                .ToListAsync();

            return Ok(customers);
        }

        [HttpGet("{id:int}")]
        public async Task<ActionResult<Customer>> GetCustomer(int id)
        {
            var customer = await _context.Customers
                .Include(c => c.Orders)
                .FirstOrDefaultAsync(c => c.CustomerId == id);

            if (customer == null) return NotFound($"Customer {id} not found");
            return Ok(customer);
        }

        [HttpGet("search")]
        public async Task<ActionResult<List<CustomerSearchDto>>> SearchCustomers([FromQuery] string q)
        {
            if (string.IsNullOrWhiteSpace(q) || q.Length < 2)
                return Ok(new List<CustomerSearchDto>());

            var lower = q.ToLower();
            var results = await _context.Customers
                .Where(c =>
                    c.FirstName.ToLower().Contains(lower) ||
                    c.LastName.ToLower().Contains(lower) ||
                    (c.FirstName + " " + c.LastName).ToLower().Contains(lower) ||
                    (c.Email != null && c.Email.ToLower().Contains(lower)))
                .OrderBy(c => c.LastName)
                .ThenBy(c => c.FirstName)
                .Take(10)
                .Select(c => new CustomerSearchDto(c.CustomerId, c.FirstName, c.LastName, c.Email, c.Phone))
                .ToListAsync();

            return Ok(results);
        }

        [HttpPost]
        public async Task<ActionResult<int>> CreateCustomer(Customer customer)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            _context.Customers.Add(customer);
            await _context.SaveChangesAsync();
            return Ok(customer.CustomerId);
        }

        [HttpPut]
        public async Task<ActionResult<int>> UpdateCustomer(Customer customer)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var existing = await _context.Customers.FindAsync(customer.CustomerId);
            if (existing == null) return NotFound($"Customer {customer.CustomerId} not found");

            existing.FirstName = customer.FirstName;
            existing.LastName = customer.LastName;
            existing.Email = customer.Email;
            existing.Phone = customer.Phone;

            await _context.SaveChangesAsync();
            return Ok(customer.CustomerId);
        }

        [HttpDelete("{id:int}")]
        public async Task<ActionResult<int>> DeleteCustomer(int id)
        {
            var customer = await _context.Customers
                .Include(c => c.Orders)
                .FirstOrDefaultAsync(c => c.CustomerId == id);

            if (customer == null) return NotFound($"Customer {id} not found");
            if (customer.Orders.Count > 0)
                return BadRequest("Cannot delete a customer with linked orders.");

            _context.Customers.Remove(customer);
            await _context.SaveChangesAsync();
            return Ok(id);
        }

        public record MergeRequest(int KeepId, int MergeId);

        [HttpPost("merge")]
        public async Task<ActionResult<int>> MergeCustomers(MergeRequest request)
        {
            if (request.KeepId == request.MergeId)
                return BadRequest("Cannot merge a customer with itself.");

            var keep  = await _context.Customers.FindAsync(request.KeepId);
            var merge = await _context.Customers.FindAsync(request.MergeId);

            if (keep  == null) return NotFound($"Customer {request.KeepId} not found");
            if (merge == null) return NotFound($"Customer {request.MergeId} not found");

            await _context.Orders
                .Where(o => o.CustomerId == request.MergeId)
                .ExecuteUpdateAsync(s => s.SetProperty(o => o.CustomerId, request.KeepId));

            _context.Customers.Remove(merge);
            await _context.SaveChangesAsync();

            return Ok(request.KeepId);
        }
    }
}
