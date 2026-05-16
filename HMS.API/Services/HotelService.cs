
using HMS.API.Data;
using HMS.API.DTOs.Hotel;
using HMS.API.Models;
using Microsoft.EntityFrameworkCore;

namespace HMS.API.Services
{
    public class HotelService : IHotelService
    {
        private readonly ApplicationDbContext _db;

        public HotelService(ApplicationDbContext db)
        {
            _db = db;
        }

        public async Task<IEnumerable<HotelDto>> GetAllAsync(bool includeInactive = false)
        {
            var hotels = await _db.Hotels
                .Include(h => h.Rooms)
                .Where(h => includeInactive || h.IsActive)
                .OrderBy(h => h.Name)
                .ToListAsync();

            return hotels.Select(ToDto);
        }

        public async Task<HotelDto?> GetByIdAsync(int id)
        {
            var hotel = await _db.Hotels
                .Include(h => h.Rooms)
                .FirstOrDefaultAsync(h => h.Id == id);

            return hotel == null ? null : ToDto(hotel);
        }

        public async Task<HotelDto> CreateAsync(CreateHotelDto dto)
        {
            var hotel = new Hotel
            {
                Name = dto.Name,
                Location = dto.Location,
                Address = dto.Address,
                Description = dto.Description,
                ImageUrl = dto.ImageUrl,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            _db.Hotels.Add(hotel);
            await _db.SaveChangesAsync();

            return ToDto(hotel);
        }

        public async Task<HotelDto> UpdateAsync(int id, UpdateHotelDto dto)
        {
            var hotel = await _db.Hotels
                .Include(h => h.Rooms)
                .FirstOrDefaultAsync(h => h.Id == id)
                ?? throw new KeyNotFoundException($"Hotel {id} not found.");

            if (dto.Name != null) hotel.Name = dto.Name;
            if (dto.Location != null) hotel.Location = dto.Location;
            if (dto.Address != null) hotel.Address = dto.Address;
            if (dto.Description != null) hotel.Description = dto.Description;
            if (dto.ImageUrl != null) hotel.ImageUrl = dto.ImageUrl;
            if (dto.IsActive.HasValue) hotel.IsActive = dto.IsActive.Value;

            await _db.SaveChangesAsync();

            return ToDto(hotel);
        }

        public async Task DeleteAsync(int id)
        {
            var hotel = await _db.Hotels.FindAsync(id)
                ?? throw new KeyNotFoundException($"Hotel {id} not found.");

            // Soft delete — preserves all related bookings and room data
            hotel.IsActive = false;
            await _db.SaveChangesAsync();
        }

        // ── Mapping ────────────────────────────────────────────────────────────

        private static HotelDto ToDto(Hotel hotel) => new()
        {
            Id = hotel.Id,
            Name = hotel.Name,
            Location = hotel.Location,
            Address = hotel.Address,
            Description = hotel.Description,
            ImageUrl = hotel.ImageUrl,
            IsActive = hotel.IsActive,
            CreatedAt = hotel.CreatedAt,
            RoomCount = hotel.Rooms.Count(r => r.Status != RoomStatus.OutOfService)
        };
    }
}
