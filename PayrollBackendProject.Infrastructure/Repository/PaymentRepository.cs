using Microsoft.EntityFrameworkCore;
using PayrollBackendProject.Application.Interfaces.Repository;
using PayrollBackendProject.Domain.Entity;
using PayrollBackendProject.Infrastructure.Data;

namespace PayrollBackendProject.Infrastructure.Repository
{
    public class PaymentRepository : IPaymentRepository
    {
        private readonly ClinicianDbContext _database;

        public PaymentRepository(ClinicianDbContext database)
        {
            _database = database;
        }
        public async Task<List<string>> GetAllPaymentFingerprints()
        {
            return await _database.PaymentLineItems.Select(l => l.Fingerprint).ToListAsync();
        }

        public async Task AddBatchItem(ImportBatch item)
        {
            _database.ImportBatches.Add(item);
        }

        public async Task AddLineItem(PaymentLineItem item)
        {
            _database.PaymentLineItems.Add(item);
        }

        public async Task<ImportBatch?> GetImportBatch(string fingerpint)
        {
            ImportBatch? returnedBatch = await _database.ImportBatches.FirstOrDefaultAsync(b => b.Fingerprint == fingerpint);
            return returnedBatch;
        }

        public async Task<PaymentLineItem?> GetPaymentLineItem(string fingerprint)
        {
            PaymentLineItem? returnedLineItem = await _database.PaymentLineItems.FirstOrDefaultAsync(p => p.Fingerprint == fingerprint);
            return returnedLineItem;
        }

        public async Task<List<PaymentLineItem>> GetPaymentBetweenDates(DateTime start, DateTime end)
        {
            // TODO MAKE THIS TAKE THE WHOLE DAY AND NOT THE SPECIFIC TIMESTAMP
            return await _database.PaymentLineItems.Where(p => p.AppliedDate <= end && p.AppliedDate >= start).ToListAsync();
        }

        public Task<List<PaymentLineItem>> GetPaymentsFromBatch(Guid batchId)
        {
            return _database.PaymentLineItems.Where(p => p.ImportBatchId == batchId).ToListAsync();
        }

        public async Task<ImportBatch?> GetImportBatchById(Guid id)
        {
            return await _database.ImportBatches.FindAsync(id);
        }

        public async Task<List<PaymentLineItem>> GetPaymentsWithUnresolvedClinician()
        {
            return await _database.PaymentLineItems.Where(p => p.ClinicianId == null).ToListAsync();
        }
    }
}
