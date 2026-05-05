using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PayrollBackendProject.API.Helper;
using PayrollBackendProject.Application.DTO;
using PayrollBackendProject.Application.Interfaces.Services;

namespace PayrollBackendProject.API.Controllers
{
    /// <summary>
    /// Provides endpoints for uploading and processing CSV files, as well as managing import batches.
    /// </summary>
    /// <remarks>
    /// This controller handles file ingestion, background processing of uploaded data, 
    /// and resolution of clinician-related payment data.
    /// Access is restricted to users with ADMIN or BACKEND roles.
    /// </remarks>
    [Authorize(Policy = "ApprovedBackendOnly, ApprovedAdminOnly")]
    [Route("api/[controller]")]
    [ApiController]
    public class ImportController : ControllerBase
    {
        private readonly IImportService _service;
        private readonly IBackgroundJobService _jobService;
        private readonly ICsvParserService _csvService;

        /// <summary>
        /// Provides access to environment-specific properties such as the content root path.
        /// </summary>
        private readonly IWebHostEnvironment _env;

        /// <summary>
        /// Initializes a new instance of the <see cref="ImportController"/> class.
        /// </summary>
        /// <param name="service">Service responsible for import batch creation and tracking.</param>
        /// <param name="jobService">Service responsible for enqueuing background processing jobs.</param>
        /// <param name="csvService">Service responsible for parsing and resolving CSV data.</param>
        /// <param name="env">Provides access to environment configuration such as file storage paths.</param>
        public ImportController(
            IImportService service,
            IBackgroundJobService jobService,
            ICsvParserService csvService,
            IWebHostEnvironment env)
        {
            _service = service;
            _jobService = jobService;
            _csvService = csvService;
            _env = env;
        }

        /// <summary>
        /// Uploads a CSV file and initiates background processing.
        /// </summary>
        /// <param name="file">The CSV file to upload.</param>
        /// <returns>The unique identifier of the created import batch.</returns>
        /// <response code="202">Returns the batch ID for tracking the import process.</response>
        /// <response code="400">If the file is not a valid CSV.</response>
        [HttpPost("upload")]
        public async Task<ActionResult<Guid?>> UploadCsv(IFormFile file)
        {
            if (Path.GetExtension(file.FileName) != ".csv")
            {
                return BadRequest("File must be a csv");
            }

            // Retrieve the current user ID from the authentication token
            Guid currentUserGuid = TokenParser.RetrieveGuidFromToken(User);

            // Store the uploaded file and create a batch record
            Stream fileStream = file.OpenReadStream();
            var batchId = await _service.CreateBatchAndStoreFile(
                fileStream,
                file.FileName,
                _env.ContentRootPath,
                currentUserGuid
            );

            // Enqueue background job to process the uploaded file
            _jobService.EnqueueImportBatchParsingJob(batchId);

            return Accepted(batchId);
        }

        /// <summary>
        /// Retrieves the status and results of a previously uploaded batch.
        /// </summary>
        /// <param name="batchId">The unique identifier of the import batch.</param>
        /// <returns>The status and result of the batch processing.</returns>
        /// <response code="200">Returns the batch status and results.</response>
        /// <response code="404">If the batch is not found.</response>
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

        /// <summary>
        /// Retrieves all payment line items that could not be matched to a clinician.
        /// </summary>
        /// <returns>A list of unresolved payment line items.</returns>
        /// <response code="200">Returns the list of unresolved payment line items.</response>
        [HttpGet("UnresolvedClinicianPayments")]
        public async Task<ActionResult<List<PaymentLineItemDTO>>> GetUnresolvedClinicianPayments()
        {
            return Ok(await _service.GetUnresolvedClinicianPayments());
        }

        /// <summary>
        /// Attempts to resolve clinician matches for unresolved payment line items.
        /// </summary>
        /// <returns>The result of the clinician matching process.</returns>
        /// <response code="200">Returns the results of the matching operation.</response>
        [HttpPost("ResolveCliniciansForPayments")]
        public async Task<ActionResult<ClinicianMatchResult>> ResolveCliniciansForPaymentLineItems()
        {
            return Ok(await _csvService.MatchUnresolvedPaymentLineItems());
        }
    }
}