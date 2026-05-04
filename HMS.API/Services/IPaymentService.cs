// Student ID: S2401276
// Student Name: Mohamed Iyaadh Ahmed
// Module: Advanced Software Development (UFCF8S-30-2)

using HMS.API.DTOs.Payment;

namespace HMS.API.Services
{
    public interface IPaymentService
    {
        Task<PaymentResponseDto> ProcessAsync(ProcessPaymentDto dto, string userId);
        Task<PaymentDto?> GetByBookingAsync(int bookingId);
    }
}
