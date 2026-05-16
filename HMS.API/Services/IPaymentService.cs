
using HMS.API.DTOs.Payment;

namespace HMS.API.Services
{
    public interface IPaymentService
    {
        Task<PaymentResponseDto> ProcessAsync(ProcessPaymentDto dto, string userId);
        Task<PaymentDto?> GetByBookingAsync(int bookingId);
    }
}
