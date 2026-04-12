using PayrollBackendProject.Domain.Enums;

namespace PayrollBackendProject.Domain.Entity
{
    public class AuditLog
    {
        public Guid Id { get; private set; }
        public string EntityName { get; private set; } = string.Empty;
        public Guid EntityId { get; private set; }
        public DateTime TimestampUTC { get; private set; }
        public AuditLogActionEnum Action { get; private set; }
        public string OriginalData { get; private set; } = string.Empty;
        public string UpdatedData { get; private set; } =  string.Empty;
        public string ActorId { get; private set; } = string.Empty;

        // Create the EF constructor
        private AuditLog() { }

        // Create the default constructor
        public AuditLog(string entityName, Guid entityId, AuditLogActionEnum action, string originalData, string updatedData,  string actorId)
        {
            Id = Guid.NewGuid();
            TimestampUTC = DateTime.UtcNow;
            EntityName = entityName;
            EntityId = entityId;
            Action = action;
            OriginalData = originalData;
            UpdatedData = updatedData;
            ActorId = actorId;
        }
    }
}
