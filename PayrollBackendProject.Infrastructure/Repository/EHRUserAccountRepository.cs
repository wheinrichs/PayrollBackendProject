using Microsoft.EntityFrameworkCore;
using PayrollBackendProject.Application.Interfaces.Repository;
using PayrollBackendProject.Domain.Entity;
using PayrollBackendProject.Infrastructure.Data;

namespace PayrollBackendProject.Infrastructure.Repository
{
    public class EHRUserAccountRepository : IEHRUserAccountRepository
    {
        private readonly ClinicianDbContext _database;

        public EHRUserAccountRepository(ClinicianDbContext database)
        {
            _database = database;
        }

        public async Task<List<EHRUser>> GetAllUsers()
        {
            return await _database.EHRUsers.ToListAsync();
        }

        public void AddNewUser(EHRUser user)
        {
            _database.EHRUsers.Add(user);
        }

        public async Task<EHRUser?> GetUserById(Guid id)
        {
            EHRUser? retrievedUser = await _database.EHRUsers.FindAsync(id);
            return retrievedUser;
        }

        public async Task<EHRUser?> GetUserByUsername(string username)
        {
            EHRUser? retrievedUser = await _database.EHRUsers.FirstOrDefaultAsync(u => u.EHRUsername == username);
            return retrievedUser;
        }
    }
}
