using PayrollBackendProject.Domain.Entity;

namespace PayrollBackendProject.Application.Interfaces.Repository
{
    public interface IPaymentRepository
    {
        public Task<List<string>> GetAllPaymentFingerprints();
        public Task AddLineItem(PaymentLineItem item);
        public Task AddBatchItem(ImportBatch item);
        public Task<ImportBatch?> GetImportBatch(string fingerpint);
        public Task<ImportBatch?> GetImportBatchById(Guid id);
        public Task<PaymentLineItem?> GetPaymentLineItem(string fingerpint);
        public Task<List<PaymentLineItem>> GetPaymentBetweenDates(DateTime start, DateTime end);
        public Task<List<PaymentLineItem>> GetPaymentsFromBatch(Guid batchId);
        public Task<List<PaymentLineItem>> GetPaymentsWithUnresolvedClinician();
        public Task<List<PaymentLineItem>> GetUnappliedCode500Payments();
        public Task<PaymentLineItem?> GetPaymentLineItemById(Guid id);
        public Task<List<Code500Application>> GetCode500ApplicationsBetweenDates(DateTime start, DateTime end);
        public void AddCode500Application(Code500Application application);
    }
}
