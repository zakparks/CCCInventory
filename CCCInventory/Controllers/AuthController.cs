using CCCInventory.Data;
using CCCInventory.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;

namespace CCCInventory.Controllers
{
    [Route("api/auth")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly DataContext _context;
        private readonly TokenService _tokenService;
        private readonly IWebHostEnvironment _env;
        private readonly IConfiguration _config;

        public AuthController(DataContext context, TokenService tokenService, IWebHostEnvironment env, IConfiguration config)
        {
            _context = context;
            _tokenService = tokenService;
            _env = env;
            _config = config;
        }

        public record LoginRequest(string Username, string Password);
        public record ChangePasswordRequest(string CurrentPassword, string NewPassword);

        [HttpPost("login")]
        [AllowAnonymous]
        [EnableRateLimiting("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequest request)
        {
            var user = await _context.AppUsers
                .FirstOrDefaultAsync(u => u.Username == request.Username);

            if (user == null || !BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
                return Unauthorized(new { message = "Invalid username or password." });

            var token = _tokenService.GenerateToken(user);
            var expiryDays = int.TryParse(_config["Jwt:ExpiryDays"], out var d) ? d : 30;

            Response.Cookies.Append("auth_token", token, new CookieOptions
            {
                HttpOnly = true,
                Secure = !_env.IsDevelopment(),
                SameSite = SameSiteMode.Strict,
                Expires = DateTimeOffset.UtcNow.AddDays(expiryDays),
                Path = "/"
            });

            return Ok(new { username = user.Username });
        }

        [HttpPost("logout")]
        [Authorize]
        public IActionResult Logout()
        {
            Response.Cookies.Append("auth_token", "", new CookieOptions
            {
                HttpOnly = true,
                Secure = !_env.IsDevelopment(),
                SameSite = SameSiteMode.Strict,
                Expires = DateTimeOffset.UtcNow.AddDays(-1),
                Path = "/"
            });
            return Ok();
        }

        [HttpGet("status")]
        [Authorize]
        public IActionResult Status()
        {
            var username = User.Identity?.Name ?? "";
            return Ok(new { isAuthenticated = true, username });
        }

        [HttpGet("config")]
        [AllowAnonymous]
        public IActionResult Config()
        {
            var minutes = int.TryParse(_config["Security:InactivityTimeoutMinutes"], out var m) ? m : 5;
            return Ok(new { inactivityTimeoutMinutes = minutes });
        }

        [HttpPost("change-password")]
        [Authorize]
        public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest request)
        {
            var user = await _context.AppUsers
                .FirstOrDefaultAsync(u => u.Username == User.Identity!.Name);

            if (user == null)
                return Unauthorized();

            if (!BCrypt.Net.BCrypt.Verify(request.CurrentPassword, user.PasswordHash))
                return BadRequest(new { message = "Current password is incorrect." });

            if (string.IsNullOrWhiteSpace(request.NewPassword) || request.NewPassword.Length < 8)
                return BadRequest(new { message = "New password must be at least 8 characters." });

            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.NewPassword);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Password changed successfully." });
        }
    }
}
