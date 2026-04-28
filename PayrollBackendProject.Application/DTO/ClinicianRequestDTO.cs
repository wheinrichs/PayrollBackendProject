using System.ComponentModel.DataAnnotations;

namespace PayrollBackendProject.Application.DTO
{
    /// <summary>
    /// Represents the data required to create a new clinician.
    /// </summary>
    public class ClinicianRequestDTO
    {
        /// <summary>
        /// Gets the clinician's first name.
        /// </summary>
        [Required]
        [MinLength(1)]
        public string FirstName { get; private set; } = string.Empty;

        /// <summary>
        /// Gets the clinician's last name.
        /// </summary>
        [Required]
        [MinLength(1)]
        public string LastName { get; private set; } = string.Empty;

        /// <summary>
        /// Gets the clinician's email address.
        /// </summary>
        [Required]
        [EmailAddress]
        public string Email { get; private set; } = string.Empty;

        /// <summary>
        /// Gets a value indicating whether the clinician provides psychiatric services today.
        /// </summary>
        public bool HasPsychToday { get; private set; }

        /// <summary>
        /// Gets the clinician's cost share value used for payroll calculations.
        /// </summary>
        [Range(0, 1)]
        public double CostShare { get; private set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="ClinicianRequestDTO"/> class.
        /// </summary>
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