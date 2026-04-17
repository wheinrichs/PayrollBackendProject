using PayrollBackendProject.Application.DTO;
using PayrollBackendProject.Application.Interfaces.Repository;
using PayrollBackendProject.Application.Interfaces.Services;
using PayrollBackendProject.Application.Interfaces.Utilities;
using PayrollBackendProject.Application.Mappings;
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
        private readonly IFingerprintGenerator _fingerprintGenerator;
        private readonly IFileHandler _fileHandler;

        public ImportService(IPaymentRepository repo, 
            IClinicianRepository clinicianRepo, 
            IUserAccountRepository userRepo, 
            IEHRUserAccountRepository ehrRepo, 
            IUnitOfWork unitOfWork, 
            IAuditLogRepository auditLogRepo, 
            IFingerprintGenerator fingerprintGenerator,
            IFileHandler fileHandler)
        {
            _repo = repo;
            _clinicianRepo = clinicianRepo;
            _userRepo = userRepo;
            _ehrUserRepo = ehrRepo;
            _unitOfWork = unitOfWork;
            _auditLogRepo = auditLogRepo;
            _fingerprintGenerator = fingerprintGenerator;
            _fileHandler = fileHandler;
        }

        public async Task<Guid> CreateBatchAndStoreFile(Stream fileStream, string filename, string uploadRoot, Guid userId)
        {
            // Create the batch - open the stream and create a fingerprint to check if its added already
            var inputStream = fileStream;
            string batchFingerprint = await _fingerprintGenerator.FileComputeSHA256Async(inputStream);
            inputStream.Position = 0;
            ImportBatch? batch = await _repo.GetImportBatch(batchFingerprint);
            if (batch != null)
            {
                return batch.Id;
            }
            else
            {
                // Create a new batch
                batch = new ImportBatch(filename, batchFingerprint);

                string filepath = await _fileHandler.WriteFile(fileStream, filename, uploadRoot, batch.Id);

                batch.AssignFilepath(filepath);
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
    }
}
