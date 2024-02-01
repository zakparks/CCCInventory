using System.ComponentModel.DataAnnotations;

namespace CCCInventory
{
    public class Cupcake
    {
        [Key]
        public int Id { get; set; }
        public string CupcakeSize {  get; set; }  
        public int CupcakeQuantity { get; set; }
        public string CupcakeFlavor { get; set; }
        public string FillingFlavor { get; set; }
        public string IcingFlavor { get; set; }
    }
}
