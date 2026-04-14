namespace PayrollBackendProject.Application.Interfaces.Repository
{
    public interface IUnitOfWork
    {
        public Task SaveChangesAsync();
    }
}
