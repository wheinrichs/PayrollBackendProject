using PayrollBackendProject.Application.DTO;

namespace PayrollBackendProject.Application.Interfaces.Services
{
    public interface IClinicianService
    {
        public Task<List<ClinicianResponseDTO>> GetClinicians();
        public Task<ClinicianResponseDTO> AddClinician(ClinicianRequestDTO requestedClinician);
        public Task<List<ClinicianResponseDTO>> GetClinicianByLastName(string lastName);
        public Task<bool> RemoveClinicianByID(Guid ID);
        public Task<ClinicianResponseDTO?> GetClinicianByID(Guid id);
    }
}
