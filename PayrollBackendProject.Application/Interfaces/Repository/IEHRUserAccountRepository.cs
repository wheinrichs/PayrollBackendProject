using PayrollBackendProject.Domain.Entity;

namespace PayrollBackendProject.Application.Interfaces.Repository
{
    public interface IEHRUserAccountRepository
    {
        public Task<List<EHRUser>> GetAllUsers();
        public Task<EHRUser?> GetUserByUsername(string username);
        public Task<EHRUser?> GetUserById(Guid id);
        public void AddNewUser(EHRUser user);
    }
}
