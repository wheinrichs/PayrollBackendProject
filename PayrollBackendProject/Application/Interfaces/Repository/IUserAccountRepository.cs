using PayrollBackendProject.Domain.Entity;

namespace PayrollBackendProject.Application.Interfaces.Repository
{
    public interface IUserAccountRepository
    {
        public Task<UserAccount?> GetByEmail(string email);
        public Task<UserAccount?> GetById(Guid id);
        public Task SignUp(UserAccount userAccount);
    }
}
