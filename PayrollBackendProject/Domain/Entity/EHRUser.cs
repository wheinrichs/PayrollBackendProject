namespace PayrollBackendProject.Domain.Entity
{
    public class EHRUser
    {
        public Guid Id { get; set; }
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string EHRUsername { get; set; } = string.Empty;
        public Clinician? Clinician { get; set; } = null;
        public UserAccount? UserAccount { get; set; } = null;

        public EHRUser() { }

        public EHRUser(string firstName, string lastName, string eHRUsername)
        {
            Id = Guid.NewGuid();
            FirstName = firstName;
            LastName = lastName;
            EHRUsername = eHRUsername;
        }
    }
}
