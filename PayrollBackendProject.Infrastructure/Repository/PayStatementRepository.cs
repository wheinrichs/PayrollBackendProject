using Microsoft.EntityFrameworkCore;
using PayrollBackendProject.Application.Interfaces.Repository;
using PayrollBackendProject.Domain.Entity;
using PayrollBackendProject.Infrastructure.Data;

namespace PayrollBackendProject.Infrastructure.Repository
{
    public class PayStatementRepository : IPayStatementRepository
    {
        private readonly ClinicianDbContext _database;

        public PayStatementRepository(ClinicianDbContext database)
        {
            _database = database;
        }

        public void AddStatement(PayStatement statement)
        {
            _database.PayStatements.Add(statement);
        }

        public async Task<List<PayStatement>> GetAllPayStatements()
        {
            return await _database.PayStatements.ToListAsync();
        }

        public async Task<PayStatement?> GetPayStatement(Guid id)
        {
            return await _database.PayStatements.Include(p => p.LineItems).Include(s => s.Clinician).FirstOrDefaultAsync(p => p.Id == id);
        }

        public async Task<List<PayStatement>> GetPayStatementsForPayRun(Guid id)
        {
            return await _database.PayStatements.Include(s => s.Clinician).Include(p => p.LineItems).Where(s => s.PayRunId == id).ToListAsync();
        }

        public async Task<List<PayStatement>> GetPayStatementsForUser(Guid id)
        {
            return await _database.PayStatements.Include(s => s.Clinician).Include(p => p.LineItems).Where(s => s.ClinicianId == id).ToListAsync();
        }
    }
}
