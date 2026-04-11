using System.ComponentModel.DataAnnotations;

namespace CCCInventory
{
    // PIN is stored as plain text by design — this is a non-security-critical
    // identity selector for audit trail purposes, not an access control mechanism.
    // Primary security is provided by the JWT-based primary auth layer.
    public class StaffMember
    {
        [Key]
        public int Id { get; set; }
        public string Name { get; set; } = null!;
        public string Pin { get; set; } = null!;
        public bool IsActive { get; set; } = true;
    }
}
