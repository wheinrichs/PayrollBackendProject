using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using PayrollBackendProject.API.Helper;
using PayrollBackendProject.Application.DTO;
using PayrollBackendProject.Application.Interfaces.Services;

namespace PayrollBackendProject.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class MeController : ControllerBase
    {
        private readonly IPayRunService _service;

        public MeController(IPayRunService service)
        {
            _service = service;
        }

        [HttpGet("statements")]
        public async Task<ActionResult<List<PayStatementDTO>>> GetStatementsForClinician()
        {
            Guid currentClinician = TokenParser.RetrieveGuidFromToken(User);
            var statements = await _service.RetrieveStatementsForUser(currentClinician);
            return Ok(statements);
        }
    }
}
