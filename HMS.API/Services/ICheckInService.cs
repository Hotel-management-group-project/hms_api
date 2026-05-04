// Student ID: S2401276
// Student Name: Mohamed Iyaadh Ahmed
// Module: Advanced Software Development (UFCF8S-30-2)

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
