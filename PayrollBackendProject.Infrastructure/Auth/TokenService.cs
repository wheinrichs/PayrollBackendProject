using Microsoft.IdentityModel.Tokens;
using PayrollBackendProject.Application.Interfaces.Services;
using PayrollBackendProject.Domain.Entity;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.Extensions.Configuration;
using System.Text;

namespace PayrollBackendProject.Application.Services
{
    public class JwtTokenService : ITokenService
    {
        private readonly IConfiguration _configuration;

        public JwtTokenService(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public string GenerateToken(UserAccount user)
        {
            // Get the section of entries from appsettings.json
            IConfiguration jwtSettings = _configuration.GetSection("Jwt");
            // Choosing to access these in different ways to practice - would not do in production
            var key = jwtSettings.GetValue<string>("Key") ?? throw new Exception("JWT key not configured");
            var issuer = jwtSettings["Issuer"];
            var audience = _configuration.GetValue<string>("Jwt:Audience");
            var expirationMinutes = int.Parse(jwtSettings["ExpirationMinutes"] ?? throw new Exception("JWT expiration not configured"));

            // Create the list of claims that will be available in the token
            // TODO: to support oauth need to add additional claim types for jwt support
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim(ClaimTypes.Role, user.Role.ToString()),
                new Claim("status", user.UserStatus.ToString())
            };

            // Generate symmetric security key from bytes of key and credentials
            var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key));
            var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

            // Create the JWT
            var token = new JwtSecurityToken(issuer, audience, claims, expires: DateTime.UtcNow.AddMinutes(expirationMinutes), signingCredentials: credentials);

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}
