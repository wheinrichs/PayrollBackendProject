using PayrollBackendProject.Domain.Entity;

namespace PayrollBackendProject.Application.Interfaces.Repository
{
    public interface IPayStatementRepository
    {
        public void AddStatement(PayStatement statement);
        public Task<PayStatement?> GetPayStatement(Guid id);
        public Task<List<PayStatement>> GetAllPayStatements();
        public Task<List<PayStatement>> GetPayStatementsForPayRun(Guid id);
        public Task<List<PayStatement>> GetPayStatementsForUser(Guid id);
    }
}
