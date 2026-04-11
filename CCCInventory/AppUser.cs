using System.ComponentModel.DataAnnotations;

namespace CCCInventory
{
    public class AppUser
    {
        [Key]
        public int Id { get; set; }
        public string Username { get; set; } = null!;
        public string PasswordHash { get; set; } = null!;
    }
}
