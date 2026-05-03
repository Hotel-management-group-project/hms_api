// Student ID: S2401276
// Student Name: Mohamed Iyaadh Ahmed
// Module: Advanced Software Development (UFCF8S-30-2)

using HMS.API.Data;
using HMS.API.DTOs.Room;
using HMS.API.Models;
using Microsoft.EntityFrameworkCore;

namespace HMS.API.Services
{
    public class RoomService : IRoomService
    {
        private readonly ApplicationDbContext _db;

        public RoomService(ApplicationDbContext db)
        {
            _db = db;
        }

        public async Task<IEnumerable<RoomDto>> GetByHotelAsync(int? hotelId, string? type)
        {
            var query = _db.Rooms.Include(r => r.Hotel).AsQueryable();

            if (hotelId.HasValue)
                query = query.Where(r => r.HotelId == hotelId.Value);

            if (!string.IsNullOrWhiteSpace(type) && Enum.TryParse<RoomType>(type, true, out var roomType))
                query = query.Where(r => r.Type == roomType);

            var rooms = await query.OrderBy(r => r.HotelId).ThenBy(r => r.RoomNumber).ToListAsync();

            return rooms.Select(r => ToDto(r, DateTime.UtcNow));
        }

        public async Task<RoomDto?> GetByIdAsync(int id)
        {
            var room = await _db.Rooms.Include(r => r.Hotel).FirstOrDefaultAsync(r => r.Id == id);
            return room == null ? null : ToDto(room, DateTime.UtcNow);
        }

        public async Task<RoomDto> CreateAsync(CreateRoomDto dto)
        {
            if (!Enum.TryParse<RoomType>(dto.Type, true, out var roomType))
                throw new ArgumentException($"Invalid room type '{dto.Type}'. Valid values: StandardDouble, DeluxeKing, FamilySuite, Penthouse.");

            var hotelExists = await _db.Hotels.AnyAsync(h => h.Id == dto.HotelId && h.IsActive);
            if (!hotelExists)
                throw new KeyNotFoundException($"Hotel {dto.HotelId} not found or is inactive.");

            var duplicate = await _db.Rooms.AnyAsync(r => r.HotelId == dto.HotelId && r.RoomNumber == dto.RoomNumber);
            if (duplicate)
                throw new InvalidOperationException($"Room number '{dto.RoomNumber}' already exists in this hotel.");

            var room = new Room
            {
                HotelId = dto.HotelId,
                RoomNumber = dto.RoomNumber,
                Type = roomType,
                Capacity = dto.Capacity,
                PriceOffPeak = dto.PriceOffPeak,
                PricePeak = dto.PricePeak,
                Status = RoomStatus.Available,
                Description = dto.Description,
                ImageUrls = dto.ImageUrls,
                Floor = dto.Floor,
                CreatedAt = DateTime.UtcNow
            };

            _db.Rooms.Add(room);
            await _db.SaveChangesAsync();

            await _db.Entry(room).Reference(r => r.Hotel).LoadAsync();
            return ToDto(room, DateTime.UtcNow);
        }

        public async Task<RoomDto> UpdateAsync(int id, UpdateRoomDto dto)
        {
            var room = await _db.Rooms
                .Include(r => r.Hotel)
                .FirstOrDefaultAsync(r => r.Id == id)
                ?? throw new KeyNotFoundException($"Room {id} not found.");

            if (dto.RoomNumber != null)
            {
                var duplicate = await _db.Rooms.AnyAsync(r =>
                    r.HotelId == room.HotelId && r.RoomNumber == dto.RoomNumber && r.Id != id);
                if (duplicate)
                    throw new InvalidOperationException($"Room number '{dto.RoomNumber}' already exists in this hotel.");
                room.RoomNumber = dto.RoomNumber;
            }

            if (dto.Type != null)
            {
                if (!Enum.TryParse<RoomType>(dto.Type, true, out var roomType))
                    throw new ArgumentException($"Invalid room type '{dto.Type}'.");
                room.Type = roomType;
            }

            if (dto.Capacity.HasValue) room.Capacity = dto.Capacity.Value;
            if (dto.PriceOffPeak.HasValue) room.PriceOffPeak = dto.PriceOffPeak.Value;
            if (dto.PricePeak.HasValue) room.PricePeak = dto.PricePeak.Value;
            if (dto.Description != null) room.Description = dto.Description;
            if (dto.ImageUrls != null) room.ImageUrls = dto.ImageUrls;
            if (dto.Floor.HasValue) room.Floor = dto.Floor.Value;

            if (dto.Status != null)
            {
                if (!Enum.TryParse<RoomStatus>(dto.Status, true, out var roomStatus))
                    throw new ArgumentException($"Invalid status '{dto.Status}'. Valid values: Available, Occupied, Cleaning, OutOfService.");
                room.Status = roomStatus;
            }

            await _db.SaveChangesAsync();
            return ToDto(room, DateTime.UtcNow);
        }

        public async Task<RoomDto> UpdateStatusAsync(int id, string status, string updatedByUserId)
        {
            if (!Enum.TryParse<RoomStatus>(status, true, out var newStatus))
                throw new ArgumentException($"Invalid status '{status}'. Valid values: Available, Occupied, Cleaning, OutOfService.");

            var room = await _db.Rooms
                .Include(r => r.Hotel)
                .FirstOrDefaultAsync(r => r.Id == id)
                ?? throw new KeyNotFoundException($"Room {id} not found.");

            var previousStatus = room.Status.ToString();
            room.Status = newStatus;

            _db.AuditLogs.Add(new AuditLog
            {
                UserId = updatedByUserId,
                Action = "RoomStatusChange",
                EntityType = "Room",
                EntityId = id.ToString(),
                Details = $"Status changed: {previousStatus} → {newStatus}",
                CreatedAt = DateTime.UtcNow
            });

            await _db.SaveChangesAsync();
            return ToDto(room, DateTime.UtcNow);
        }

        public async Task<IEnumerable<RoomAvailabilityDto>> GetAvailableAsync(
            int hotelId, DateTime checkIn, DateTime checkOut, int? capacity, string? type)
        {
            RoomType? roomType = null;
            if (!string.IsNullOrWhiteSpace(type))
            {
                if (!Enum.TryParse<RoomType>(type, true, out var parsed))
                    throw new ArgumentException($"Invalid room type '{type}'.");
                roomType = parsed;
            }

            var rooms = await _db.Rooms
                .Include(r => r.Hotel)
                .Include(r => r.BookingRooms).ThenInclude(br => br.Booking)
                .Where(r => r.HotelId == hotelId)
                .Where(r => r.Status != RoomStatus.OutOfService)
                .Where(r => !r.BookingRooms.Any(br =>
                    (br.Booking.Status == BookingStatus.Confirmed || br.Booking.Status == BookingStatus.CheckedIn) &&
                    br.Booking.CheckInDate < checkOut &&
                    br.Booking.CheckOutDate > checkIn))
                .Where(r => capacity == null || r.Capacity >= capacity)
                .Where(r => roomType == null || r.Type == roomType)
                .OrderBy(r => r.Type).ThenBy(r => r.Floor)
                .ToListAsync();

            var nights = (int)(checkOut.Date - checkIn.Date).TotalDays;

            return rooms.Select(r => ToAvailabilityDto(r, checkIn, checkOut, nights));
        }

        // ── Peak season logic ──────────────────────────────────────────────────

        private static bool IsPeakMonth(int month) => month is 6 or 7 or 8 or 12;

        private static decimal GetNightlyRate(Room room, DateTime night) =>
            IsPeakMonth(night.Month) ? room.PricePeak : room.PriceOffPeak;

        private static decimal CalculateTotalPrice(Room room, DateTime checkIn, DateTime checkOut)
        {
            var total = 0m;
            var night = checkIn.Date;
            while (night < checkOut.Date)
            {
                total += GetNightlyRate(room, night);
                night = night.AddDays(1);
            }
            return total;
        }

        // ── Mapping ────────────────────────────────────────────────────────────

        private static RoomDto ToDto(Room room, DateTime referenceDate) => new()
        {
            Id = room.Id,
            HotelId = room.HotelId,
            HotelName = room.Hotel?.Name ?? string.Empty,
            RoomNumber = room.RoomNumber,
            Type = room.Type.ToString(),
            Capacity = room.Capacity,
            PriceOffPeak = room.PriceOffPeak,
            PricePeak = room.PricePeak,
            CurrentPricePerNight = GetNightlyRate(room, referenceDate),
            Status = room.Status.ToString(),
            Description = room.Description,
            ImageUrls = room.ImageUrls,
            Floor = room.Floor,
            CreatedAt = room.CreatedAt
        };

        private static RoomAvailabilityDto ToAvailabilityDto(Room room, DateTime checkIn, DateTime checkOut, int nights) => new()
        {
            Id = room.Id,
            HotelId = room.HotelId,
            HotelName = room.Hotel?.Name ?? string.Empty,
            RoomNumber = room.RoomNumber,
            Type = room.Type.ToString(),
            Capacity = room.Capacity,
            PriceOffPeak = room.PriceOffPeak,
            PricePeak = room.PricePeak,
            PricePerNight = GetNightlyRate(room, checkIn),
            TotalPrice = CalculateTotalPrice(room, checkIn, checkOut),
            Nights = nights,
            Description = room.Description,
            ImageUrls = room.ImageUrls,
            Floor = room.Floor
        };
    }
}
