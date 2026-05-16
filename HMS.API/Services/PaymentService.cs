
using HMS.API.Data;
using HMS.API.DTOs.Payment;
using HMS.API.Models;
using Microsoft.EntityFrameworkCore;

namespace HMS.API.Services
{
    public class PaymentService : IPaymentService
    {
        private readonly ApplicationDbContext _db;
        private readonly IEmailService _emailService;
        private readonly ILogger<PaymentService> _logger;

        public PaymentService(ApplicationDbContext db, IEmailService emailService, ILogger<PaymentService> logger)
        {
            _db = db;
            _emailService = emailService;
            _logger = logger;
        }

        public async Task<PaymentResponseDto> ProcessAsync(ProcessPaymentDto dto, string userId)
        {
            // ── Load and validate booking ──────────────────────────────────────
            var booking = await _db.Bookings
                .Include(b => b.Guest)
                .Include(b => b.Hotel)
                .FirstOrDefaultAsync(b => b.Id == dto.BookingId)
                ?? throw new KeyNotFoundException($"Booking {dto.BookingId} not found.");

            if (booking.Status != BookingStatus.Pending)
                throw new InvalidOperationException(
                    $"Only Pending bookings can be paid. Current status: {booking.Status}.");

            var alreadyPaid = await _db.Payments.AnyAsync(p =>
                p.BookingId == dto.BookingId && p.Status == PaymentStatus.Completed);
            if (alreadyPaid)
                throw new InvalidOperationException("This booking already has a completed payment.");

            if (Math.Abs(dto.Amount - booking.TotalPrice) > 0.01m)
                throw new ArgumentException(
                    $"Payment amount £{dto.Amount:F2} does not match booking total £{booking.TotalPrice:F2}.");

            // ── Mock processing (95 % success) ────────────────────────────────
            var success = Random.Shared.Next(1, 101) <= 95;
            var transactionRef = success ? GenerateTransactionRef() : null;

            // ── Persist payment record ─────────────────────────────────────────
            var payment = new Payment
            {
                BookingId = dto.BookingId,
                Amount = dto.Amount,
                Method = dto.Method,
                Status = success ? PaymentStatus.Completed : PaymentStatus.Pending,
                TransactionReference = transactionRef,
                ProcessedAt = DateTime.UtcNow
            };

            _db.Payments.Add(payment);

            if (success)
            {
                booking.Status = BookingStatus.Confirmed;
                booking.UpdatedAt = DateTime.UtcNow;
            }

            _db.AuditLogs.Add(new AuditLog
            {
                UserId = userId,
                Action = success ? "PaymentCompleted" : "PaymentFailed",
                EntityType = "Payment",
                EntityId = dto.BookingId.ToString(),
                Details = success
                    ? $"Payment {transactionRef} of £{dto.Amount:F2} for booking {booking.ReferenceNumber} succeeded."
                    : $"Payment attempt of £{dto.Amount:F2} for booking {booking.ReferenceNumber} failed (mock).",
                CreatedAt = DateTime.UtcNow
            });

            await _db.SaveChangesAsync();

            // ── Send confirmation email on success ────────────────────────────
            if (success)
            {
                try
                {
                    var roomTypes = string.Join(", ", booking.BookingRooms
                        .Select(br => br.Room.Type.ToString())
                        .Distinct());
                    await _emailService.SendBookingConfirmationAsync(
                        booking.Guest.Email!,
                        $"{booking.Guest.FirstName} {booking.Guest.LastName}",
                        booking.ReferenceNumber,
                        booking.Hotel?.Name ?? "Hotel",
                        roomTypes,
                        booking.CheckInDate,
                        booking.CheckOutDate,
                        booking.TotalPrice,
                        booking.QrCodeUrl ?? string.Empty);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex,
                        "Confirmation email failed for booking {Reference}", booking.ReferenceNumber);
                }
            }

            return new PaymentResponseDto
            {
                Success = success,
                Message = success
                    ? "Payment processed successfully. Confirmation email sent."
                    : "Payment declined by processor. Please try again.",
                Payment = new PaymentDto
                {
                    Id = payment.Id,
                    BookingId = payment.BookingId,
                    BookingReferenceNumber = booking.ReferenceNumber,
                    Amount = payment.Amount,
                    Method = payment.Method,
                    Status = payment.Status.ToString(),
                    TransactionReference = payment.TransactionReference,
                    ProcessedAt = payment.ProcessedAt
                },
                BookingStatus = booking.Status.ToString()
            };
        }

        public async Task<PaymentDto?> GetByBookingAsync(int bookingId)
        {
            var payment = await _db.Payments
                .Include(p => p.Booking)
                .FirstOrDefaultAsync(p => p.BookingId == bookingId);

            return payment == null ? null : new PaymentDto
            {
                Id = payment.Id,
                BookingId = payment.BookingId,
                BookingReferenceNumber = payment.Booking.ReferenceNumber,
                Amount = payment.Amount,
                Method = payment.Method,
                Status = payment.Status.ToString(),
                TransactionReference = payment.TransactionReference,
                ProcessedAt = payment.ProcessedAt
            };
        }

        // ── Helpers ────────────────────────────────────────────────────────────

        private static string GenerateTransactionRef()
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
            var suffix = new string(Enumerable.Range(0, 6)
                .Select(_ => chars[Random.Shared.Next(chars.Length)])
                .ToArray());
            return $"HMS-PAY-{suffix}";
        }
    }
}
