using Microsoft.EntityFrameworkCore.Storage;
using PayrollBackendProject.Application.Interfaces.Repository;
using PayrollBackendProject.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using PayrollBackendProject.Domain.Entity;

namespace PayrollBackendProject.Infrastructure.Repository
{
    public class UserAccountRepository : IUserAccountRepository
    {
        private readonly ClinicianDbContext _database;
        public UserAccountRepository(ClinicianDbContext database)
        {
            _database = database;
        }
        public async Task<UserAccount?> GetById(Guid id)
        {
            UserAccount? retrievedUser = await _database.Users.FindAsync(id);
            return retrievedUser;
        }

        public async Task<UserAccount?> GetByEmail(string email)
        {
            UserAccount? retrievedUser = await _database.Users.FirstOrDefaultAsync(user => user.Email == email);
            return retrievedUser;
        }

        public async Task SignUp(UserAccount userAccount)
        {
            _database.Users.Add(userAccount);
        }

        public async Task<List<UserAccount>> GetPendingUserAccounts()
        {
            return await _database.Users.Where(u => u.UserStatus == Domain.Enums.UserAccountApprovalStateEnum.PENDING_APPROVAL).ToListAsync();
        }
    }
}
