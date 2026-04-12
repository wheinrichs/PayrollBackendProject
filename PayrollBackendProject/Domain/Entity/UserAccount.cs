using PayrollBackendProject.Domain.Enums;
using System.Diagnostics;

namespace PayrollBackendProject.Domain.Entity
{
    public class UserAccount
    {
        public Guid Id { get; set; }
        public string Email { get; private set; } = string.Empty;
        public string PasswordHash { get; set; } = string.Empty;
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string PracticeMateAccountName { get; set; } = string.Empty;
        public Guid? ClinicianId { get; set; } = null;
        public Clinician? Clinician { get; set; }
        public RoleEnum Role { get; set; }

        public UserAccount() { }
        public UserAccount(string email, string password, string firstName, string lastName)
        {
            Id = Guid.NewGuid();
            Email = email;
            PasswordHash = password;
            FirstName = firstName;
            LastName = lastName;
        }

        public static UserAccount GenerateUserAccount(string email, string password, string  firstName, string lastName, RoleEnum role, Clinician? clinician)
        {
            switch(role)
            { 
                case RoleEnum.CLINICIAN:
                    {
                        if (clinician == null)
                        {
                            throw new ArgumentNullException("Cannot create a clinician user account without the clinician object.");
                        }
                        UserAccount account = new UserAccount(email, password, firstName, lastName);
                        account.Role = role;
                        account.Clinician = clinician;
                        account.ClinicianId = clinician.ID;
                        return account;
                    }

                default:
                    {
                        UserAccount account = new UserAccount(email, password, firstName, lastName);
                        account.Role = role;
                        return account;
                    }
            }
        }

    }
}
