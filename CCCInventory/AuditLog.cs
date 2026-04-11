using System.ComponentModel.DataAnnotations;

namespace CCCInventory
{
    public class AuditLog
    {
        [Key]
        public int Id { get; set; }
        public int? StaffMemberId { get; set; }
        public StaffMember? StaffMember { get; set; }
        public string EntityType { get; set; } = null!;
        public int EntityId { get; set; }
        public string Action { get; set; } = null!;
        public string? Details { get; set; }
        public DateTime Timestamp { get; set; }
    }
}
