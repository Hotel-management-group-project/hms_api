
using HMS.API.DTOs.Hotel;

namespace HMS.API.Services
{
    public interface IHotelService
    {
        Task<IEnumerable<HotelDto>> GetAllAsync(bool includeInactive = false);
        Task<HotelDto?> GetByIdAsync(int id);
        Task<HotelDto> CreateAsync(CreateHotelDto dto);
        Task<HotelDto> UpdateAsync(int id, UpdateHotelDto dto);
        Task DeleteAsync(int id);
    }
}
