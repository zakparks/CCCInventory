using System.ComponentModel.DataAnnotations;

namespace CCCInventory
{
    // Key/value store backing the Google OAuth token (Google.Apis IDataStore). There is a single
    // app-wide Google connection (the bakery account that owns the contract templates).
    public class GoogleToken
    {
        [Key]
        public string Key { get; set; } = null!;
        public string Value { get; set; } = null!;
        public DateTime UpdatedAt { get; set; }
    }
}
