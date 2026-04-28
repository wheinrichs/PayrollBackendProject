namespace PayrollBackendProject.Application.DTO
{
    /// <summary>
    /// Represents clinician data returned by the API.
    /// </summary>
    /// <remarks>
    /// This DTO is used in responses when retrieving clinician information.
    /// </remarks>
    public class ClinicianResponseDTO
    {
        /// <summary>
        /// Gets the clinician's first name.
        /// </summary>
        public string FirstName { get; private set; } = string.Empty;

        /// <summary>
        /// Gets the clinician's last name.
        /// </summary>
        public string LastName { get; private set; } = string.Empty;

        /// <summary>
        /// Gets the clinician's email address.
        /// </summary>
        public string Email { get; private set; } = string.Empty;

        /// <summary>
        /// Gets the unique identifier for the clinician.
        /// </summary>
        public Guid ID { get; private set; }

        /// <summary>
        /// Gets a value indicating whether the clinician provides psychiatric services today.
        /// </summary>
        public bool HasPsychToday { get; private set; }

        /// <summary>
        /// Gets the clinician's cost share value used for payroll calculations.
        /// </summary>
        public double CostShare { get; private set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="ClinicianResponseDTO"/> class.
        /// </summary>
        public ClinicianResponseDTO() { }

        /// <summary>
        /// Initializes a new instance of the <see cref="ClinicianResponseDTO"/> class with all properties.
        /// </summary>
        /// <param name="firstName">The clinician's first name.</param>
        /// <param name="lastName">The clinician's last name.</param>
        /// <param name="email">The clinician's email address.</param>
        /// <param name="iD">The unique identifier of the clinician.</param>
        /// <param name="hasPsychToday">Indicates whether the clinician provides psychiatric services today.</param>
        /// <param name="costShare">The clinician's cost share value used in payroll calculations.</param>
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