using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PayrollBackendProject.Application.DTO
{
    /// <summary>
    /// The response received after a user signs up for the first time.
    /// </summary>
    /// <remarks>
    /// Contains identity information along with a message about the current status of the account.
    /// </remarks>
    public class SignUpResponseDTO
    {
        /// <summary>
        /// The email the user was created under.
        /// </summary>
        public String Email { get; private set; }

        /// <summary>
        /// The UserRole that the user signed up under
        /// </summary>
        public String Status {  get; private set; }

        /// <summary>
        /// A message about the status of the user account. Usually notifies the user is pending approval.
        /// </summary>
        public String Message { get; private set; }

        /// <summary>
        /// Constructor for the signup reponse DTO
        /// </summary>
        /// <param name="email">Email the account was created under.</param>
        /// <param name="status">The role the user registered with.</param>
        /// <param name="message">A message about the status of the account.</param>
        public SignUpResponseDTO(
        string email,
        string status,
        string message)
            {
                Email = email;
                Status = status;
                Message = message;

            }
    }
}
