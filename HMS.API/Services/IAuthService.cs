// Student ID: S2401276
// Student Name: Mohamed Iyaadh Ahmed
// Module: Advanced Software Development (UFCF8S-30-2)

using HMS.API.DTOs.Auth;

namespace HMS.API.Services
{
    public interface IAuthService
    {
        Task<(AuthResponseDto Response, string RefreshToken)> RegisterAsync(RegisterDto dto);
        Task<(AuthResponseDto Response, string RefreshToken)> LoginAsync(LoginDto dto, string? ipAddress);
        Task<(AuthResponseDto Response, string RefreshToken)> RefreshTokenAsync(string refreshToken);
        Task LogoutAsync(string userId);
        Task ChangePasswordAsync(string userId, ChangePasswordDto dto);
    }
}
