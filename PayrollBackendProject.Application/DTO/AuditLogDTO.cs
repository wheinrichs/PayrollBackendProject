namespace PayrollBackendProject.Application.DTO
{
    public class AuditLogDTO
    {
        public string EntityName { get; set; } = string.Empty;
        public Guid EntityId { get; set; }
        public string Action { get; set; } = string.Empty;
        public string ActorId { get; set; } = string.Empty;
        public DateTime TimestampUtc { get; set; }

        public string? OldValuesJson { get; set; }
        public string? NewValuesJson { get; set; }

        public AuditLogDTO(string entityName, Guid entityId, string action, string actorId, DateTime timestampUtc, string? oldValuesJson, string? newValuesJson)
        {
            EntityName = entityName;
            EntityId = entityId;
            Action = action;
            ActorId = actorId;
            TimestampUtc = timestampUtc;
            OldValuesJson = oldValuesJson;
            NewValuesJson = newValuesJson;
        }
    }
}
