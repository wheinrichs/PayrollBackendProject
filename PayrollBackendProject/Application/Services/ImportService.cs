using PayrollBackendProject.Application.DTO;
using PayrollBackendProject.Application.Interfaces.Repository;
using PayrollBackendProject.Application.Interfaces.Services;
using PayrollBackendProject.Application.Mappings;
using PayrollBackendProject.Application.Utilities;
using PayrollBackendProject.Domain.Entity;
using PayrollBackendProject.Domain.Enums;
using System.Text.Json;

namespace PayrollBackendProject.Application.Services
{
    public class ImportService : IImportService
    {
        private readonly IPaymentRepository _repo;
        private readonly IClinicianRepository _clinicianRepo;
        private readonly IUserAccountRepository _userRepo;
        private readonly IEHRUserAccountRepository _ehrUserRepo;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IAuditLogRepository _auditLogRepo;

        // Inject the service to access the current directory of the folder you're running in
        private readonly IWebHostEnvironment _env;

        public ImportService(IPaymentRepository repo, IClinicianRepository clinicianRepo, IUserAccountRepository userRepo, IEHRUserAccountRepository ehrRepo, IUnitOfWork unitOfWork, IWebHostEnvironment env, IAuditLogRepository auditLogRepo)
        {
            _repo = repo;
            _clinicianRepo = clinicianRepo;
            _userRepo = userRepo;
            _ehrUserRepo = ehrRepo;
            _unitOfWork = unitOfWork;
            _env = env;
            _auditLogRepo = auditLogRepo;
        }

        public async Task<Guid> CreateBatchAndStoreFile(IFormFile file, Guid userId)
        {
            // Create the batch - open the stream and create a fingerprint to check if its added already
            var inputStream = file.OpenReadStream();
            string batchFingerprint = await FingerprintGenerator.FileComputeSHA256Async(inputStream);
            ImportBatch? batch = await _repo.GetImportBatch(batchFingerprint);
            if (batch != null)
            {
                return batch.Id;
            }
            else
            {
                // Create a new batch
                batch = new ImportBatch(file.FileName, batchFingerprint);
                // Store the file so it can be processed later
                string filepath = CreateFilepath(batch.Id);
                batch.AssignFilepath(filepath);

                using (var writeStream = new FileStream(filepath, FileMode.Create))
                {
                    await file.CopyToAsync(writeStream);
                }
                AuditLog log = new("Import Batch", batch.Id, AuditLogActionEnum.CREATED, "", JsonSerializer.Serialize(batch), userId.ToString());
                await _repo.AddBatchItem(batch);
                await _auditLogRepo.AddAuditLog(log);
                await _unitOfWork.SaveChangesAsync();

                return batch.Id;
            }
        }

        public async Task<UploadResult?> GetBatchStatus(Guid batchId)
        {
            ImportBatch? batch = await _repo.GetImportBatchById(batchId);
            if(batch != null)
            {
                UploadResult result = new UploadResult(batch.Filename, batch.TotalRows, batch.FailedItems, batch.UnresolvedRows, batch.Errors);
                return result;
            }
            return null;
        }

        public async Task<List<PaymentLineItemDTO>> GetUnresolvedClinicianPayments()
        {
            List<PaymentLineItem> domainList = await _repo.GetPaymentsWithUnresolvedClinician();
            return domainList.Select(p => PaymentLineItemMapper.DomainToDto(p)).ToList();
        }

        private string CreateFilepath(Guid batchId)
        {
            // Create a path for the uploads in a folder called uploads
            // TODO LEGITIMIZE THIS WHEN YOU DEPLOY BUT FINE FOR LOCAL DEV
            var uploadPath = "uploads";
            var uploadsFolder = Path.Combine(_env.ContentRootPath, uploadPath);

            // If the directory does not exist then create the directory
            if(!Directory.Exists(uploadsFolder))
            {
                Directory.CreateDirectory(uploadsFolder);
            }

            var fullFilepath = Path.Combine(uploadsFolder, $"{batchId}");
            return fullFilepath;
        }
    }
}
