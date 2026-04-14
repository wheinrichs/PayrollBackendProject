using PayrollBackendProject.Application.Interfaces.Repository;
using PayrollBackendProject.Infrastructure.Data;

namespace PayrollBackendProject.Infrastructure.Repository
{
    public class UnitOfWork : IUnitOfWork
    {
        private readonly ClinicianDbContext _database;
        public UnitOfWork(ClinicianDbContext database)
        {
            _database = database;
        }

        public Task SaveChangesAsync()
        {
            return _database.SaveChangesAsync();
        }
    }
}
