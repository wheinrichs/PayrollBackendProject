namespace PayrollBackendProject.Application.DTO
{
    public class LoginRequestDTO
    {
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;

        public LoginRequestDTO(string email, string password)
        {
            Email = email;
            Password = password;
        }
    }
}
