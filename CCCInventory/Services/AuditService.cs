using CCCInventory.Data;

namespace CCCInventory.Services
{
    public class AuditService
    {
        private readonly DataContext _context;

        public AuditService(DataContext context)
        {
            _context = context;
        }

        // Adds an audit log entry to the context WITHOUT calling SaveChanges.
        // The caller is responsible for saving — this ensures the audit record
        // is committed in the same transaction as the entity change.
        public void PrepareLog(int? staffMemberId, string entityType, int entityId, string action, string? details = null)
        {
            _context.AuditLogs.Add(new AuditLog
            {
                StaffMemberId = staffMemberId,
                EntityType = entityType,
                EntityId = entityId,
                Action = action,
                Details = details,
                Timestamp = DateTime.UtcNow
            });
        }
    }
}
