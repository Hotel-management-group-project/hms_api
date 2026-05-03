// Student ID: S2401276
// Student Name: Mohamed Iyaadh Ahmed
// Module: Advanced Software Development (UFCF8S-30-2)

namespace HMS.API.DTOs.Room
{
    public class RoomAvailabilityDto
    {
        public int Id { get; set; }
        public int HotelId { get; set; }
        public string HotelName { get; set; } = string.Empty;
        public string RoomNumber { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public int Capacity { get; set; }
        public decimal PriceOffPeak { get; set; }
        public decimal PricePeak { get; set; }
        // Price for the check-in night — use this as the headline rate for display
        public decimal PricePerNight { get; set; }
        // Precise sum of each night's rate (handles stays that span peak/off-peak boundary)
        public decimal TotalPrice { get; set; }
        public int Nights { get; set; }
        public string? Description { get; set; }
        public string? ImageUrls { get; set; }
        public int Floor { get; set; }
    }
}
