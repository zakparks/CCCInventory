using System.ComponentModel.DataAnnotations;

namespace CCCInventory
{
    public class Cookie
    {
        [Key]
        public int Id { get; set; }
        public string CookieType { get; set; } = null!;
        public int CookieQuantity { get; set; }
        public string? CookieSize { get; set; }
    }
}
