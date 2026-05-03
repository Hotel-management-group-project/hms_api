// Student ID: S2401276
// Student Name: Mohamed Iyaadh Ahmed
// Module: Advanced Software Development (UFCF8S-30-2)

namespace HMS.API.Models
{
    public enum PaymentStatus { Pending, Completed, Refunded }

    public class Payment
    {
        public int Id { get; set; }
        public int BookingId { get; set; }
        public Booking Booking { get; set; } = null!;
        public decimal Amount { get; set; }
        public string Method { get; set; } = "Mock";
        public PaymentStatus Status { get; set; } = PaymentStatus.Pending;
        public string? TransactionReference { get; set; }
        public DateTime ProcessedAt { get; set; } = DateTime.UtcNow;
    }
}