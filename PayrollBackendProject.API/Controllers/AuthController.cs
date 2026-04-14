using Microsoft.AspNetCore.Mvc;
using PayrollBackendProject.Application.DTO;
using PayrollBackendProject.Application.Interfaces.Services;
using PayrollBackendProject.Domain.Enums;

namespace PayrollBackendProject.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _service;

        public AuthController(IAuthService service)
        {
            _service = service;
        }

        [HttpPost("login")]
        public async Task<ActionResult<LoginResponseDTO?>> Login(LoginRequestDTO userAccountDTO)
        {
            var user = await _service.Login(userAccountDTO.Email, userAccountDTO.Password);
            if (user == null)
            {
                return Unauthorized("Invalid username or password");
            }
            return Ok(user);
        }

        [HttpPost("signup")]
        public async Task<ActionResult<LoginResponseDTO?>> Signup(SignUpRequestDTO newUserAccountDTO)
        {
            if (!Enum.TryParse<RoleEnum>(newUserAccountDTO.Role, ignoreCase: true, out var role))
            {
                return BadRequest($"Invalid role: {newUserAccountDTO.Role}");
            }

            LoginResponseDTO? newUser = await _service.SignUp(newUserAccountDTO, role);
            if (newUser == null)
            {
                return BadRequest("Unable to create new user.");
            }
            return Ok(newUser);
        }
    }
}
