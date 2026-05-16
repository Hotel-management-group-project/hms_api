// Student ID: S2401276
// Student Name: Mohamed Iyaadh Ahmed
// Module: Advanced Software Development (UFCF8S-30-2)

using HMS.API.DTOs.Booking;

namespace HMS.API.Services
{
    public interface IBookingService
    {
        Task<IEnumerable<BookingListDto>> GetAllAsync(string userId, bool isStaff, string? referenceNumber = null);
        Task<BookingDto?> GetByIdAsync(int id, string userId, bool isStaff);
        Task<BookingDto> CreateAsync(string guestId, CreateBookingDto dto);
        Task<BookingDto> UpdateAsync(int id, UpdateBookingDto dto);
        Task<BookingDto> CancelAsync(int id, string userId, bool isStaff, CancelBookingDto? body);
        Task<BookingDto> MarkNoShowAsync(int id, string actorUserId);
    }
}
