using PayrollBackendProject.Application.DTO;
using PayrollBackendProject.Domain.Entity;

namespace PayrollBackendProject.Application.Mappings
{
    public static class AuditLogMapper
    {
        public static AuditLogDTO DomainToDto(AuditLog log)
        {
            return new AuditLogDTO(log.EntityName, log.EntityId, log.Action.ToString(), log.ActorId, log.TimestampUTC, log.OriginalData, log.UpdatedData);
        }
    }
}
