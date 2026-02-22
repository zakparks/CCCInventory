using System.ComponentModel.DataAnnotations;

namespace CCCInventory
{
    public class Cupcake
    {
        [Key]
        public int Id { get; set; }
        public string CupcakeSize { get; set; } = null!;
        public int CupcakeQuantity { get; set; }
        public string CupcakeFlavor { get; set; } = null!;
        public string FillingFlavor { get; set; } = null!;
        public string IcingFlavor { get; set; } = null!;
        public string? SignatureName { get; set; }
    }
}
