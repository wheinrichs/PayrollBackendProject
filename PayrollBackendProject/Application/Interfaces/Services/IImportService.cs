using PayrollBackendProject.Application.DTO;

namespace PayrollBackendProject.Application.Interfaces.Services
{
    public interface IImportService
    {
        public Task<Guid> CreateBatchAndStoreFile(IFormFile file, Guid userId);
        public Task<UploadResult?> GetBatchStatus(Guid batchId);
        public Task<List<PaymentLineItemDTO>> GetUnresolvedClinicianPayments();
    }
}
