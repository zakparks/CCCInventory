using System;
using System.ComponentModel.DataAnnotations;

namespace CCCInventory
{
    public class Cake
    {
        [Key]
        public int Id { get; set; }
        public string TierSize { get; set; }
        public int NumTierLayers { get; set; }
        public string CakeShape { get; set; }
        public string CakeFlavor { get; set; }
        public string FillingFlavor { get; set; }
        public string IcingFlavor { get; set; }
        public bool SplitTier { get; set; }
    }
}