// Student ID: S2401276
// Student Name: Mohamed Iyaadh Ahmed
// Module: Advanced Software Development (UFCF8S-30-2)

using System.ComponentModel.DataAnnotations;

namespace HMS.API.DTOs.Room
{
    public class CreateRoomDto
    {
        [Required]
        public int HotelId { get; set; }

        [Required]
        [MaxLength(10)]
        public string RoomNumber { get; set; } = string.Empty;

        // StandardDouble | DeluxeKing | FamilySuite | Penthouse
        [Required]
        public string Type { get; set; } = string.Empty;

        [Required]
        [Range(1, 10)]
        public int Capacity { get; set; }

        [Required]
        [Range(0.01, 99999.99)]
        public decimal PriceOffPeak { get; set; }

        [Required]
        [Range(0.01, 99999.99)]
        public decimal PricePeak { get; set; }

        public string? Description { get; set; }
        public string? ImageUrls { get; set; }

        [Required]
        [Range(1, 200)]
        public int Floor { get; set; }
    }
}
