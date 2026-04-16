using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using PayrollBackendProject.Application.Interfaces.Repository;
using PayrollBackendProject.Domain.Entity;
using PayrollBackendProject.Infrastructure.Data;

namespace PayrollBackendProject.Infrastructure.Repository
{
    public class ClinicianRepository : IClinicianRepository
    {
        private readonly ClinicianDbContext _database;

        public ClinicianRepository(ClinicianDbContext database)
        {
            _database = database;
        }

        async public void AddClinician(Clinician clinician)
        {
            _database.Clinicians.Add(clinician);
        }

        public async Task<List<Clinician>> GetAllClinicians()
        {
            return await _database.Clinicians.ToListAsync<Clinician>();
        }

        public async Task<Clinician?> GetClinicianByEmail(string email)
        {
            return await _database.Clinicians.FirstOrDefaultAsync(clinician => clinician.Email == email);
        }

        public async Task<Clinician?> GetClinicianByFullName(string firstName, string lastName)
        {
            Clinician? clinician = await _database.Clinicians.FirstOrDefaultAsync(c => c.FirstName == firstName && c.LastName == lastName);
            return clinician;
        }

        public async Task<Clinician?> GetClinicianByID(Guid ID)
        {
            return await _database.Clinicians.FindAsync(ID);
        }

        public async Task<List<Clinician>> GetClinicianByLastName(string lastName)
        {
            return await _database.Clinicians.Where(clinician => clinician.LastName == lastName).ToListAsync();
        }

        public async Task<bool> RemoveClinicianByID(Guid ID)
        {
            Clinician? toRemove = await _database.Clinicians.FindAsync(ID);
            if (toRemove == null)
            {
                return false;
            }
            _database.Clinicians.Remove(toRemove);
            return true;
        }

    }
}
