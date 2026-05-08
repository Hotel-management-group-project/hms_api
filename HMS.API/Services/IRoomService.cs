// Student ID: S2401276
// Student Name: Mohamed Iyaadh Ahmed
// Module: Advanced Software Development (UFCF8S-30-2)

using HMS.API.DTOs.Room;

namespace HMS.API.Services
{
    public interface IRoomService
    {
        Task<IEnumerable<RoomDto>> GetByHotelAsync(int? hotelId, string? type);
        Task<RoomDto?> GetByIdAsync(int id);
        Task<RoomDto> CreateAsync(CreateRoomDto dto);
        Task<RoomDto> UpdateAsync(int id, UpdateRoomDto dto);
        Task<RoomDto> UpdateStatusAsync(int id, string status, string updatedByUserId);
        Task<IEnumerable<RoomAvailabilityDto>> GetAvailableAsync(
            int? hotelId, DateTime checkIn, DateTime checkOut, int? capacity, string? type);
    }
}
