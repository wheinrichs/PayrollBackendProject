using Microsoft.EntityFrameworkCore;
using PayrollBackendProject.Application.Interfaces.Repository;
using PayrollBackendProject.Domain.Entity;
using PayrollBackendProject.Infrastructure.Data;

namespace PayrollBackendProject.Infrastructure.Repository
{
    public class PayRunRepository : IPayRunRepository
    {
        private readonly ClinicianDbContext _database;

        public PayRunRepository(ClinicianDbContext database)
        {
            _database = database;
        }

        public void AddPayRun(PayRun payRun)
        {
            _database.PayRuns.AddAsync(payRun);
        }

        public async Task<PayRun?> GetPayRun(Guid id)
        {
            return await _database.PayRuns.FindAsync(id);
        }

        public async Task<List<PayRun>> GetAllPayRuns()
        {
            return await _database.PayRuns.ToListAsync<PayRun>();
        }
    }
}
