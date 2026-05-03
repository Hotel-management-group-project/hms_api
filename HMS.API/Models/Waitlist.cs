// Student ID: S2401276
// Student Name: Mohamed Iyaadh Ahmed
// Module: Advanced Software Development (UFCF8S-30-2)

namespace HMS.API.Models
{
    public class Waitlist
    {
        public int Id { get; set; }
        public string GuestId { get; set; } = string.Empty;
        public User Guest { get; set; } = null!;
        public int HotelId { get; set; }
        public Hotel Hotel { get; set; } = null!;
        public RoomType RoomType { get; set; }
        public DateTime CheckInDate { get; set; }
        public DateTime CheckOutDate { get; set; }
        public string Status { get; set; } = "Waiting";
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}