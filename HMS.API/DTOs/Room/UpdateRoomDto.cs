// Student ID: S2401276
// Student Name: Mohamed Iyaadh Ahmed
// Module: Advanced Software Development (UFCF8S-30-2)

using System.ComponentModel.DataAnnotations;

namespace HMS.API.DTOs.Room
{
    public class UpdateRoomDto
    {
        [MaxLength(10)]
        public string? RoomNumber { get; set; }

        // StandardDouble | DeluxeKing | FamilySuite | Penthouse
        public string? Type { get; set; }

        [Range(1, 10)]
        public int? Capacity { get; set; }

        [Range(0.01, 99999.99)]
        public decimal? PriceOffPeak { get; set; }

        [Range(0.01, 99999.99)]
        public decimal? PricePeak { get; set; }

        // Available | Occupied | Cleaning | OutOfService
        public string? Status { get; set; }

        public string? Description { get; set; }
        public string? ImageUrls { get; set; }

        [Range(1, 200)]
        public int? Floor { get; set; }
    }
}
