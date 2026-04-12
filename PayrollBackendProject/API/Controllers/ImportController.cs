using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PayrollBackendProject.API.Helper;
using PayrollBackendProject.Application.DTO;
using PayrollBackendProject.Application.Interfaces.Services;

namespace PayrollBackendProject.API.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class ImportController : ControllerBase
    {
        private readonly IImportService _service;
        private readonly IBackgroundJobService _jobService;
        private readonly ICsvParserService _csvService;

        public ImportController(IImportService service, IBackgroundJobService jobService, ICsvParserService csvService)
        {
            _service = service;
            _jobService = jobService;
            _csvService = csvService;
        }

        [HttpPost("upload")]
        public async Task<ActionResult<Guid?>> UploadCsv(IFormFile file)
        {
            if(Path.GetExtension(file.FileName) != ".csv")
            {
                return BadRequest("File must be a csv");
            }
            // Store the file and get the batchId for the background worker to process
            Guid currentUserGuid = TokenParser.RetrieveGuidFromToken(User);
            var batchId = await _service.CreateBatchAndStoreFile(file, currentUserGuid);
            // Process the job in the background
            _jobService.EnqueueImportBatchParsingJob(batchId);
            return Ok(batchId);
        }

        [HttpGet("{batchId}")]
        public async Task<ActionResult<UploadResult?>> GetBatchStatus(Guid batchId)
        {
            UploadResult? result = await _service.GetBatchStatus(batchId);
            if (result == null)
            {
                return NotFound("Batch was not found");
            }
            return Ok(result);
        }

        [HttpGet("UnresolvedClinicianPayments")]
        public async Task<ActionResult<List<PaymentLineItemDTO>>> GetUnresolvedClinicianPayments()
        {
            return Ok(await _service.GetUnresolvedClinicianPayments());
        }

        [HttpPost("ResolveCliniciansForPayments")]
        public async Task<ActionResult<ClinicianMatchResult>> ResolveCliniciansForPaymentLineItems()
        {
            return Ok(await _csvService.MatchUnresolvedPaymentLineItems());
        }
    }
}
