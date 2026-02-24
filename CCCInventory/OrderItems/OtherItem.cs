using System.ComponentModel.DataAnnotations;

namespace CCCInventory
{
    public class OtherItem
    {
        [Key]
        public int Id { get; set; }
        public string? Name { get; set; }
        public string? Item { get; set; }
    }
}
