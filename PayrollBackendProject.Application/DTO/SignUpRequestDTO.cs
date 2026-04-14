using PayrollBackendProject.Domain.Enums;
using System.ComponentModel.DataAnnotations;

namespace PayrollBackendProject.Application.DTO
{
    public class SignUpRequestDTO
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required]
        [MinLength (6)]
        public string Password { get; set; } = string.Empty;

        [Required]
        public string FirstName { get; set; } = string.Empty;

        [Required]
        public string LastName { get; set; } = string.Empty;
        [Required]
        public string Role {  get; set; }

        public SignUpRequestDTO(string email, string password, string firstName, string lastName, string role)
        {
            Email = email;
            Password = password;
            FirstName = firstName;
            LastName = lastName;
            Role = role;
        }
    }
}
