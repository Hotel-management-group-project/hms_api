
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
