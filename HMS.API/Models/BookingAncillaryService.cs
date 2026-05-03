// Student ID: S2401276
// Student Name: Mohamed Iyaadh Ahmed
// Module: Advanced Software Development (UFCF8S-30-2)

namespace HMS.API.Models
{
    public class BookingAncillaryService
    {
        public int Id { get; set; }
        public int BookingId { get; set; }
        public Booking Booking { get; set; } = null!;
        public int AncillaryServiceId { get; set; }
        public AncillaryService AncillaryService { get; set; } = null!;
        public int Quantity { get; set; }
        public decimal TotalPrice { get; set; }
    }
}