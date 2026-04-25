using PayrollBackendProject.Domain.Enums;
using System.ComponentModel.DataAnnotations;

namespace PayrollBackendProject.Application.DTO
{
    /// <summary>
    /// Represents the request payload for creating a new user account.
    /// </summary>
    /// <remarks>
    /// Includes user credentials, personal information, and role selection.
    /// Validation attributes enforce required fields and basic input constraints.
    /// </remarks>
    public class SignUpRequestDTO
    {
        /// <summary>
        /// The user's email address.
        /// </summary>
        /// <remarks>
        /// Must be a valid email format.
        /// </remarks>
        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        /// <summary>
        /// The user's password.
        /// </summary>
        /// <remarks>
        /// Must be at least 6 characters long.
        /// </remarks>
        [Required]
        [MinLength(6)]
        public string Password { get; set; } = string.Empty;

        /// <summary>
        /// The user's first name.
        /// </summary>
        [Required]
        public string FirstName { get; set; } = string.Empty;

        /// <summary>
        /// The user's last name.
        /// </summary>
        [Required]
        public string LastName { get; set; } = string.Empty;

        /// <summary>
        /// The role assigned to the user.
        /// </summary>
        /// <remarks>
        /// Must match a valid value from <see cref="RoleEnum"/> (e.g., Admin, Clinician).
        /// The value is parsed case-insensitively.
        /// </remarks>
        [Required]
        public string Role { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="SignUpRequestDTO"/> class.
        /// </summary>
        /// <param name="email">The user's email address.</param>
        /// <param name="password">The user's password.</param>
        /// <param name="firstName">The user's first name.</param>
        /// <param name="lastName">The user's last name.</param>
        /// <param name="role">The role assigned to the user.</param>
        public SignUpRequestDTO(
            string email,
            string password,
            string firstName,
            string lastName,
            string role)
        {
            Email = email;
            Password = password;
            FirstName = firstName;
            LastName = lastName;
            Role = role;
        }
    }
}