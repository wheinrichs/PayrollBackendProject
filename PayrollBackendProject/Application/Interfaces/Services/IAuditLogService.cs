using PayrollBackendProject.Application.DTO;

namespace PayrollBackendProject.Application.Interfaces.Services
{
    public interface IAuditLogService
    {
        public Task<List<AuditLogDTO>> GetAllLogs();
        public Task<List<AuditLogDTO>> GetLogsByEntityId(Guid entityId);
    }
}
