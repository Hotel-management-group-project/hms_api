// Student ID: S2401276
// Student Name: Mohamed Iyaadh Ahmed
// Module: Advanced Software Development (UFCF8S-30-2)

namespace HMS.API.Models
{
    public enum RoomType { StandardDouble, DeluxeKing, FamilySuite, Penthouse }
    public enum RoomStatus { Available, Occupied, Cleaning, OutOfService }

    public class Room
    {
        public int Id { get; set; }
        public int HotelId { get; set; }
        public Hotel Hotel { get; set; } = null!;
        public string RoomNumber { get; set; } = string.Empty;
        public RoomType Type { get; set; }
        public int Capacity { get; set; }
        public decimal PriceOffPeak { get; set; }
        public decimal PricePeak { get; set; }
        public RoomStatus Status { get; set; } = RoomStatus.Available;
        public string? Description { get; set; }
        public string? ImageUrls { get; set; }
        public int Floor { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public ICollection<BookingRoom> BookingRooms { get; set; } = new List<BookingRoom>();
    }
}