using PayrollBackendProject.Application.DTO;
using PayrollBackendProject.Domain.Entity;

namespace PayrollBackendProject.Application.Mappings
{
    public static class ClinicianMapper
    {
        public static Clinician RequestToDomain(ClinicianRequestDTO clinicianReq)
        {
            return new Clinician(
                clinicianReq.FirstName,
                clinicianReq.LastName,
                clinicianReq.Email,
                clinicianReq.HasPsychToday,
                clinicianReq.CostShare);
        }

        public static ClinicianResponseDTO DomainToDTO(Clinician clinician)
        {
            return new ClinicianResponseDTO(
                clinician.FirstName,
                clinician.LastName,
                clinician.Email,
                clinician.ID,
                clinician.HasPsychToday,
                clinician.CostShare);
        }
    }
}
