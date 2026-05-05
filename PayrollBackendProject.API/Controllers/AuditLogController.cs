using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using PayrollBackendProject.Application.DTO;
using PayrollBackendProject.Application.Interfaces.Services;

namespace PayrollBackendProject.API.Controllers
{
    /// <summary>
    /// Provides endpoints for retrieving audit logs for the payroll system entities.
    /// </summary>
    /// <remarks>
    /// Audit logs track changes made to entities within the system, including
    /// creation, updates, and deletions. All endpoints require authentication.
    /// </remarks>
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Policy = "ApprovedAdminOnly")]
    public class AuditLogController : ControllerBase
    {
        private readonly IAuditLogService _service;

        /// <summary>
        /// Initializes a new instance of the <see cref="AuditLogController"/> class.
        /// </summary>
        /// <param name="service">
        /// Service responsible for retrieving audit log data.
        /// </param>
        public AuditLogController(IAuditLogService service)
        {
            _service = service;
        }

        /// <summary>
        /// Retrieves all audit logs in the system.
        /// </summary>
        /// <returns>
        /// A list of all audit logs.
        /// </returns>
        /// <response code="200">Successfully retrieved audit logs.</response>
        /// <response code="401">Unauthorized. A valid JWT token is required.</response>
        [HttpGet]
        public async Task<ActionResult<List<AuditLogDTO>>> GetAllAuditLogs()
        {
            return Ok(await _service.GetAllLogs());
        }

        /// <summary>
        /// Retrieves audit logs associated with a specific entity.
        /// </summary>
        /// <param name="entityId">
        /// The unique identifier of the entity whose audit logs are requested.
        /// </param>
        /// <returns>
        /// A list of audit logs related to the specified entity.
        /// </returns>
        /// <response code="200">Successfully retrieved audit logs for the entity.</response>
        /// <response code="401">Unauthorized. A valid JWT token is required.</response>
        /// <response code="400">Invalid entity ID format.</response>
        [HttpGet("{entityId}")]
        public async Task<ActionResult<List<AuditLogDTO>>> GetAuditLogsForEntity(Guid entityId)
        {
            return Ok(await _service.GetLogsByEntityId(entityId));
        }
    }
}