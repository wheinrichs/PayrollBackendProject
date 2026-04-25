namespace PayrollBackendProject.Application.DTO
{
    /// <summary>
    /// Represents a Login request.
    /// </summary>
    /// <remarks>
    /// Passed in as apart of the login flow and contains information to validate a user.
    /// </remarks>
    public class LoginRequestDTO
    {
        /// <summary>
        /// The email that the user has associated with the account for login. 
        /// </summary>
        public string Email { get; set; } = string.Empty;
        /// <summary>
        /// The password the user has associated with the account for login. 
        /// </summary>
        public string Password { get; set; } = string.Empty;

        /// <summary>
        /// Initializes a new instance of the <see cref="LoginRequestDTO"/> class.
        /// </summary>
        /// <param name="email">Email to login with</param>
        /// <param name="password">Password to login with</param>
        public LoginRequestDTO(string email, string password)
        {
            Email = email;
            Password = password;
        }
    }
}
