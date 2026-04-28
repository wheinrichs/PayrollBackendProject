using Microsoft.AspNetCore.Mvc;
using PayrollBackendProject.Application.DTO;
using PayrollBackendProject.Application.Interfaces.Services;
using PayrollBackendProject.Domain.Enums;

namespace PayrollBackendProject.API.Controllers
{
    /// <summary>
    /// Provides the endpoints for logging in and signing up for the payroll service.
    /// </summary>
    /// <remarks>
    /// Supports multiple user roles during signup and returns JWT tokens upon successful authentication. 
    /// </remarks>
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _service;

        /// <summary>
        /// Initializes a new instance of the <see cref="AuthController"/> class. 
        /// </summary>
        /// <param name="service">
        /// Service responsible for creating new users and validating login information. 
        /// </param>
        public AuthController(IAuthService service)
        {
            _service = service;
        }

        /// <summary>
        /// Attempts to login a user with a passed in <see cref="LoginRequestDTO"/> object.  
        /// </summary>
        /// <param name="userAccountDTO">A DTO object containing an email and password.</param>
        /// <returns>
        /// A <see cref="LoginResponseDTO"/> object that contains the token and user information. 
        /// </returns>
        /// <response code="200"> Successfully logged in the user. </response>
        /// <response code="401"> Failed log in - wrong username or password.</response>
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

        /// <summary>
        /// Registers a new user account with the specified role.
        /// </summary>
        /// <param name="newUserAccountDTO">
        /// Contains email, password, name, and role information.
        /// </param>
        /// <returns>
        /// A <see cref="LoginResponseDTO"/> for the newly created user.
        /// </returns>
        /// <response code="200">User successfully created.</response>
        /// <response code="400">Invalid role or signup failure.</response>
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
