
namespace HMS.API.DTOs.Payment
{
    public class PaymentResponseDto
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public PaymentDto Payment { get; set; } = null!;
        public string BookingStatus { get; set; } = string.Empty;
    }
}
