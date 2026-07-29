using PayrollBackendProject.Application.DTO;
using PayrollBackendProject.Application.Interfaces.Repository;
using PayrollBackendProject.Application.Interfaces.Services;
using PayrollBackendProject.Application.Interfaces.Utilities;
using PayrollBackendProject.Application.Mappings;
using PayrollBackendProject.Domain.Entity;
using PayrollBackendProject.Domain.Enums;

namespace PayrollBackendProject.Application.Services
{
    public class AuthService : IAuthService
    {
        private readonly IUserAccountRepository _repo;
        private readonly IClinicianRepository _clinicianRepo;
        private readonly ITokenService _tokenService;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IPasswordHasher _passwordHasher;
        private readonly IAuditLogRepository _auditLogRepo;
        public AuthService(IUserAccountRepository repository, ITokenService tokenService, IUnitOfWork unitOfWork, IClinicianRepository clinicianRepo, IPasswordHasher passwordHasher, IAuditLogRepository auditLogRepo)
        {
            _repo = repository;
            _tokenService = tokenService;
            _unitOfWork = unitOfWork;
            _clinicianRepo = clinicianRepo;
            _passwordHasher = passwordHasher;
            _auditLogRepo = auditLogRepo;
        }

        public async Task<LoginResponseDTO?> Login(string username, string password)
        {
            UserAccount? retrievedUser = await _repo.GetByEmail(username);
            if (retrievedUser == null || !_passwordHasher.Verify(password, retrievedUser.PasswordHash))
            {
                return null;
            }
            if (retrievedUser.UserStatus != UserAccountApprovalStateEnum.APPROVED)
            {
                return null;
            }
            var token = _tokenService.GenerateToken(retrievedUser);
            LoginResponseDTO mappedUser = UserAccountMapper.DomainToDto(retrievedUser, token);
            return mappedUser;
        }

        public async Task<SignUpResponseDTO?> SignUp(SignUpRequestDTO newUser, RoleEnum role)
        {
            // Check if an existing user is already associated with this email
            var existingUser = await _repo.GetByEmail(newUser.Email);
            if (existingUser != null)
            {
                return null;
            }

            // Create the new user if one does not already exist
            UserAccount domainNewUser;
            // If the user type is clinician also create the clinician object to store payroll information 
            if (role == RoleEnum.CLINICIAN)
            {
                Clinician? existingClinician = await _clinicianRepo.GetClinicianByEmail(newUser.Email);
                if (existingClinician != null)
                {
                    domainNewUser = UserAccountMapper.SignUpClinicianDtoToDomain(newUser, existingClinician);
                }
                else
                {
                    // Create a new clinician
                    Clinician newClinician = new(newUser.FirstName, newUser.LastName, newUser.Email);

                    // Add a new clinician to the repo with the bare information
                    _clinicianRepo.AddClinician(newClinician);

                    domainNewUser = UserAccountMapper.SignUpClinicianDtoToDomain(newUser, newClinician);

                }
            }
            else
            {
                domainNewUser = UserAccountMapper.SignUpDtoToDomainBackend(newUser, role);
            }
            domainNewUser.PasswordHash = _passwordHasher.Hash(newUser.Password);
            await _repo.SignUp(domainNewUser);
            await _unitOfWork.SaveChangesAsync();
            return new SignUpResponseDTO(domainNewUser.Email, domainNewUser.UserStatus.ToString(), "Account created and pending admin approval.");
        }

        public async Task ApprovePendingUserAccount(Guid id)
        {
            var existingUser = await _repo.GetById(id);
            if (existingUser == null)
            {
                throw new DirectoryNotFoundException("User account not found.");
            }
            existingUser.UpdateUserAccountStatus(UserAccountApprovalStateEnum.APPROVED);
            await _unitOfWork.SaveChangesAsync();
        }

        public async Task DisableUserAccount(Guid id)
        {
            var existingUser = await _repo.GetById(id);
            if (existingUser == null)
            {
                throw new DirectoryNotFoundException("User account not found.");
            }
            existingUser.UpdateUserAccountStatus(UserAccountApprovalStateEnum.DENIED);
            await _unitOfWork.SaveChangesAsync();
        }

        public async Task<List<UserAccountDTO>> GetPendingUserAccounts()
        {
            List<UserAccount> userAccounts = await _repo.GetPendingUserAccounts();
            return userAccounts.Select(u => UserAccountMapper.UserAccountToDto(u)).ToList();
        }

        public async Task<List<UserAccountDTO>> GetAllUserAccounts()
        {
            List<UserAccount> userAccounts = await _repo.GetAllUserAccounts();
            return userAccounts.Select(u => UserAccountMapper.UserAccountToDto(u)).ToList();
        }

        public async Task UpdateUserRole(Guid id, RoleEnum newRole, Guid actorId)
        {
            if (id == actorId)
            {
                throw new InvalidOperationException("You cannot change your own role.");
            }

            UserAccount? existingUser = await _repo.GetById(id);
            if (existingUser == null)
            {
                throw new KeyNotFoundException("User account not found.");
            }

            string oldRole = existingUser.Role.ToString();
            existingUser.UpdateRole(newRole);

            // Role changes are permission changes so record who changed what in the audit log
            AuditLog roleChangeLog = new("User Account", existingUser.Id, AuditLogActionEnum.UPDATED, oldRole, newRole.ToString(), actorId.ToString());
            await _auditLogRepo.AddAuditLog(roleChangeLog);
            await _unitOfWork.SaveChangesAsync();
        }
    }
}
