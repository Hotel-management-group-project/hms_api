
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using HMS.API.Data;
using HMS.API.DTOs.Auth;
using HMS.API.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.Tokens;

namespace HMS.API.Services
{
    public class AuthService : IAuthService
    {
        private readonly UserManager<User> _userManager;
        private readonly SignInManager<User> _signInManager;
        private readonly ApplicationDbContext _db;
        private readonly IEmailService _emailService;
        private readonly ILogger<AuthService> _logger;
        private readonly string _jwtSecret;
        private readonly string _jwtIssuer;
        private readonly string _jwtAudience;
        private readonly int _accessTokenExpiryMinutes;
        private readonly int _refreshTokenExpiryDays;

        public AuthService(
            UserManager<User> userManager,
            SignInManager<User> signInManager,
            ApplicationDbContext db,
            IEmailService emailService,
            ILogger<AuthService> logger,
            IConfiguration config)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _db = db;
            _emailService = emailService;
            _logger = logger;
            _jwtSecret = config["JwtSettings:Secret"]!;
            _jwtIssuer = config["JwtSettings:Issuer"]!;
            _jwtAudience = config["JwtSettings:Audience"]!;
            _accessTokenExpiryMinutes = int.Parse(config["JwtSettings:ExpiryMinutes"] ?? "15");
            _refreshTokenExpiryDays = int.Parse(config["JwtSettings:RefreshTokenExpiryDays"] ?? "7");
        }

        public async Task<(AuthResponseDto Response, string RefreshToken)> RegisterAsync(RegisterDto dto)
        {
            var existing = await _userManager.FindByEmailAsync(dto.Email);
            if (existing != null)
                throw new InvalidOperationException("An account with this email already exists.");

            var user = new User
            {
                UserName = dto.Email,
                Email = dto.Email,
                FirstName = dto.FirstName,
                LastName = dto.LastName,
                PhoneNumber = dto.PhoneNumber,
                Role = "Guest",
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            var result = await _userManager.CreateAsync(user, dto.Password);
            if (!result.Succeeded)
                throw new InvalidOperationException(string.Join(", ", result.Errors.Select(e => e.Description)));

            await _userManager.AddToRoleAsync(user, "Guest");

            var (accessToken, refreshToken) = await GenerateAndStoreTokensAsync(user);

            await LogAuditAsync(user.Id, "Register", "User", user.Id, null, $"New guest account: {user.Email}");

            return (BuildResponse(user, accessToken, false), refreshToken);
        }

        public async Task<(AuthResponseDto Response, string RefreshToken)> LoginAsync(LoginDto dto, string? ipAddress)
        {
            var user = await _userManager.FindByEmailAsync(dto.Email);

            if (user == null)
            {
                await LogAuditAsync(null, "Login.Failed", "User", null, ipAddress, $"Unknown email: {dto.Email}");
                throw new UnauthorizedAccessException("Invalid credentials.");
            }

            if (!user.IsActive)
            {
                await LogAuditAsync(user.Id, "Login.Failed", "User", user.Id, ipAddress, "Inactive account.");
                throw new UnauthorizedAccessException("This account has been deactivated.");
            }

            var signInResult = await _signInManager.CheckPasswordSignInAsync(user, dto.Password, lockoutOnFailure: true);

            if (signInResult.IsLockedOut)
            {
                await LogAuditAsync(user.Id, "Login.Failed", "User", user.Id, ipAddress, "Account locked out.");
                throw new UnauthorizedAccessException("Account locked after too many failed attempts. Try again in 15 minutes.");
            }

            if (!signInResult.Succeeded)
            {
                await LogAuditAsync(user.Id, "Login.Failed", "User", user.Id, ipAddress, "Wrong password.");
                throw new UnauthorizedAccessException("Invalid credentials.");
            }

            var requiresPasswordChange = RequiresPasswordChange(user);
            var (accessToken, refreshToken) = await GenerateAndStoreTokensAsync(user);

            await LogAuditAsync(user.Id, "Login.Success", "User", user.Id, ipAddress, $"Login: {user.Email}");

            if (requiresPasswordChange)
            {
                try
                {
                    await _emailService.SendPasswordChangeReminderEmailAsync(
                        user.Email!,
                        $"{user.FirstName} {user.LastName}");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Password change reminder email failed for user {Email}", user.Email);
                }
            }

            return (BuildResponse(user, accessToken, requiresPasswordChange), refreshToken);
        }

        public async Task<(AuthResponseDto Response, string RefreshToken)> RefreshTokenAsync(string refreshToken)
        {
            var principal = ValidateRefreshToken(refreshToken);
            var userId = principal.FindFirstValue(JwtRegisteredClaimNames.Sub)
                ?? throw new UnauthorizedAccessException("Invalid refresh token.");

            var user = await _userManager.FindByIdAsync(userId)
                ?? throw new UnauthorizedAccessException("User not found.");

            if (!user.IsActive)
                throw new UnauthorizedAccessException("This account has been deactivated.");

            var storedToken = await _userManager.GetAuthenticationTokenAsync(user, "HMS", "RefreshToken");
            if (storedToken != refreshToken)
                throw new UnauthorizedAccessException("Refresh token has been revoked.");

            var requiresPasswordChange = RequiresPasswordChange(user);
            var (accessToken, newRefreshToken) = await GenerateAndStoreTokensAsync(user);

            await LogAuditAsync(user.Id, "TokenRefresh", "User", user.Id, null, null);

            return (BuildResponse(user, accessToken, requiresPasswordChange), newRefreshToken);
        }

        public async Task LogoutAsync(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null) return;

            await _userManager.RemoveAuthenticationTokenAsync(user, "HMS", "RefreshToken");
            await LogAuditAsync(userId, "Logout", "User", userId, null, null);
        }

        public async Task ChangePasswordAsync(string userId, ChangePasswordDto dto)
        {
            var user = await _userManager.FindByIdAsync(userId)
                ?? throw new InvalidOperationException("User not found.");

            var result = await _userManager.ChangePasswordAsync(user, dto.CurrentPassword, dto.NewPassword);
            if (!result.Succeeded)
                throw new InvalidOperationException(string.Join(", ", result.Errors.Select(e => e.Description)));

            user.LastPasswordChange = DateTime.UtcNow;
            await _userManager.UpdateAsync(user);

            // Revoke existing refresh token so the user must re-login after a password change
            await _userManager.RemoveAuthenticationTokenAsync(user, "HMS", "RefreshToken");

            await LogAuditAsync(userId, "PasswordChange", "User", userId, null, null);
        }

        // ── Private helpers ────────────────────────────────────────────────────

        private async Task<(string AccessToken, string RefreshToken)> GenerateAndStoreTokensAsync(User user)
        {
            var accessToken = GenerateAccessToken(user);
            var refreshToken = GenerateRefreshToken(user);
            await _userManager.SetAuthenticationTokenAsync(user, "HMS", "RefreshToken", refreshToken);
            return (accessToken, refreshToken);
        }

        private string GenerateAccessToken(User user)
        {
            var claims = new[]
            {
                new Claim(JwtRegisteredClaimNames.Sub, user.Id),
                new Claim(JwtRegisteredClaimNames.Email, user.Email!),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                new Claim("role", user.Role),
                new Claim("firstName", user.FirstName),
                new Claim("lastName", user.LastName)
            };

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtSecret));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: _jwtIssuer,
                audience: _jwtAudience,
                claims: claims,
                expires: DateTime.UtcNow.AddMinutes(_accessTokenExpiryMinutes),
                signingCredentials: creds
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        private string GenerateRefreshToken(User user)
        {
            var claims = new[]
            {
                new Claim(JwtRegisteredClaimNames.Sub, user.Id),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                new Claim("type", "refresh")
            };

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtSecret));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: _jwtIssuer,
                audience: _jwtAudience,
                claims: claims,
                expires: DateTime.UtcNow.AddDays(_refreshTokenExpiryDays),
                signingCredentials: creds
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        private ClaimsPrincipal ValidateRefreshToken(string token)
        {
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtSecret));
            var handler = new JwtSecurityTokenHandler();

            try
            {
                var principal = handler.ValidateToken(token, new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = key,
                    ValidateIssuer = true,
                    ValidIssuer = _jwtIssuer,
                    ValidateAudience = true,
                    ValidAudience = _jwtAudience,
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.Zero
                }, out _);

                if (principal.FindFirstValue("type") != "refresh")
                    throw new UnauthorizedAccessException("Invalid token type.");

                return principal;
            }
            catch (SecurityTokenException)
            {
                throw new UnauthorizedAccessException("Invalid or expired refresh token.");
            }
        }

        private static bool RequiresPasswordChange(User user)
        {
            if (user.Role != "Admin" && user.Role != "Manager") return false;
            if (user.LastPasswordChange == null) return true;
            return user.LastPasswordChange.Value.AddMonths(6) < DateTime.UtcNow;
        }

        private static AuthResponseDto BuildResponse(User user, string accessToken, bool requiresPasswordChange) =>
            new()
            {
                Token = accessToken,
                User = new AuthUserDto
                {
                    Id = user.Id,
                    Email = user.Email!,
                    FirstName = user.FirstName,
                    LastName = user.LastName,
                    Role = user.Role
                },
                RequiresPasswordChange = requiresPasswordChange
            };

        private async Task LogAuditAsync(string? userId, string action, string entityType, string? entityId, string? ipAddress, string? details)
        {
            _db.AuditLogs.Add(new AuditLog
            {
                UserId = userId,
                Action = action,
                EntityType = entityType,
                EntityId = entityId,
                IpAddress = ipAddress,
                Details = details,
                CreatedAt = DateTime.UtcNow
            });
            await _db.SaveChangesAsync();
        }
    }
}
