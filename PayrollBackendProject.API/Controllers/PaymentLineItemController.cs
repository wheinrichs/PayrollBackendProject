using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PayrollBackendProject.API.Helper;
using PayrollBackendProject.Application.DTO;
using PayrollBackendProject.Application.Interfaces.Services;

namespace PayrollBackendProject.API.Controllers
{
    [Authorize(Policy = "ApprovedBackendOnly")]
    [Route("api/payments")]
    [ApiController]
    public class PaymentLineItemController : ControllerBase
    {
        private readonly IPaymentLineItemService _service;

        public PaymentLineItemController(IPaymentLineItemService service)
        {
            _service = service;
        }

        [HttpGet("takebacks/pending")]
        public async Task<ActionResult<List<UnappliedCode500ResponseDTO>>> GetUnappliedCode500Payments()
        {
            List<UnappliedCode500ResponseDTO> result = await _service.GetUnappliedCode500Payments();
            return Ok(result);
        }

        [HttpPost("manual")]
        public async Task<ActionResult<Guid>> AddManualPayment([FromBody] ManualPaymentRequestDTO request)
        {
            try
            {
                Guid userId = TokenParser.RetrieveGuidFromToken(User);
                Guid newItemId = await _service.AddManualPayment(request, userId);
                return CreatedAtAction(nameof(GetUnappliedCode500Payments), new { }, newItemId);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(ex.Message);
            }
            catch (InvalidOperationException ex)
            {
                return Conflict(ex.Message);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpPost("takebacks/{id}/reject")]
        public async Task<IActionResult> RejectCode500Payment(Guid id)
        {
            try
            {
                Guid userId = TokenParser.RetrieveGuidFromToken(User);
                await _service.RejectCode500Payment(id, userId);
                return NoContent();
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(ex.Message);
            }
            catch (InvalidOperationException ex)
            {
                return Conflict(ex.Message);
            }
        }
    }
}
