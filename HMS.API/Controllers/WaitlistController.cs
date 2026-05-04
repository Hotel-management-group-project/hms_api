// Student ID: S2401276
// Student Name: Mohamed Iyaadh Ahmed
// Module: Advanced Software Development (UFCF8S-30-2)

using HMS.API.Data;
using HMS.API.DTOs.Waitlist;
using HMS.API.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace HMS.API.Controllers
{
    [ApiController]
    [Route("api/waitlist")]
    [Authorize]
    public class WaitlistController : ControllerBase
    {
        private readonly ApplicationDbContext _db;

        public WaitlistController(ApplicationDbContext db)
        {
            _db = db;
        }

        [HttpPost]
        [Authorize(Roles = "Guest")]
        public async Task<IActionResult> Join([FromBody] JoinWaitlistDto dto)
        {
            var guestId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;

            if (!Enum.TryParse<RoomType>(dto.RoomType, out var roomType))
                return BadRequest(new { message = $"Invalid room type '{dto.RoomType}'." });

            if (dto.CheckInDate.Date <= DateTime.UtcNow.Date)
                return BadRequest(new { message = "Check-in date must be in the future." });

            if (dto.CheckOutDate.Date <= dto.CheckInDate.Date)
                return BadRequest(new { message = "Check-out must be after check-in." });

            var hotel = await _db.Hotels.FindAsync(dto.HotelId);
            if (hotel is null || !hotel.IsActive)
                return NotFound(new { message = "Hotel not found." });

            var entry = new Waitlist
            {
                GuestId = guestId,
                HotelId = dto.HotelId,
                RoomType = roomType,
                CheckInDate = dto.CheckInDate.ToUniversalTime(),
                CheckOutDate = dto.CheckOutDate.ToUniversalTime(),
                Status = "Waiting"
            };

            _db.Waitlists.Add(entry);
            await _db.SaveChangesAsync();

            return CreatedAtAction(nameof(GetById), new { id = entry.Id }, await ToDto(entry));
        }

        [HttpGet("my")]
        [Authorize(Roles = "Guest")]
        public async Task<IActionResult> GetMine()
        {
            var guestId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;

            var entries = await _db.Waitlists
                .Include(w => w.Guest)
                .Include(w => w.Hotel)
                .Where(w => w.GuestId == guestId)
                .OrderByDescending(w => w.CreatedAt)
                .ToListAsync();

            return Ok(entries.Select(ToWaitlistDto));
        }

        [HttpGet]
        [Authorize(Roles = "FrontDesk,Manager,Admin")]
        public async Task<IActionResult> GetAll(
            [FromQuery] int? hotelId,
            [FromQuery] string? status,
            [FromQuery] string? roomType)
        {
            var query = _db.Waitlists
                .Include(w => w.Guest)
                .Include(w => w.Hotel)
                .AsQueryable();

            if (hotelId.HasValue)
                query = query.Where(w => w.HotelId == hotelId.Value);

            if (!string.IsNullOrWhiteSpace(status))
                query = query.Where(w => w.Status == status);

            if (!string.IsNullOrWhiteSpace(roomType) && Enum.TryParse<RoomType>(roomType, out var rt))
                query = query.Where(w => w.RoomType == rt);

            var entries = await query.OrderBy(w => w.CreatedAt).ToListAsync();
            return Ok(entries.Select(ToWaitlistDto));
        }

        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetById(int id)
        {
            var entry = await _db.Waitlists
                .Include(w => w.Guest)
                .Include(w => w.Hotel)
                .FirstOrDefaultAsync(w => w.Id == id);

            if (entry is null) return NotFound();

            var guestId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
            var callerRole = User.FindFirstValue(ClaimTypes.Role)
                ?? User.FindFirstValue("role")
                ?? string.Empty;

            if (callerRole == "Guest" && entry.GuestId != guestId)
                return Forbid();

            return Ok(ToWaitlistDto(entry));
        }

        [HttpPatch("{id:int}/status")]
        [Authorize(Roles = "FrontDesk,Manager,Admin")]
        public async Task<IActionResult> UpdateStatus(int id, [FromBody] UpdateWaitlistStatusDto dto)
        {
            var entry = await _db.Waitlists
                .Include(w => w.Guest)
                .Include(w => w.Hotel)
                .FirstOrDefaultAsync(w => w.Id == id);

            if (entry is null) return NotFound();

            var allowedStatuses = new[] { "Waiting", "Notified", "Booked", "Cancelled" };
            if (!allowedStatuses.Contains(dto.Status))
                return BadRequest(new { message = $"Invalid status '{dto.Status}'. Allowed: {string.Join(", ", allowedStatuses)}" });

            entry.Status = dto.Status;
            await _db.SaveChangesAsync();

            return Ok(ToWaitlistDto(entry));
        }

        [HttpDelete("{id:int}")]
        [Authorize(Roles = "Guest")]
        public async Task<IActionResult> Leave(int id)
        {
            var guestId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
            var entry = await _db.Waitlists.FirstOrDefaultAsync(w => w.Id == id && w.GuestId == guestId);

            if (entry is null) return NotFound();
            if (entry.Status != "Waiting")
                return BadRequest(new { message = "Only entries with status 'Waiting' can be removed." });

            _db.Waitlists.Remove(entry);
            await _db.SaveChangesAsync();

            return NoContent();
        }

        private static WaitlistDto ToWaitlistDto(Waitlist w) => new()
        {
            Id = w.Id,
            GuestId = w.GuestId,
            GuestName = $"{w.Guest.FirstName} {w.Guest.LastName}",
            GuestEmail = w.Guest.Email ?? string.Empty,
            HotelId = w.HotelId,
            HotelName = w.Hotel.Name,
            RoomType = w.RoomType.ToString(),
            CheckInDate = w.CheckInDate,
            CheckOutDate = w.CheckOutDate,
            Status = w.Status,
            CreatedAt = w.CreatedAt
        };

        private static Task<WaitlistDto> ToDto(Waitlist w) => Task.FromResult(ToWaitlistDto(w));
    }
}
