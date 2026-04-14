using PayrollBackendProject.Application.DTO;

namespace PayrollBackendProject.Application.Interfaces.Services
{
    public interface IImportService
    {
        public Task<Guid> CreateBatchAndStoreFile(Stream fileStream, string filename, string uploadRoot, Guid userId);
        public Task<UploadResult?> GetBatchStatus(Guid batchId);
        public Task<List<PaymentLineItemDTO>> GetUnresolvedClinicianPayments();
    }
}
