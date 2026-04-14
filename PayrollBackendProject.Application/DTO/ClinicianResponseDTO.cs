namespace PayrollBackendProject.Application.DTO
{
    public class ClinicianResponseDTO
    {
        public string FirstName { get; private set; } = string.Empty;
        public string LastName { get; private set; } = string.Empty;
        public string Email { get; private set; } = string.Empty;
        public Guid ID { get; private set; }
        public bool HasPsychToday { get; private set; }
        public double CostShare { get; private set; }

        public ClinicianResponseDTO() { }
        public ClinicianResponseDTO(string firstName, string lastName, string email, Guid iD, bool hasPsychToday, double costShare)
        {
            FirstName = firstName;
            LastName = lastName;
            Email = email;
            ID = iD;
            HasPsychToday = hasPsychToday;
            CostShare = costShare;
        }
    }
}
