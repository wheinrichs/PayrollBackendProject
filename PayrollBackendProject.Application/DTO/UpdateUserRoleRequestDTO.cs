namespace PayrollBackendProject.Application.DTO
{
    /// <summary>
    /// Represents a request to change a user account's role.
    /// </summary>
    public class UpdateUserRoleRequestDTO
    {
        /// <summary>
        /// The new role for the user (e.g. "ADMIN", "BACKEND", "CLINICIAN").
        /// </summary>
        public string Role { get; set; } = string.Empty;
    }
}
