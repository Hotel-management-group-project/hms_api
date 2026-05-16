
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