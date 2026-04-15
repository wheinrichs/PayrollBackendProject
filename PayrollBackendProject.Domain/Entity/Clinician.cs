namespace PayrollBackendProject.Domain.Entity
{
    public class Clinician
    {
        public string FirstName { get; private set; } = string.Empty;
        public string LastName { get; private set; } = string.Empty;

        public string Email { get; private set; } = string.Empty;
        public Guid ID { get; private set; }
        public bool HasPsychToday { get; private set; } = false;
        public double CostShare { get; private set; } = 0.6;

        public Clinician(string firstName, string lastName, string email, bool hasPsychToday, double costShare)
        {
            ValidateCostShare(costShare);
            ValidateName(firstName, lastName);
            ValidateEmail(email);

            FirstName = firstName;
            LastName = lastName;
            Email = email;
            HasPsychToday = hasPsychToday;
            ID = Guid.NewGuid();
            CostShare = costShare;
        }

        public Clinician(string firstName, string lastName, string email)
        {
            ValidateName(firstName, lastName);
            ValidateEmail(email);

            FirstName = firstName;
            LastName = lastName;
            Email = email;
            HasPsychToday = false;
            ID = Guid.NewGuid();
            CostShare = 0;
        }

        // This method allows for updating core information for the clinician in the event the clinician registers before their first payroll
        public void UpdateCoreInformation(string firstName, string lastName, bool hasPsychToday, double costShare)
        {
            ValidateCostShare(costShare);
            ValidateName(firstName, lastName);

            FirstName = firstName;
            LastName =lastName;
            HasPsychToday = hasPsychToday;
            CostShare = costShare;
        }

        private void ValidateCostShare(double costShare)
        {
            if (costShare >= 1 || costShare <= 0)
            {
                throw new ArgumentException("Cost share must be between 0 and 1");
            }
        }


        private void ValidateName(string firstName, string lastName)
        {
            if (string.IsNullOrWhiteSpace(firstName) || string.IsNullOrWhiteSpace(lastName))
            {
                throw new ArgumentException("Invalid name");
            }
        }

        private void ValidateEmail(string email)
        {
            if (string.IsNullOrWhiteSpace(email))
            {
                throw new ArgumentException("Invalid email");
            }

            if (!email.Contains("@"))
            {
                throw new ArgumentException("Invalid email format");
            }
        }
    }
}
