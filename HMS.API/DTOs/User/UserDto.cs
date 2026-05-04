// Student ID: S2401276
// Student Name: Mohamed Iyaadh Ahmed
// Module: Advanced Software Development (UFCF8S-30-2)

using System.ComponentModel.DataAnnotations;

namespace HMS.API.DTOs.User
{
    public class UserDto
    {
        public string Id { get; set; } = string.Empty;
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string? PhoneNumber { get; set; }
        public string Role { get; set; } = string.Empty;
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? LastPasswordChange { get; set; }
    }

    public class CreateUserDto
    {
        [Required] public string FirstName { get; set; } = string.Empty;
        [Required] public string LastName { get; set; } = string.Empty;
        [Required, EmailAddress] public string Email { get; set; } = string.Empty;
        [Required, MinLength(8)] public string Password { get; set; } = string.Empty;
        public string? PhoneNumber { get; set; }
        [Required] public string Role { get; set; } = "Guest";
    }

    public class UpdateUserDto
    {
        [Required] public string FirstName { get; set; } = string.Empty;
        [Required] public string LastName { get; set; } = string.Empty;
        public string? PhoneNumber { get; set; }
    }

    public class UpdateUserStatusDto
    {
        [Required] public bool IsActive { get; set; }
    }
}
