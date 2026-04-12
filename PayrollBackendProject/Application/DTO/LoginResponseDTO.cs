namespace PayrollBackendProject.Application.DTO
{
    public class LoginResponseDTO
    {
        public string Email { get; set; } = string.Empty;
        public string Role { get; set; }
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string Token { get; set; } = string.Empty;

        public LoginResponseDTO(string email, string role, string firstName, string lastName, string token)
        {
            Email = email;
            Role = role;
            FirstName = firstName;
            LastName = lastName;
            Token = token;
        }
    }
}
