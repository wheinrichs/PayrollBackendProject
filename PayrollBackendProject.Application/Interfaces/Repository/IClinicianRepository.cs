using PayrollBackendProject.Domain.Entity;

namespace PayrollBackendProject.Application.Interfaces.Repository
{
    public interface IClinicianRepository
    {
        public void AddClinician(Clinician clinician);
        public Task<List<Clinician>> GetClinicianByLastName(string name);
        public Task<List<Clinician>> GetAllClinicians();
        public Task<bool> RemoveClinicianByID(Guid ID);
        public Task<Clinician> GetClinicianByID(Guid ID);
        public Task<Clinician?> GetClinicianByFullName(string firstName, string lastName);
        public Task<Clinician?> GetClinicianByEmail(string email);

    }
}
