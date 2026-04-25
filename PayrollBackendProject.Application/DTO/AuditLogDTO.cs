namespace PayrollBackendProject.Application.DTO
{
    /// <summary>
    /// Represents an audit log entry capturing a change made to a system entity.
    /// </summary>
    /// <remarks>
    /// Audit logs record create, update, and delete operations, including metadata
    /// about the actor, timestamp, and the before/after state of the entity.
    /// </remarks>
    public class AuditLogDTO
    {
        /// <summary>
        /// The name of the entity that was modified (e.g., "Clinician", "PayRun").
        /// </summary>
        public string EntityName { get; set; } = string.Empty;

        /// <summary>
        /// The unique identifier of the entity that was modified.
        /// </summary>
        public Guid EntityId { get; set; }

        /// <summary>
        /// The type of action performed on the entity (e.g., "CREATE", "UPDATE", "DELETE").
        /// </summary>
        public string Action { get; set; } = string.Empty;

        /// <summary>
        /// The identifier of the user or system that performed the action.
        /// </summary>
        public string ActorId { get; set; } = string.Empty;

        /// <summary>
        /// The UTC timestamp when the action occurred.
        /// </summary>
        public DateTime TimestampUtc { get; set; }

        /// <summary>
        /// A JSON representation of the entity's state before the change.
        /// </summary>
        /// <remarks>
        /// This value is null for create operations.
        /// </remarks>
        public string? OldValuesJson { get; set; }

        /// <summary>
        /// A JSON representation of the entity's state after the change.
        /// </summary>
        /// <remarks>
        /// This value is null for delete operations.
        /// </remarks>
        public string? NewValuesJson { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="AuditLogDTO"/> class.
        /// </summary>
        /// <param name="entityName">The name of the affected entity.</param>
        /// <param name="entityId">The unique identifier of the entity.</param>
        /// <param name="action">The action performed on the entity.</param>
        /// <param name="actorId">The identifier of the actor who performed the action.</param>
        /// <param name="timestampUtc">The UTC timestamp of the action.</param>
        /// <param name="oldValuesJson">The serialized state of the entity before the change.</param>
        /// <param name="newValuesJson">The serialized state of the entity after the change.</param>
        public AuditLogDTO(
            string entityName,
            Guid entityId,
            string action,
            string actorId,
            DateTime timestampUtc,
            string? oldValuesJson,
            string? newValuesJson)
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