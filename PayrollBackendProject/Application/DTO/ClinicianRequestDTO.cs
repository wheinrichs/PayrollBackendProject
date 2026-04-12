namespace PayrollBackendProject.Application.DTO
{
    public class ClinicianRequestDTO
    {
        public string FirstName { get; private set; } = string.Empty;
        public string LastName { get; private set; } = string.Empty;
        public string Email { get; private set; } = string.Empty;
        public bool HasPsychToday { get; private set; }
        public double CostShare { get; private set; }

        public ClinicianRequestDTO(string firstName, string lastName, string email, bool hasPsychToday, double costShare)
        {
            FirstName = firstName;
            LastName = lastName;
            Email = email;
            HasPsychToday = hasPsychToday;
            CostShare = costShare;
        }
    }
}
