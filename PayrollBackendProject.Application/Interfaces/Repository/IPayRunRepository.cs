using PayrollBackendProject.Domain.Entity;

namespace PayrollBackendProject.Application.Interfaces.Repository
{
    public interface IPayRunRepository
    {
        public void AddPayRun(PayRun payRun);
        public Task<PayRun?> GetPayRun(Guid id);
        public Task<List<PayRun>> GetAllPayRuns();
        public Task<List<PayRun>> GetOverlappingPayRuns(DateTime start, DateTime end);
    }
}
