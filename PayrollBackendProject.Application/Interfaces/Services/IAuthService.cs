using PayrollBackendProject.Application.DTO;
using PayrollBackendProject.Domain.Enums;

namespace PayrollBackendProject.Application.Interfaces.Services
{
    public interface IAuthService
    {
        public Task<LoginResponseDTO?> Login(string username, string password);
        public Task<SignUpResponseDTO?> SignUp(SignUpRequestDTO newUser, RoleEnum role);
        public Task<List<UserAccountDTO>> GetPendingUserAccounts();
        public Task ApprovePendingUserAccount(Guid id);
        public Task DisableUserAccount(Guid id);
    }
}
