using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using PayrollBackendProject.Application.DTO;
using PayrollBackendProject.Application.Interfaces.Services;

namespace PayrollBackendProject.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class AuditLogController : ControllerBase
    {
        private readonly IAuditLogService _service;

        public AuditLogController(IAuditLogService service)
        {
            _service = service;
        }

        [HttpGet]
        public async Task<ActionResult<List<AuditLogDTO>>> GetAllAuditLogs()
        {
            return Ok(await _service.GetAllLogs());
        }

        [HttpGet("{entityId}")]
        public async Task<ActionResult<List<AuditLogDTO>>> GetAuditLogsForEntity(Guid entityId)
        {
            return Ok(await _service.GetLogsByEntityId(entityId));
        }
    }
}
