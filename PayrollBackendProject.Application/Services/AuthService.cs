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
        public AuthService(IUserAccountRepository repository, ITokenService tokenService, IUnitOfWork unitOfWork, IClinicianRepository clinicianRepo, IPasswordHasher passwordHasher)
        {
            _repo = repository;
            _tokenService = tokenService;
            _unitOfWork = unitOfWork;
            _clinicianRepo = clinicianRepo;
            _passwordHasher = passwordHasher;
        }

        public async Task<LoginResponseDTO?> Login(string username, string password)
        {
            UserAccount? retrievedUser = await _repo.GetByEmail(username);
            if (retrievedUser == null || !_passwordHasher.Verify(password, retrievedUser.PasswordHash))
            {
                return null;
            }
            var token = _tokenService.GenerateToken(retrievedUser);
            LoginResponseDTO mappedUser = UserAccountMapper.DomainToDto(retrievedUser, token);
            return mappedUser;
        }

        // TODO add in approval so not everyone can just sign up as an admin
        public async Task<LoginResponseDTO?> SignUp(SignUpRequestDTO newUser, RoleEnum role)
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
            var token = _tokenService.GenerateToken(domainNewUser);
            LoginResponseDTO mappedUser = UserAccountMapper.DomainToDto(domainNewUser, token);
            return mappedUser;
        }
    }
}
