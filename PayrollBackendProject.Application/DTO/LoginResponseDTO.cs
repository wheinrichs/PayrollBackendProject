namespace PayrollBackendProject.Application.DTO
{
    /// <summary>
    /// Represents the response returned after a successful authentication request.
    /// </summary>
    /// <remarks>
    /// Contains user identity information along with a JWT token used to authenticate
    /// subsequent requests to protected API endpoints.
    /// </remarks>
    public class LoginResponseDTO
    {
        /// <summary>
        /// The authenticated user's email address.
        /// </summary>
        public string Email { get; set; } = string.Empty;

        /// <summary>
        /// The role assigned to the user (e.g., Admin, Clinician).
        /// </summary>
        public string Role { get; set; }

        /// <summary>
        /// The user's first name.
        /// </summary>
        public string FirstName { get; set; } = string.Empty;

        /// <summary>
        /// The user's last name.
        /// </summary>
        public string LastName { get; set; } = string.Empty;

        /// <summary>
        /// A JSON Web Token (JWT) used for authenticating future API requests.
        /// </summary>
        /// <remarks>
        /// This token should be included in the Authorization header as a Bearer token:
        /// <c>Authorization: Bearer &lt;token&gt;</c>.
        /// </remarks>
        public string Token { get; set; } = string.Empty;

        /// <summary>
        /// Initializes a new instance of the <see cref="LoginResponseDTO"/> class.
        /// </summary>
        /// <param name="email">The authenticated user's email address.</param>
        /// <param name="role">The user's assigned role.</param>
        /// <param name="firstName">The user's first name.</param>
        /// <param name="lastName">The user's last name.</param>
        /// <param name="token">The JWT token used for authentication.</param>
        public LoginResponseDTO(
            string email,
            string role,
            string firstName,
            string lastName,
            string token)
        {
            Email = email;
            Role = role;
            FirstName = firstName;
            LastName = lastName;
            Token = token;
        }
    }
}