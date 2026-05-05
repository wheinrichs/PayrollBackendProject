using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using PayrollBackendProject.Application.DTO;
using PayrollBackendProject.Application.Interfaces.Services;

namespace PayrollBackendProject.API.Controllers
{
    /// <summary>
    /// Provides endpoints for managing clinicians, including creation, retrieval, search, and deletion.
    /// </summary>
    /// <remarks>
    /// All endpoints require authentication. Deletion operations require an Admin role.
    /// </remarks>
    [Authorize(Policy = "ApprovedBackendOnly, ApprovedAdminOnly")]
    [Route("api/[controller]")]
    [ApiController]
    public class CliniciansController : ControllerBase
    {
        private readonly IClinicianService _service;

        /// <summary>
        /// Initializes a new instance of the <see cref="CliniciansController"/> class.
        /// </summary>
        /// <param name="service">Service responsible for clinician-related business logic.</param>
        public CliniciansController(IClinicianService service)
        {
            _service = service;
        }

        /// <summary>
        /// Retrieves all clinicians.
        /// </summary>
        /// <returns>A list of clinicians.</returns>
        /// <response code="200">Returns the list of clinicians.</response>
        [HttpGet]
        public async Task<ActionResult<IEnumerable<ClinicianResponseDTO>>> GetClinicians()
        {
            List<ClinicianResponseDTO> response = await _service.GetClinicians();
            return Ok(response);
        }

        /// <summary>
        /// Creates a new clinician.
        /// </summary>
        /// <param name="newClinician">The clinician data to create.</param>
        /// <returns>The created clinician.</returns>
        /// <response code="201">Returns the newly created clinician.</response>
        /// <response code="400">If the request data is invalid.</response>
        [HttpPost]
        public async Task<ActionResult<ClinicianResponseDTO>> AddClinician(ClinicianRequestDTO newClinician)
        {
            ClinicianResponseDTO returnClinician = await _service.AddClinician(newClinician);
            return CreatedAtAction(nameof(GetClinicianByID), new { id = returnClinician.ID}, returnClinician);
        }

        /// <summary>
        /// Searches for a clinician by last name.
        /// </summary>
        /// <param name="lastName">The last name of the clinician.</param>
        /// <returns>The matching clinician if found.</returns>
        /// <response code="200">Returns the matching clinician or null if not found.</response>
        /// <response code="400">If the query parameter is missing or invalid.</response>
        [HttpGet("search")]
        public async Task<ActionResult<ClinicianResponseDTO>> GetClinician([FromQuery] string? lastName)
        {
            if(string.IsNullOrEmpty(lastName))
            {
                return BadRequest("lastName query parameter is required.");
            }

            var returnClinician = await _service.GetClinicianByLastName(lastName);

            return Ok(returnClinician);
        }

        /// <summary>
        /// Retrieves a clinician by their unique identifier.
        /// </summary>
        /// <param name="id">The unique ID of the clinician.</param>
        /// <returns>The clinician if found.</returns>
        /// <response code="200">Returns the clinician.</response>
        /// <response code="404">If the clinician is not found.</response>
        [HttpGet("{id}")]
        public async Task<ActionResult<ClinicianResponseDTO>> GetClinicianByID(Guid id)
        {
            ClinicianResponseDTO? returnClinician = await _service.GetClinicianByID(id);

            if (returnClinician == null)
            {
                return NotFound();
            }
            return Ok(returnClinician);
        }

        /// <summary>
        /// Deletes a clinician by their unique identifier.
        /// </summary>
        /// <param name="id">The unique ID of the clinician to delete.</param>
        /// <returns>No content if deletion is successful.</returns>
        /// <response code="204">Clinician successfully deleted.</response>
        /// <response code="404">If the clinician is not found.</response>
        /// <response code="403">If the user is not authorized to perform this action.</response>
        [Authorize(Roles = "Admin")]
        [HttpDelete("{id}")]
        public async Task<ActionResult<bool>> RemoveClinician(Guid id)
        {
            bool returnDelete = await _service.RemoveClinicianByID(id);
            if(returnDelete) {  return NoContent(); }
            return NotFound();
        }
    }
}