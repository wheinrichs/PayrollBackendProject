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
    public class PayRunController : ControllerBase
    {
        private readonly IPayRunService _service;
        public PayRunController(IPayRunService payRunService)
        {
            _service = payRunService;
        }

        [HttpPost]
        public async Task<ActionResult<PayRunResponseDTO>> GeneratePayRun(PayRunRequestDTO payRunRequest)
        {
            Guid currentUserGuid = TokenParser.RetrieveGuidFromToken(User);
            PayRunResponseDTO response = await _service.ExecutePayRun(payRunRequest, currentUserGuid);
            if (response == null)
            {
                return BadRequest("Unable to generate pay run");
            }
            return Ok(response);

        }

        [HttpGet("{payRunGuid}")]
        public async Task<ActionResult<List<PayStatementDTO>>> GetPayStatementsForRun(Guid payRunGuid)
        {
            List<PayStatementDTO> response = await _service.RetrievePayStatementsForRun(payRunGuid);
            return Ok(response);
        }

        [HttpPost("/approveRun/{payRunGuid}")]
        public async Task<ActionResult> ApprovePayRun(Guid payRunGuid)
        {
            Guid approvalUserId = TokenParser.RetrieveGuidFromToken(User);
            await _service.ApprovePayRun(payRunGuid, approvalUserId);
            return Ok();
        }


        [HttpPost("/approveStatement/{payStatementGuid}")]
        public async Task<ActionResult> ApprovePayStatement(Guid payStatementGuid)
        {
            Guid approvalUserId = TokenParser.RetrieveGuidFromToken(User);
            await _service.ApprovePayStatement(payStatementGuid, approvalUserId);
            return Ok();
        }
    }
}
