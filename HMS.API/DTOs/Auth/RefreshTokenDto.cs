
namespace HMS.API.DTOs.Auth
{
    // Used as fallback for non-browser clients that cannot send HttpOnly cookies.
    // The /refresh-token endpoint checks the cookie first, then falls back to this body.
    public class RefreshTokenDto
    {
        public string? Token { get; set; }
    }
}
