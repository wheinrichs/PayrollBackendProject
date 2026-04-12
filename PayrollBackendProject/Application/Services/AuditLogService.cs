using Microsoft.AspNetCore.Authorization;
using PayrollBackendProject.Application.DTO;
using PayrollBackendProject.Application.Interfaces.Repository;
using PayrollBackendProject.Application.Interfaces.Services;
using PayrollBackendProject.Application.Mappings;
using PayrollBackendProject.Domain.Entity;

namespace PayrollBackendProject.Application.Services
{
    public class AuditLogService : IAuditLogService
    {
        private readonly IAuditLogRepository _auditLogRepository;

        public AuditLogService(IAuditLogRepository auditLogRepository)
        {
            _auditLogRepository = auditLogRepository;
        }

        public async Task<List<AuditLogDTO>> GetAllLogs()
        {
            List<AuditLog> auditLogList = await _auditLogRepository.GetAllAuditLogs();
            List<AuditLogDTO> auditLogsDto = auditLogList.Select(l => AuditLogMapper.DomainToDto(l)).ToList();
            return auditLogsDto;
        }

        public async Task<List<AuditLogDTO>> GetLogsByEntityId(Guid entityId)
        {
            throw new NotImplementedException();
        }
    }
}
