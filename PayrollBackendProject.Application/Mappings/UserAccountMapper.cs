using PayrollBackendProject.Application.DTO;
using PayrollBackendProject.Domain.Entity;
using PayrollBackendProject.Domain.Enums;

namespace PayrollBackendProject.Application.Mappings
{
    public static class UserAccountMapper
    {
        public static UserAccount DtoToDomain(LoginRequestDTO account)
        {
            return new UserAccount(account.Email, account.Password, string.Empty, string.Empty);
        }

        public static LoginResponseDTO DomainToDto(UserAccount account, string token)
        {
            return new LoginResponseDTO(account.Email, account.Role.ToString(), account.FirstName, account.LastName, token);
        }

        public static UserAccount SignUpClinicianDtoToDomain(SignUpRequestDTO account, Clinician clinician)
        {
            return UserAccount.GenerateUserAccount(account.Email, account.Password, account.FirstName, account.LastName, Domain.Enums.RoleEnum.CLINICIAN, clinician);
        }

        public static UserAccount SignUpDtoToDomainBackend(SignUpRequestDTO account, RoleEnum role)
        {
            return UserAccount.GenerateUserAccount(account.Email, account.Password, account.FirstName, account.LastName, role, null);
        }

        public static UserAccountDTO UserAccountToDto(UserAccount account)
        {
            return new UserAccountDTO(account.Id, account.Email, account.Role.ToString(), account.FirstName, account.LastName, account.UserStatus.ToString());
        }
    }
}
