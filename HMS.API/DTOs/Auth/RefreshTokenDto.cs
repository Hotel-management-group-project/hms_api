// Student ID: S2401276
// Student Name: Mohamed Iyaadh Ahmed
// Module: Advanced Software Development (UFCF8S-30-2)

namespace HMS.API.DTOs.Auth
{
    // Used as fallback for non-browser clients that cannot send HttpOnly cookies.
    // The /refresh-token endpoint checks the cookie first, then falls back to this body.
    public class RefreshTokenDto
    {
        public string? Token { get; set; }
    }
}
