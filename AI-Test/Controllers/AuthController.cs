using AI_Test.DTOs;
using AI_Test.Services;
using Microsoft.AspNetCore.Mvc;

namespace AI_Test.Controllers
{
    [ApiController]
    [Route("auth")]
    public class AuthController : ControllerBase
    {
        private readonly AuthService _authService;

        public AuthController(AuthService authService)
        {
            _authService = authService;
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register(RegisterRequest request)
        {
            var result = await _authService.RegisterAsync(request);
            if (result == null)
                return Conflict(new { message = "Email đã được sử dụng." });

            return Ok(result);
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login(LoginRequest request)
        {
            var result = await _authService.LoginAsync(request);
            if (result == null)
                return Unauthorized(new { message = "Email hoặc mật khẩu không đúng." });

            return Ok(result);
        }
    }
}