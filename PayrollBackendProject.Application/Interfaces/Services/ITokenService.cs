using PayrollBackendProject.Domain.Entity;

namespace PayrollBackendProject.Application.Interfaces.Services
{
    public interface ITokenService
    {
        string GenerateToken(UserAccount user);
    }
}
