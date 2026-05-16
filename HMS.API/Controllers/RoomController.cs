
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using HMS.API.DTOs.Room;
using HMS.API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HMS.API.Controllers
{
    [ApiController]
    [Route("api/rooms")]
    public class RoomController : ControllerBase
    {
        private readonly IRoomService _roomService;

        public RoomController(IRoomService roomService)
        {
            _roomService = roomService;
        }

        /// <summary>
        /// GET /api/rooms/availability?hotelId=&amp;checkIn=&amp;checkOut=&amp;capacity=&amp;type=
        /// Public — returns rooms not overlapping with confirmed/checkedIn bookings.
        /// Defined before {id:int} so the literal "availability" is never treated as an ID.
        /// </summary>
        [HttpGet("availability")]
        public async Task<IActionResult> GetAvailable(
            [FromQuery] int? hotelId,
            [FromQuery] DateTime checkIn,
            [FromQuery] DateTime checkOut,
            [FromQuery] int? capacity,
            [FromQuery] string? type)
        {
            if (checkIn.Date < DateTime.UtcNow.Date)
                return BadRequest(new { message = "Check-in date cannot be in the past." });

            if (checkOut.Date <= checkIn.Date)
                return BadRequest(new { message = "Check-out must be after check-in." });

            try
            {
                var rooms = await _roomService.GetAvailableAsync(hotelId, checkIn, checkOut, capacity, type);
                return Ok(rooms);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        /// <summary>GET /api/rooms?hotelId=&amp;type= — public room listing (management view).</summary>
        [HttpGet]
        public async Task<IActionResult> GetAll([FromQuery] int? hotelId, [FromQuery] string? type)
        {
            try
            {
                var rooms = await _roomService.GetByHotelAsync(hotelId, type);
                return Ok(rooms);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        /// <summary>GET /api/rooms/{id} — public.</summary>
        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetById(int id)
        {
            var room = await _roomService.GetByIdAsync(id);
            return room == null ? NotFound(new { message = $"Room {id} not found." }) : Ok(room);
        }

        /// <summary>POST /api/rooms — Admin or Manager.</summary>
        [HttpPost]
        [Authorize(Roles = "Admin,Manager")]
        public async Task<IActionResult> Create([FromBody] CreateRoomDto dto)
        {
            try
            {
                var room = await _roomService.CreateAsync(dto);
                return CreatedAtAction(nameof(GetById), new { id = room.Id }, room);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return Conflict(new { message = ex.Message });
            }
        }

        /// <summary>PUT /api/rooms/{id} — Admin or Manager (full update).</summary>
        [HttpPut("{id:int}")]
        [Authorize(Roles = "Admin,Manager")]
        public async Task<IActionResult> Update(int id, [FromBody] UpdateRoomDto dto)
        {
            try
            {
                var room = await _roomService.UpdateAsync(id, dto);
                return Ok(room);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return Conflict(new { message = ex.Message });
            }
        }

        /// <summary>
        /// PATCH /api/rooms/{id}/status — Admin, Manager, or FrontDesk.
        /// Body: { "status": "Cleaning" }
        /// Writes an audit log entry on every change.
        /// </summary>
        [HttpPatch("{id:int}/status")]
        [Authorize(Roles = "Admin,Manager,FrontDesk")]
        public async Task<IActionResult> UpdateStatus(int id, [FromBody] UpdateRoomStatusDto dto)
        {
            var userId = User.FindFirstValue(JwtRegisteredClaimNames.Sub)
                ?? User.FindFirstValue(ClaimTypes.NameIdentifier)
                ?? string.Empty;

            try
            {
                var room = await _roomService.UpdateStatusAsync(id, dto.Status, userId);
                return Ok(room);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }
    }
}
