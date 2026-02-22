using System.ComponentModel.DataAnnotations;

namespace CCCInventory
{
    public class SignatureCupcake
    {
        [Key]
        public int Id { get; set; }
        public string Name { get; set; } = null!;
        public string CupcakeFlavor { get; set; } = null!;
        public string FillingFlavor { get; set; } = null!;
        public string IcingFlavor { get; set; } = null!;
        public bool IsActive { get; set; } = true;
        public int SortOrder { get; set; } = 0;
    }
}
