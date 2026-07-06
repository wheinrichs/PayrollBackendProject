using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PayrollBackendProject.Application.DTO;
using PayrollBackendProject.Application.Interfaces.Services;

namespace PayrollBackendProject.API.Controllers
{
    /// <summary>
    /// Provides an endpoint for generating a read-only business pay report over a date range.
    /// </summary>
    /// <remarks>
    /// Unlike a pay run, generating a business pay report never creates or persists a pay run
    /// or pay statement - it only computes totals for review.
    ///
    /// Access is restricted to users with ADMIN or BACKEND roles.
    /// </remarks>
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Policy = "ApprovedBackendOnly")]
    public class BusinessPayReportController : ControllerBase
    {
        private readonly IBusinessPayReportService _service;

        /// <summary>
        /// Initializes a new instance of the <see cref="BusinessPayReportController"/> class.
        /// </summary>
        /// <param name="service">Service responsible for generating business pay reports.</param>
        public BusinessPayReportController(IBusinessPayReportService service)
        {
            _service = service;
        }

        /// <summary>
        /// Generates a business pay report for the specified date range.
        /// </summary>
        /// <param name="request">The request containing the report date range.</param>
        /// <returns>The computed totals for the report period.</returns>
        /// <response code="200">Returns the generated report.</response>
        /// <response code="400">If the report could not be generated.</response>
        [HttpPost]
        public async Task<ActionResult<BusinessPayReportResponseDTO>> GenerateReport(BusinessPayReportRequestDTO request)
        {
            try
            {
                BusinessPayReportResponseDTO response = await _service.GenerateReport(request);
                return Ok(response);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message);
            }
        }
    }
}
