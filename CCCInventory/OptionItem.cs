using System.ComponentModel.DataAnnotations;

namespace CCCInventory
{
    public class OptionItem
    {
        [Key]
        public int Id { get; set; }
        public string Category { get; set; } = null!;
        public string Value { get; set; } = null!;
        public bool IsActive { get; set; } = true;
        public int SortOrder { get; set; } = 0;
        /// <summary>Cached flag: true = known to be referenced by at least one order. Set automatically on a failed delete attempt; cleared via the recheck endpoint.</summary>
        public bool IsInUse { get; set; } = false;
    }
}
