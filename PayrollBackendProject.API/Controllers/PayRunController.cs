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
    /// - Approving or rejecting pay runs and individual pay statements
    /// 
    /// Access is restricted to users with ADMIN or BACKEND roles.
    /// </remarks>
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Policy = "ApprovedBackendOnly")]
    public class PayRunController : ControllerBase
    {
        private readonly IPayRunService _service;
        private readonly IStatementExportService _exportService;

        /// <summary>
        /// Initializes a new instance of the <see cref="PayRunController"/> class.
        /// </summary>
        /// <param name="payRunService">Service responsible for executing and managing pay runs.</param>
        /// <param name="statementExportService">Service responsible for exporting approved pay statements.</param>
        public PayRunController(IPayRunService payRunService, IStatementExportService statementExportService)
        {
            _service = payRunService;
            _exportService = statementExportService;
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
            try
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
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(ex.Message);
            }
        }

        /// <summary>
        /// Retrieves all pay runs.
        /// </summary>
        /// <returns>A list of all pay runs with summary totals and status.</returns>
        /// <response code="200">Returns the list of pay runs.</response>
        [HttpGet]
        public async Task<ActionResult<List<PayRunResponseDTO>>> GetAllPayRuns()
        {
            List<PayRunResponseDTO> response = await _service.GetAllPayRuns();
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
        /// Exports all approved pay statements for a pay run as a single CSV file.
        /// </summary>
        /// <param name="payRunGuid">The unique identifier of the pay run.</param>
        /// <returns>A CSV file containing one row per line item across all approved statements.</returns>
        /// <response code="200">Returns the CSV file.</response>
        /// <response code="400">If the pay run has no approved statements.</response>
        /// <response code="404">If the pay run does not exist.</response>
        [HttpGet("{payRunGuid}/statements/export")]
        public async Task<IActionResult> ExportApprovedStatements(Guid payRunGuid)
        {
            try
            {
                // Extract the current user ID from the authentication token
                Guid currentUserGuid = TokenParser.RetrieveGuidFromToken(User);

                StatementExportResultDTO result = await _exportService.ExportApprovedStatementsCsv(payRunGuid, currentUserGuid);

                return File(result.Content, "text/csv", result.FileName);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(ex.Message);
            }
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

        /// <summary>
        /// Rejects a pay run.
        /// </summary>
        /// <param name="payRunGuid">The unique identifier of the pay run to reject.</param>
        /// <returns>No content if the rejection is successful.</returns>
        /// <response code="204">The pay run was successfully rejected.</response>
        /// <response code="401">If the user is not authenticated.</response>
        /// <response code="403">If the user does not have permission to reject.</response>
        [HttpPost("/rejectRun/{payRunGuid}/reject")]
        public async Task<ActionResult> RejectPayRun(Guid payRunGuid)
        {
            // Extract the rejecting user ID from the authentication token
            Guid approvalUserId = TokenParser.RetrieveGuidFromToken(User);

            // Reject the specified pay run
            await _service.RejectPayRun(payRunGuid, approvalUserId);

            return NoContent();
        }

        /// <summary>
        /// Rejects an individual pay statement.
        /// </summary>
        /// <param name="payStatementGuid">The unique identifier of the pay statement to reject.</param>
        /// <returns>No content if the rejection is successful.</returns>
        /// <response code="204">The pay statement was successfully rejected.</response>
        /// <response code="401">If the user is not authenticated.</response>
        /// <response code="403">If the user does not have permission to reject.</response>
        [HttpPost("/rejectStatement/{payStatementGuid}/reject")]
        public async Task<ActionResult> RejectPayStatement(Guid payStatementGuid)
        {
            // Extract the rejecting user ID from the authentication token
            Guid approvalUserId = TokenParser.RetrieveGuidFromToken(User);

            // Reject the specified pay statement
            await _service.RejectPayStatement(payStatementGuid, approvalUserId);

            return NoContent();
        }
    }
}