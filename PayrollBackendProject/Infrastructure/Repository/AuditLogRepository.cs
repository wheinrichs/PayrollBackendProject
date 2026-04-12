using Microsoft.EntityFrameworkCore;
using PayrollBackendProject.Application.Interfaces.Repository;
using PayrollBackendProject.Domain.Entity;
using PayrollBackendProject.Infrastructure.Data;

namespace PayrollBackendProject.Infrastructure.Repository
{
    public class AuditLogRepository : IAuditLogRepository
    {
        private readonly ClinicianDbContext _database;

        public AuditLogRepository(ClinicianDbContext database)
        {
            _database = database;
        }

        public async Task AddAuditLog(AuditLog log)
        {
            await _database.AuditLogs.AddAsync(log);
        }

        public async Task<List<AuditLog>> GetAllAuditLogs()
        {
            return await _database.AuditLogs.ToListAsync();
        }

        public async Task<List<AuditLog>> GetAuditLogsByEntityId(Guid entityId)
        {
            return await _database.AuditLogs.Where(l => l.EntityId == entityId).ToListAsync();
        }
    }
}
