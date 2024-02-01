using System.ComponentModel.DataAnnotations;

namespace CCCInventory
{
    public class Cookie
    {
        [Key]
        public int Id { get; set; }
        public string CookieType { get; set; }
        public int CookieQuantity { get; set; }
    }
}
