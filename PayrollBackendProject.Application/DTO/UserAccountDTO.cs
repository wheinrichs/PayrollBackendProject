using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PayrollBackendProject.Application.DTO
{
    /// <summary>
    /// A DTO object that represents the information and the current status of a user's account.
    /// </summary>
    public class UserAccountDTO
    {
        /// <summary>
        /// The user ID for the pending account.
        /// </summary>
        public Guid UserId { get; private set; }
        
        /// <summary>
        /// The authenticated user's email address.
        /// </summary>
        public string Email { get; private set; } = string.Empty;

        /// <summary>
        /// The role assigned to the user (e.g., Admin, Clinician).
        /// </summary>
        public string Role { get; private set; } = string.Empty;

        /// <summary>
        /// The user's first name.
        /// </summary>
        public string FirstName { get; private set; } = string.Empty;

        /// <summary>
        /// The user's last name.
        /// </summary>
        public string LastName { get; private set; } = string.Empty;

        /// <summary>
        /// The current Users approval status.
        /// </summary>
        public string UserStatus { get; private set; } = string.Empty;

        /// <summary>
        /// Constructor for the user account DTO
        /// </summary>
        /// <param name="userId">User account ID</param>
        /// <param name="email">User account email</param>
        /// <param name="role">User account role</param>
        /// <param name="firstName">User account first name</param>
        /// <param name="lastName">User account last name</param>
        /// <param name="userStatus">Status of the current user account.</param>
        public UserAccountDTO (
            Guid userId,
            string email,
            string role,
            string firstName,
            string lastName,
            string userStatus)
        {
            UserId = userId;
            Email = email;
            Role = role;
            FirstName = firstName;
            LastName = lastName;
            UserStatus = userStatus;
        }
    }
}
