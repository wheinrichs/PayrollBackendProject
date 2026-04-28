using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using PayrollBackendProject.API.Helper;
using PayrollBackendProject.Application.DTO;
using PayrollBackendProject.Application.Interfaces.Services;

namespace PayrollBackendProject.API.Controllers
{
    /// <summary>
    /// Provides endpoints for retrieving data related to the currently authenticated user.
    /// </summary>
    /// <remarks>
    /// This controller allows a clinician to access their own payroll-related data.
    /// All endpoints require authentication.
    /// </remarks>
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class MeController : ControllerBase
    {
        private readonly IPayRunService _service;

        /// <summary>
        /// Initializes a new instance of the <see cref="MeController"/> class.
        /// </summary>
        /// <param name="service">Service responsible for retrieving pay run and statement data.</param>
        public MeController(IPayRunService service)
        {
            _service = service;
        }

        /// <summary>
        /// Retrieves all pay statements for the currently authenticated clinician.
        /// </summary>
        /// <returns>A list of pay statements associated with the authenticated user.</returns>
        /// <response code="200">Returns the list of pay statements.</response>
        /// <response code="401">If the user is not authenticated.</response>
        [HttpGet("statements")]
        public async Task<ActionResult<List<PayStatementDTO>>> GetStatementsForClinician()
        {
            // Extract clinician ID from the authentication token
            Guid currentClinician = TokenParser.RetrieveGuidFromToken(User);

            // Retrieve statements associated with the current clinician
            var statements = await _service.RetrieveStatementsForUser(currentClinician);

            return Ok(statements);
        }
    }
}