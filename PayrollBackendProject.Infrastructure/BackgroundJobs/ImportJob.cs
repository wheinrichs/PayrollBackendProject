using Hangfire;
using PayrollBackendProject.Application.DTO;
using PayrollBackendProject.Application.Interfaces.Repository;
using PayrollBackendProject.Application.Interfaces.Services;
using PayrollBackendProject.Domain.Entity;
using System.Text.Json;

namespace PayrollBackendProject.Application.Services
{
    public class ImportJob : IImportJob
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IPaymentRepository _paymentRepository;
        private readonly ICsvParserService _csvParserService;
        private readonly IAuditLogRepository _auditLogRepository;

        public ImportJob(IUnitOfWork unitOfWork, IPaymentRepository paymentRepository, ICsvParserService csvParserService, IAuditLogRepository auditLogRepository)
        {
            _unitOfWork = unitOfWork;
            _paymentRepository = paymentRepository;
            _csvParserService = csvParserService;
            _auditLogRepository = auditLogRepository;
        }

        [AutomaticRetry(Attempts = 3, DelaysInSeconds = [60, 180, 600])]
        public async Task ProcessBatch(Guid batchId)
        {
            // Retrieve the batch and mark it as processing 
            var batch = await _paymentRepository.GetImportBatchById(batchId);
            if (batch == null)
            {
                throw new Exception("Batch is null");
            }
            // Create the log snapshots
            string originalState = JsonSerializer.Serialize(batch);
            batch.UpdateStatus(Domain.Enums.ImportStatusEnum.PROCESSING);
            string updatedState = JsonSerializer.Serialize(batch);
            AuditLog log = new("Import Batch", batchId, Domain.Enums.AuditLogActionEnum.UPDATED, originalState, updatedState, "Hangfire");
            await _auditLogRepository.AddAuditLog(log);
            await _unitOfWork.SaveChangesAsync();
            // Attempt to parse the batch
            try
            {
                var result = await _csvParserService.Parse(batch);
                if (result == null)
                {
                    batch.UpdateStatus(Domain.Enums.ImportStatusEnum.FAILED);
                }
                else
                {
                    batch.SetResults(result.FailedRows, result.SuccessfulRows, result.TotalRows, result.UnresolvedRows, result.Errors);
                }
            }
            catch
            {
                batch.UpdateStatus(Domain.Enums.ImportStatusEnum.FAILED);
                await _unitOfWork.SaveChangesAsync();
                throw;
            }
            string finalState = JsonSerializer.Serialize(batch);
            log = new("Import Batch", batchId, Domain.Enums.AuditLogActionEnum.UPDATED, updatedState, finalState, "Hangfire");
            await _auditLogRepository.AddAuditLog(log);
            await _unitOfWork.SaveChangesAsync();
        }
    }
}
