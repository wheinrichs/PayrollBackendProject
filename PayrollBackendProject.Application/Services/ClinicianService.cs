using PayrollBackendProject.Application.DTO;
using PayrollBackendProject.Application.Interfaces.Repository;
using PayrollBackendProject.Application.Interfaces.Services;
using PayrollBackendProject.Application.Mappings;
using PayrollBackendProject.Domain.Entity;

namespace PayrollBackendProject.Application.Services
{
    public class ClinicianService : IClinicianService
    {
        private readonly IClinicianRepository _clinicianRepository;
        private readonly IUnitOfWork _unitOfWork;

        public ClinicianService(IClinicianRepository clinicianRepository, IUnitOfWork unitOfWork)
        {
            _clinicianRepository = clinicianRepository;
            _unitOfWork = unitOfWork;
        }

        public async Task<ClinicianResponseDTO> AddClinician(ClinicianRequestDTO requestedClinician)
        {
            Clinician? clinicianDomain = await _clinicianRepository.GetClinicianByEmail(requestedClinician.Email);
            // If the Clinician is being created before the user account has been created
            if(clinicianDomain == null)
            {
                clinicianDomain = ClinicianMapper.RequestToDomain(requestedClinician);
                _clinicianRepository.AddClinician(clinicianDomain);
            }
            // If the Clinician signed up for a user account already just update the exisitng Clinician entity
            else
            {
                clinicianDomain.UpdateCoreInformation(requestedClinician.FirstName,
                    requestedClinician.LastName,
                    requestedClinician.HasPsychToday,
                    requestedClinician.CostShare);
            }
            await _unitOfWork.SaveChangesAsync();
            return ClinicianMapper.DomainToDTO(clinicianDomain);
        }

        public async Task<ClinicianResponseDTO?> GetClinicianByLastName(string lastName)
        {
            Clinician? returnClinician = await _clinicianRepository.GetClinicianByLastName(lastName);
            return returnClinician == null ? null : ClinicianMapper.DomainToDTO(returnClinician);
        }

        public async Task<List<ClinicianResponseDTO>> GetClinicians()
        {
            List<Clinician> returnClinicians = await _clinicianRepository.GetAllClinicians();
            List<ClinicianResponseDTO> responseClinicians = returnClinicians.Select(clinician => ClinicianMapper.DomainToDTO(clinician)).ToList();
            return responseClinicians;
        }

        public async Task<bool> RemoveClinicianByID(Guid ID)
        {
            return await _clinicianRepository.RemoveClinicianByID(ID);
        }

        public async Task<ClinicianResponseDTO?> GetClinicianByID(Guid ID)
        {
            Clinician? returnClinician = await _clinicianRepository.GetClinicianByID(ID);
            return returnClinician == null ? null : ClinicianMapper.DomainToDTO(returnClinician);


        }

    }
}
