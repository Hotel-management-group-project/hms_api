// Student ID: S2401276
// Student Name: Mohamed Iyaadh Ahmed
// Module: Advanced Software Development (UFCF8S-30-2)

namespace HMS.API.Models
{
    public enum BookingStatus { Pending, Confirmed, CheckedIn, CheckedOut, Cancelled, NoShow }

    public class Booking
    {
        public int Id { get; set; }
        public string GuestId { get; set; } = string.Empty;
        public User Guest { get; set; } = null!;
        public int HotelId { get; set; }
        public Hotel Hotel { get; set; } = null!;
        public string ReferenceNumber { get; set; } = string.Empty;
        public DateTime CheckInDate { get; set; }
        public DateTime CheckOutDate { get; set; }
        public decimal TotalPrice { get; set; }
        public BookingStatus Status { get; set; } = BookingStatus.Pending;
        public decimal CancellationFee { get; set; } = 0;
        public string? QrCodeUrl { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
        public ICollection<BookingRoom> BookingRooms { get; set; } = new List<BookingRoom>();
        public ICollection<BookingAncillaryService> BookingAncillaryServices { get; set; } = new List<BookingAncillaryService>();
        public Payment? Payment { get; set; }
    }
}