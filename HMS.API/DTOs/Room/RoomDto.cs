// Student ID: S2401276
// Student Name: Mohamed Iyaadh Ahmed
// Module: Advanced Software Development (UFCF8S-30-2)

namespace HMS.API.DTOs.Room
{
    public class RoomDto
    {
        public int Id { get; set; }
        public int HotelId { get; set; }
        public string HotelName { get; set; } = string.Empty;
        public string RoomNumber { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public int Capacity { get; set; }
        public decimal PriceOffPeak { get; set; }
        public decimal PricePeak { get; set; }
        public decimal CurrentPricePerNight { get; set; }
        public string Status { get; set; } = string.Empty;
        public string? Description { get; set; }
        public List<string> ImageUrls { get; set; } = [];
        public int Floor { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
