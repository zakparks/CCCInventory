using System.ComponentModel.DataAnnotations;

namespace CCCInventory
{
    public class Cake
    {
        [Key]
        public int Id { get; set; }
        public string TierSize { get; set; } = null!;
        public int NumTierLayers { get; set; }
        public string CakeShape { get; set; } = null!;
        public string CakeFlavor { get; set; } = null!;
        public string FillingFlavor { get; set; } = null!;
        public string IcingFlavor { get; set; } = null!;
        public bool SplitTier { get; set; }
    }
}
