// Student ID: S2401276
// Student Name: Mohamed Iyaadh Ahmed
// Module: Advanced Software Development (UFCF8S-30-2)

using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using HMS.API.DTOs.Auth;
using HMS.API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HMS.API.Controllers
{
    [ApiController]
    [Route("api/auth")]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;
        private readonly int _refreshTokenExpiryDays;

        public AuthController(IAuthService authService, IConfiguration config)
        {
            _authService = authService;
            _refreshTokenExpiryDays = int.Parse(config["JwtSettings:RefreshTokenExpiryDays"] ?? "7");
        }

        /// <summary>POST /api/auth/register — creates a Guest account.</summary>
        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterDto dto)
        {
            try
            {
                var (response, refreshToken) = await _authService.RegisterAsync(dto);
                SetRefreshTokenCookie(refreshToken);
                return StatusCode(201, response);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        /// <summary>POST /api/auth/login</summary>
        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginDto dto)
        {
            try
            {
                var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
                var (response, refreshToken) = await _authService.LoginAsync(dto, ipAddress);
                SetRefreshTokenCookie(refreshToken);
                return Ok(response);
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(new { message = ex.Message });
            }
        }

        /// <summary>
        /// POST /api/auth/refresh-token — reads refresh token from HttpOnly cookie.
        /// Non-browser clients may send it in the request body as a fallback.
        /// </summary>
        [HttpPost("refresh-token")]
        public async Task<IActionResult> RefreshToken([FromBody] RefreshTokenDto? body)
        {
            var token = Request.Cookies["refreshToken"] ?? body?.Token;

            if (string.IsNullOrEmpty(token))
                return Unauthorized(new { message = "No refresh token provided." });

            try
            {
                var (response, newRefreshToken) = await _authService.RefreshTokenAsync(token);
                SetRefreshTokenCookie(newRefreshToken);
                return Ok(response);
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(new { message = ex.Message });
            }
        }

        /// <summary>POST /api/auth/logout — revokes the refresh token and clears the cookie.</summary>
        [HttpPost("logout")]
        [Authorize]
        public async Task<IActionResult> Logout()
        {
            var userId = User.FindFirstValue(JwtRegisteredClaimNames.Sub)
                ?? User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (userId != null)
                await _authService.LogoutAsync(userId);

            Response.Cookies.Delete("refreshToken", new CookieOptions { SameSite = SameSiteMode.Strict });
            return NoContent();
        }

        /// <summary>POST /api/auth/change-password</summary>
        [HttpPost("change-password")]
        [Authorize]
        public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordDto dto)
        {
            var userId = User.FindFirstValue(JwtRegisteredClaimNames.Sub)
                ?? User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (string.IsNullOrEmpty(userId))
                return Unauthorized();

            try
            {
                await _authService.ChangePasswordAsync(userId, dto);
                // Clear cookie — user must re-login after password change
                Response.Cookies.Delete("refreshToken", new CookieOptions { SameSite = SameSiteMode.Strict });
                return Ok(new { message = "Password changed successfully. Please log in again." });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        // ── Private helpers ────────────────────────────────────────────────────

        private void SetRefreshTokenCookie(string token)
        {
            Response.Cookies.Append("refreshToken", token, new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.Strict,
                Expires = DateTimeOffset.UtcNow.AddDays(_refreshTokenExpiryDays)
            });
        }
    }
}
