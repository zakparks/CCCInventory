using System.ComponentModel.DataAnnotations;

namespace CCCInventory
{
    public class Pupcake
    {
        [Key]
        public int Id { get; set; }
        public string PupcakeSize { get; set; } = null!;
        public int PupcakeQuantity { get; set; }
    }
}
