
using HMS.API.DTOs.Booking;

namespace HMS.API.Services
{
    public interface ICheckInService
    {
        Task<BookingDto> ScanAsync(string referenceNumber);
        Task<BookingDto> CheckInAsync(int bookingId, string staffUserId);
        Task<BookingDto> CheckOutAsync(int bookingId, string staffUserId);
    }
}
