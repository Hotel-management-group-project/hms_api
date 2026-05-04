// Student ID: S2401276
// Student Name: Mohamed Iyaadh Ahmed
// Module: Advanced Software Development (UFCF8S-30-2)

using HMS.API.Data;
using HMS.API.DTOs.Booking;
using HMS.API.Models;
using Microsoft.EntityFrameworkCore;

namespace HMS.API.Services
{
    public class CheckInService : ICheckInService
    {
        private readonly ApplicationDbContext _db;
        private readonly IBookingService _bookingService;
        private readonly IEmailService _emailService;
        private readonly IPdfService _pdfService;
        private readonly IOccupancyBroadcaster _broadcaster;
        private readonly ILogger<CheckInService> _logger;

        public CheckInService(
            ApplicationDbContext db,
            IBookingService bookingService,
            IEmailService emailService,
            IPdfService pdfService,
            IOccupancyBroadcaster broadcaster,
            ILogger<CheckInService> logger)
        {
            _db = db;
            _bookingService = bookingService;
            _emailService = emailService;
            _pdfService = pdfService;
            _broadcaster = broadcaster;
            _logger = logger;
        }

        public async Task<BookingDto> ScanAsync(string referenceNumber)
        {
            var booking = await _db.Bookings
                .FirstOrDefaultAsync(b => b.ReferenceNumber == referenceNumber.Trim().ToUpper())
                ?? throw new KeyNotFoundException($"No booking found for reference '{referenceNumber}'.");

            var dto = await _bookingService.GetByIdAsync(booking.Id, string.Empty, isStaff: true);
            return dto ?? throw new KeyNotFoundException("Booking not found.");
        }

        public async Task<BookingDto> CheckInAsync(int bookingId, string staffUserId)
        {
            var booking = await _db.Bookings
                .Include(b => b.BookingRooms)
                .FirstOrDefaultAsync(b => b.Id == bookingId)
                ?? throw new KeyNotFoundException($"Booking {bookingId} not found.");

            if (booking.Status != BookingStatus.Confirmed)
                throw new InvalidOperationException(
                    $"Cannot check in — booking status is '{booking.Status}'. Only Confirmed bookings can be checked in.");

            if (booking.CheckInDate.Date > DateTime.UtcNow.Date)
                throw new InvalidOperationException(
                    $"Check-in is not allowed before the reservation date ({booking.CheckInDate:dd MMM yyyy}).");

            // Update booking
            booking.Status = BookingStatus.CheckedIn;
            booking.UpdatedAt = DateTime.UtcNow;

            // Set all booked rooms to Occupied
            var roomIds = booking.BookingRooms.Select(br => br.RoomId).ToList();
            var rooms = await _db.Rooms.Where(r => roomIds.Contains(r.Id)).ToListAsync();
            foreach (var room in rooms)
                room.Status = RoomStatus.Occupied;

            _db.AuditLogs.Add(new AuditLog
            {
                UserId = staffUserId,
                Action = "CheckIn",
                EntityType = "Booking",
                EntityId = bookingId.ToString(),
                Details = $"Guest checked in to booking {booking.ReferenceNumber}. Rooms set to Occupied.",
                CreatedAt = DateTime.UtcNow
            });

            await _db.SaveChangesAsync();
            await _broadcaster.BroadcastAsync(booking.HotelId);

            return (await _bookingService.GetByIdAsync(bookingId, string.Empty, isStaff: true))!;
        }

        public async Task<BookingDto> CheckOutAsync(int bookingId, string staffUserId)
        {
            var booking = await _db.Bookings
                .Include(b => b.Guest)
                .Include(b => b.Hotel)
                .Include(b => b.BookingRooms)
                .FirstOrDefaultAsync(b => b.Id == bookingId)
                ?? throw new KeyNotFoundException($"Booking {bookingId} not found.");

            if (booking.Status != BookingStatus.CheckedIn)
                throw new InvalidOperationException(
                    $"Cannot check out — booking status is '{booking.Status}'. Only CheckedIn bookings can be checked out.");

            // Update booking
            booking.Status = BookingStatus.CheckedOut;
            booking.UpdatedAt = DateTime.UtcNow;

            // Return all booked rooms to Available
            var roomIds = booking.BookingRooms.Select(br => br.RoomId).ToList();
            var rooms = await _db.Rooms.Where(r => roomIds.Contains(r.Id)).ToListAsync();
            foreach (var room in rooms)
                room.Status = RoomStatus.Available;

            _db.AuditLogs.Add(new AuditLog
            {
                UserId = staffUserId,
                Action = "CheckOut",
                EntityType = "Booking",
                EntityId = bookingId.ToString(),
                Details = $"Guest checked out from booking {booking.ReferenceNumber}. Rooms set to Available.",
                CreatedAt = DateTime.UtcNow
            });

            await _db.SaveChangesAsync();
            await _broadcaster.BroadcastAsync(booking.HotelId);

            // Load full booking for PDF + email
            var fullBooking = (await _bookingService.GetByIdAsync(bookingId, string.Empty, isStaff: true))!;

            // Generate invoice and send email (non-blocking — errors are logged, not thrown)
            try
            {
                var pdfBytes = _pdfService.GenerateInvoice(fullBooking);
                await _emailService.SendInvoiceAsync(
                    booking.Guest.Email!,
                    $"{booking.Guest.FirstName} {booking.Guest.LastName}",
                    booking.ReferenceNumber,
                    booking.Hotel?.Name ?? "Hotel",
                    pdfBytes);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Invoice generation/email failed for booking {Reference}", booking.ReferenceNumber);
            }

            return fullBooking;
        }
    }
}
