using System.Security.Claims;

namespace PayrollBackendProject.API.Helper
{
    public static class TokenParser
    {
        public static Guid RetrieveGuidFromToken(ClaimsPrincipal user)
        {
            if (user == null)
            {
                throw new InvalidDataException("The token user cannot be null when retrieving the Guid");
            }
            var value = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (value == null)
            {
                throw new InvalidDataException("ID not included in token.");
            }
            return Guid.Parse(value);
        }
    }
}
