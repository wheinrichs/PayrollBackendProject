using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using PayrollBackendProject.API.Helper;
using PayrollBackendProject.Application.DTO;
using PayrollBackendProject.Application.Interfaces.Services;

namespace PayrollBackendProject.API.Controllers
{
    /// <summary>
    /// Provides endpoints for generating and managing pay runs and their associated pay statements.
    /// </summary>
    /// <remarks>
    /// This controller supports:
    /// - Creating pay runs for a given date range
    /// - Retrieving pay statements for a specific run
    /// - Approving pay runs and individual pay statements
    /// 
    /// Access is restricted to users with ADMIN or BACKEND roles.
    /// </remarks>
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Policy = "ApprovedBackendOnly")]
    public class PayRunController : ControllerBase
    {
        private readonly IPayRunService _service;

        /// <summary>
        /// Initializes a new instance of the <see cref="PayRunController"/> class.
        /// </summary>
        /// <param name="payRunService">Service responsible for executing and managing pay runs.</param>
        public PayRunController(IPayRunService payRunService)
        {
            _service = payRunService;
        }

        /// <summary>
        /// Generates a new pay run for the specified date range.
        /// </summary>
        /// <param name="payRunRequest">The request containing the pay run date range.</param>
        /// <returns>The generated pay run with summary totals and status.</returns>
        /// <response code="200">Returns the generated pay run.</response>
        /// <response code="400">If the pay run could not be generated.</response>
        [HttpPost]
        public async Task<ActionResult<PayRunResponseDTO>> GeneratePayRun(PayRunRequestDTO payRunRequest)
        {
            // Extract the current user ID from the authentication token
            Guid currentUserGuid = TokenParser.RetrieveGuidFromToken(User);

            // Execute the pay run generation process
            PayRunResponseDTO response = await _service.ExecutePayRun(payRunRequest, currentUserGuid);

            if (response == null)
            {
                return BadRequest("Unable to generate pay run");
            }

            return Ok(response);
        }

        /// <summary>
        /// Retrieves all pay statements associated with a specific pay run.
        /// </summary>
        /// <param name="payRunGuid">The unique identifier of the pay run.</param>
        /// <returns>A list of pay statements for the specified pay run.</returns>
        /// <response code="200">Returns the list of pay statements.</response>
        [HttpGet("{payRunGuid}")]
        public async Task<ActionResult<List<PayStatementDTO>>> GetPayStatementsForRun(Guid payRunGuid)
        {
            List<PayStatementDTO> response = await _service.RetrievePayStatementsForRun(payRunGuid);
            return Ok(response);
        }

        /// <summary>
        /// Approves a pay run.
        /// </summary>
        /// <param name="payRunGuid">The unique identifier of the pay run to approve.</param>
        /// <returns>No content if the approval is successful.</returns>
        /// <response code="204">The pay run was successfully approved.</response>
        /// <response code="401">If the user is not authenticated.</response>
        /// <response code="403">If the user does not have permission to approve.</response>
        [HttpPost("/approveRun/{payRunGuid}/approve")]
        public async Task<ActionResult> ApprovePayRun(Guid payRunGuid)
        {
            // Extract the approving user ID from the authentication token
            Guid approvalUserId = TokenParser.RetrieveGuidFromToken(User);

            // Approve the specified pay run
            await _service.ApprovePayRun(payRunGuid, approvalUserId);

            return NoContent();
        }

        /// <summary>
        /// Approves an individual pay statement.
        /// </summary>
        /// <param name="payStatementGuid">The unique identifier of the pay statement to approve.</param>
        /// <returns>No content if the approval is successful.</returns>
        /// <response code="204">The pay statement was successfully approved.</response>
        /// <response code="401">If the user is not authenticated.</response>
        /// <response code="403">If the user does not have permission to approve.</response>
        [HttpPost("/approveStatement/{payStatementGuid}/approve")]
        public async Task<ActionResult> ApprovePayStatement(Guid payStatementGuid)
        {
            // Extract the approving user ID from the authentication token
            Guid approvalUserId = TokenParser.RetrieveGuidFromToken(User);

            // Approve the specified pay statement
            await _service.ApprovePayStatement(payStatementGuid, approvalUserId);

            return NoContent();
        }
    }
}