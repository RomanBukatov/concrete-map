using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ConcreteMap.Domain.Dtos;
using ConcreteMap.Infrastructure.Services;

namespace ConcreteMap.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly AuthService _authService;

        public AuthController(AuthService authService)
        {
            _authService = authService;
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterDto dto)
        {
            try
            {
                await _authService.RegisterAsync(dto);
                return Ok("Пользователь зарегистрирован. Ожидайте подтверждения администратора.");
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginDto dto)
        {
            try
            {
                var token = await _authService.LoginAsync(dto);
                return Ok(new { token });
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpPost("change-password")]
        [Authorize]
        public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordDto dto)
        {
            try
            {
                var username = User.Identity.Name;
                await _authService.ChangePasswordAsync(username, dto.OldPassword, dto.NewPassword);
                return Ok("Пароль успешно изменен");
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }
    }
}