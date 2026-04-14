using PayrollBackendProject.Domain.Entity;

namespace PayrollBackendProject.Application.Interfaces.Repository
{
    public interface IAuditLogRepository
    {
        public Task AddAuditLog(AuditLog log);
        public Task<List<AuditLog>> GetAllAuditLogs();
        public Task<List<AuditLog>> GetAuditLogsByEntityId(Guid entityId);
    }
}
