// Student ID: S2401276
// Student Name: Mohamed Iyaadh Ahmed
// Module: Advanced Software Development (UFCF8S-30-2)

using System.ComponentModel.DataAnnotations;

namespace HMS.API.DTOs.Room
{
    public class UpdateRoomStatusDto
    {
        // Available | Occupied | Cleaning | OutOfService
        [Required]
        public string Status { get; set; } = string.Empty;
    }
}
