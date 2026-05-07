// Student ID: S2401276
// Student Name: Mohamed Iyaadh Ahmed
// Module: Advanced Software Development (UFCF8S-30-2)

using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using HMS.API.DTOs.Booking;
using HMS.API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HMS.API.Controllers
{
    [ApiController]
    [Route("api/bookings")]
    [Authorize]
    public class BookingController : ControllerBase
    {
        private readonly IBookingService _bookingService;
        private readonly IPdfService _pdfService;

        public BookingController(IBookingService bookingService, IPdfService pdfService)
        {
            _bookingService = bookingService;
            _pdfService = pdfService;
        }

        /// <summary>
        /// GET /api/bookings
        /// Guest → own bookings only. FrontDesk/Manager/Admin → all bookings.
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var bookings = await _bookingService.GetAllAsync(GetUserId(), IsStaff());
            return Ok(bookings);
        }

        /// <summary>
        /// GET /api/bookings/{id}
        /// Guest may only retrieve their own booking. Staff may retrieve any.
        /// </summary>
        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetById(int id)
        {
            var booking = await _bookingService.GetByIdAsync(id, GetUserId(), IsStaff());
            return booking == null
                ? NotFound(new { message = $"Booking {id} not found." })
                : Ok(booking);
        }

        /// <summary>
        /// GET /api/bookings/{id}/qr
        /// Returns the base64 QR code for the booking (guest owns it, staff any).
        /// </summary>
        [HttpGet("{id:int}/qr")]
        public async Task<IActionResult> GetQr(int id)
        {
            var booking = await _bookingService.GetByIdAsync(id, GetUserId(), IsStaff());
            if (booking == null)
                return NotFound(new { message = $"Booking {id} not found." });

            if (string.IsNullOrEmpty(booking.QrCodeUrl))
                return NotFound(new { message = "QR code not available for this booking." });

            return Ok(new { qrCodeUrl = booking.QrCodeUrl, referenceNumber = booking.ReferenceNumber });
        }

        /// <summary>
        /// GET /api/bookings/{id}/invoice
        /// Returns the PDF invoice for a completed or checked-out booking.
        /// Guest may only download their own invoice; staff may download any.
        /// </summary>
        [HttpGet("{id:int}/invoice")]
        public async Task<IActionResult> GetInvoice(int id)
        {
            var booking = await _bookingService.GetByIdAsync(id, GetUserId(), IsStaff());
            if (booking == null)
                return NotFound(new { message = $"Booking {id} not found." });

            var allowedStatuses = new[] { "CheckedOut", "CheckedIn", "Confirmed" };
            if (!allowedStatuses.Contains(booking.Status))
                return BadRequest(new { message = $"Invoice is only available for Confirmed, CheckedIn or CheckedOut bookings. Current status: {booking.Status}." });

            var pdfBytes = _pdfService.GenerateInvoice(booking);
            var fileName = $"Invoice-{booking.ReferenceNumber}.pdf";
            return File(pdfBytes, "application/pdf", fileName);
        }

        /// <summary>
        /// POST /api/bookings
        /// Any authenticated user can create a booking (defaults to self as guest).
        /// Staff may supply GuestId to book on behalf of a specific user.
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateBookingDto dto)
        {
            // Staff can book on behalf of another user; guests always book for themselves
            var guestId = IsStaff() && !string.IsNullOrEmpty(dto.GuestId)
                ? dto.GuestId
                : GetUserId();

            try
            {
                var booking = await _bookingService.CreateAsync(guestId, dto);
                return CreatedAtAction(nameof(GetById), new { id = booking.Id }, booking);
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
        /// PUT /api/bookings/{id}
        /// Staff only: confirms a Pending booking (Status = "Confirmed").
        /// Check-in/out and cancellation use their own dedicated endpoints.
        /// </summary>
        [HttpPut("{id:int}")]
        [Authorize(Roles = "FrontDesk,Manager,Admin")]
        public async Task<IActionResult> Update(int id, [FromBody] UpdateBookingDto dto)
        {
            try
            {
                var booking = await _bookingService.UpdateAsync(id, dto);
                return Ok(booking);
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
        /// DELETE /api/bookings/{id}
        /// Guest may cancel their own booking; staff may cancel any.
        /// Cancellation fee is calculated automatically based on notice period:
        ///   14+ days  → free
        ///   3–14 days → 50 % of first night
        ///   &lt; 72 hrs  → 100 % of first night
        ///   No-show   → 100 % of total booking value
        /// </summary>
        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Cancel(int id, [FromBody] CancelBookingDto? body)
        {
            try
            {
                var booking = await _bookingService.CancelAsync(id, GetUserId(), IsStaff(), body);
                return Ok(booking);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (UnauthorizedAccessException ex)
            {
                return Forbid(ex.Message);
            }
            catch (InvalidOperationException ex)
            {
                return Conflict(new { message = ex.Message });
            }
        }

        // ── Private helpers ────────────────────────────────────────────────────

        private string GetUserId() =>
            User.FindFirstValue(JwtRegisteredClaimNames.Sub)
            ?? User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? string.Empty;

        private bool IsStaff() =>
            User.IsInRole("FrontDesk") || User.IsInRole("Manager") || User.IsInRole("Admin");
    }
}
