using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using PayrollBackendProject.Application.DTO;
using PayrollBackendProject.Application.Interfaces.Services;

namespace PayrollBackendProject.API.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class CliniciansController : ControllerBase
    {
        private readonly IClinicianService _service;

        public CliniciansController(IClinicianService service)
        {
            _service = service;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<ClinicianResponseDTO>>> GetClinicians()
        {
            List<ClinicianResponseDTO> response = await _service.GetClinicians();
            return Ok(response);
        }

        [HttpPost]
        public async Task<ActionResult<ClinicianResponseDTO>> AddClinician(ClinicianRequestDTO newClinician)
        {
            ClinicianResponseDTO returnClinician = await _service.AddClinician(newClinician);
            return CreatedAtAction(nameof(GetClinician), new { id = returnClinician.ID}, returnClinician);
        }

        [HttpGet("search")]
        public async Task<ActionResult<ClinicianResponseDTO>> GetClinician([FromQuery] string? lastName)
        {
            if(string.IsNullOrEmpty(lastName))
            {
                return NotFound();
            }
            var returnClinician = await _service.GetClinicianByLastName(lastName);
            if (returnClinician != null)
            {
                return Ok(returnClinician);
            }
            return NotFound();
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<ClinicianResponseDTO>> GetClinicianByID(Guid id)
        {
            ClinicianResponseDTO? returnClinician = await _service.GetClinicianByID(id);
            if ( returnClinician == null)
            {
                return NotFound();
            }
            return Ok(returnClinician);
        }

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
