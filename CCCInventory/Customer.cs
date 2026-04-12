using System.ComponentModel.DataAnnotations;

namespace CCCInventory
{
    public class Customer
    {
        [Key]
        public int CustomerId { get; set; }
        public string FirstName { get; set; } = null!;
        public string LastName { get; set; } = null!;
        public string? Email { get; set; }
        public string? Phone { get; set; }
        public List<Order> Orders { get; set; } = null!;
    }
}
