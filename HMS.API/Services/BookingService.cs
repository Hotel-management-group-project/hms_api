// Student ID: S2401276
// Student Name: Mohamed Iyaadh Ahmed
// Module: Advanced Software Development (UFCF8S-30-2)

using HMS.API.Data;
using HMS.API.DTOs.Booking;
using HMS.API.Models;
using Microsoft.EntityFrameworkCore;

namespace HMS.API.Services
{
    public class BookingService : IBookingService
    {
        private readonly ApplicationDbContext _db;
        private readonly IQRCodeService _qrCodeService;
        private readonly IEmailService _emailService;
        private readonly ILogger<BookingService> _logger;

        public BookingService(
            ApplicationDbContext db,
            IQRCodeService qrCodeService,
            IEmailService emailService,
            ILogger<BookingService> logger)
        {
            _db = db;
            _qrCodeService = qrCodeService;
            _emailService = emailService;
            _logger = logger;
        }

        public async Task<IEnumerable<BookingListDto>> GetAllAsync(string userId, bool isStaff, string? referenceNumber = null)
        {
            var query = _db.Bookings
                .Include(b => b.Guest)
                .Include(b => b.Hotel)
                .Include(b => b.BookingRooms)
                .AsQueryable();

            if (!isStaff)
                query = query.Where(b => b.GuestId == userId);

            if (!string.IsNullOrWhiteSpace(referenceNumber))
                query = query.Where(b => b.ReferenceNumber == referenceNumber.Trim().ToUpper());

            var bookings = await query.OrderByDescending(b => b.CreatedAt).ToListAsync();
            return bookings.Select(ToListDto);
        }

        public async Task<BookingDto?> GetByIdAsync(int id, string userId, bool isStaff)
        {
            var booking = await LoadFullBookingAsync(id);
            if (booking == null) return null;
            if (!isStaff && booking.GuestId != userId) return null;
            return ToDto(booking);
        }

        public async Task<BookingDto> CreateAsync(string guestId, CreateBookingDto dto)
        {
            // ── Validate dates ────────────────────────────────────────────────
            if (dto.CheckInDate.Date < DateTime.UtcNow.Date)
                throw new ArgumentException("Check-in date cannot be in the past.");
            if (dto.CheckOutDate.Date <= dto.CheckInDate.Date)
                throw new ArgumentException("Check-out date must be after check-in date.");

            // ── Validate hotel ────────────────────────────────────────────────
            var hotel = await _db.Hotels.FirstOrDefaultAsync(h => h.Id == dto.HotelId && h.IsActive)
                ?? throw new KeyNotFoundException($"Hotel {dto.HotelId} not found or is inactive.");

            // ── Validate rooms ────────────────────────────────────────────────
            var requestedRoomIds = dto.RoomIds.Distinct().ToList();
            if (requestedRoomIds.Count == 0)
                throw new ArgumentException("At least one room must be selected.");

            var rooms = await _db.Rooms
                .Where(r => requestedRoomIds.Contains(r.Id) && r.HotelId == dto.HotelId)
                .ToListAsync();

            if (rooms.Count != requestedRoomIds.Count)
                throw new ArgumentException("One or more rooms do not belong to the specified hotel.");

            var outOfServiceRooms = rooms.Where(r => r.Status == RoomStatus.OutOfService).ToList();
            if (outOfServiceRooms.Count != 0)
                throw new InvalidOperationException(
                    $"Room(s) {string.Join(", ", outOfServiceRooms.Select(r => r.RoomNumber))} are out of service.");

            // ── Availability check (no double booking) ────────────────────────
            var overlappingRoomIds = await _db.BookingRooms
                .Where(br =>
                    requestedRoomIds.Contains(br.RoomId) &&
                    (br.Booking.Status == BookingStatus.Pending ||
                     br.Booking.Status == BookingStatus.Confirmed ||
                     br.Booking.Status == BookingStatus.CheckedIn) &&
                    br.Booking.CheckInDate < dto.CheckOutDate &&
                    br.Booking.CheckOutDate > dto.CheckInDate)
                .Select(br => br.RoomId)
                .ToListAsync();

            if (overlappingRoomIds.Count != 0)
            {
                var conflictNumbers = rooms
                    .Where(r => overlappingRoomIds.Contains(r.Id))
                    .Select(r => r.RoomNumber);
                throw new InvalidOperationException(
                    $"Room(s) {string.Join(", ", conflictNumbers)} are already booked for the selected dates.");
            }

            // ── Calculate prices ──────────────────────────────────────────────
            var roomTotal = CalculateRoomTotal(rooms, dto.CheckInDate, dto.CheckOutDate);

            var serviceEntries = new List<BookingAncillaryService>();
            var serviceTotal = 0m;

            if (dto.AncillaryServices is { Count: > 0 })
            {
                var serviceIds = dto.AncillaryServices.Select(s => s.ServiceId).Distinct().ToList();
                var services = await _db.AncillaryServices
                    .Where(s => serviceIds.Contains(s.Id))
                    .ToListAsync();

                if (services.Count != serviceIds.Count)
                    throw new ArgumentException("One or more ancillary services not found.");

                foreach (var request in dto.AncillaryServices)
                {
                    var service = services.First(s => s.Id == request.ServiceId);
                    var lineTotal = service.Price * request.Quantity;
                    serviceTotal += lineTotal;
                    serviceEntries.Add(new BookingAncillaryService
                    {
                        AncillaryServiceId = request.ServiceId,
                        Quantity = request.Quantity,
                        TotalPrice = lineTotal
                    });
                }
            }

            var totalPrice = roomTotal + serviceTotal;

            // ── Generate reference number and QR code ─────────────────────────
            var referenceNumber = await GenerateReferenceNumberAsync();
            var qrCodeUrl = _qrCodeService.GenerateBase64(referenceNumber);

            // ── Persist booking ───────────────────────────────────────────────
            var booking = new Booking
            {
                GuestId = guestId,
                HotelId = dto.HotelId,
                ReferenceNumber = referenceNumber,
                CheckInDate = dto.CheckInDate,
                CheckOutDate = dto.CheckOutDate,
                TotalPrice = totalPrice,
                Status = BookingStatus.Pending,
                CancellationFee = 0,
                QrCodeUrl = qrCodeUrl,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _db.Bookings.Add(booking);
            await _db.SaveChangesAsync(); // get booking.Id

            foreach (var roomId in requestedRoomIds)
                _db.BookingRooms.Add(new BookingRoom { BookingId = booking.Id, RoomId = roomId });

            foreach (var entry in serviceEntries)
            {
                entry.BookingId = booking.Id;
                _db.BookingAncillaryServices.Add(entry);
            }

            _db.AuditLogs.Add(new AuditLog
            {
                UserId = guestId,
                Action = "BookingCreated",
                EntityType = "Booking",
                EntityId = booking.Id.ToString(),
                Details = $"Booking {referenceNumber} created for hotel {hotel.Name}. Total: £{totalPrice:F2}",
                CreatedAt = DateTime.UtcNow
            });

            await _db.SaveChangesAsync();

            var full = await LoadFullBookingAsync(booking.Id);
            return ToDto(full!);
        }

        public async Task<BookingDto> UpdateAsync(int id, UpdateBookingDto dto)
        {
            if (!Enum.TryParse<BookingStatus>(dto.Status, true, out var newStatus))
                throw new ArgumentException($"Invalid status '{dto.Status}'.");

            // PUT is used to confirm a Pending booking.
            // Check-in/out and cancellation use their own endpoints.
            if (newStatus != BookingStatus.Confirmed)
                throw new ArgumentException(
                    "Only 'Confirmed' may be set via this endpoint. " +
                    "Use DELETE for cancellation and the check-in/out endpoints for those transitions.");

            var booking = await LoadFullBookingAsync(id)
                ?? throw new KeyNotFoundException($"Booking {id} not found.");

            if (booking.Status != BookingStatus.Pending)
                throw new InvalidOperationException(
                    $"Only Pending bookings can be confirmed. Current status: {booking.Status}.");

            booking.Status = BookingStatus.Confirmed;
            booking.UpdatedAt = DateTime.UtcNow;

            _db.AuditLogs.Add(new AuditLog
            {
                UserId = booking.GuestId,
                Action = "BookingConfirmed",
                EntityType = "Booking",
                EntityId = id.ToString(),
                Details = $"Booking {booking.ReferenceNumber} confirmed by staff.",
                CreatedAt = DateTime.UtcNow
            });

            await _db.SaveChangesAsync();
            return ToDto(booking);
        }

        public async Task<BookingDto> CancelAsync(int id, string userId, bool isStaff, CancelBookingDto? body)
        {
            var booking = await _db.Bookings
                .Include(b => b.Guest)
                .Include(b => b.Hotel)
                .Include(b => b.BookingRooms).ThenInclude(br => br.Room)
                .Include(b => b.BookingAncillaryServices).ThenInclude(bas => bas.AncillaryService)
                .FirstOrDefaultAsync(b => b.Id == id)
                ?? throw new KeyNotFoundException($"Booking {id} not found.");

            // Guests may only cancel their own bookings
            if (!isStaff && booking.GuestId != userId)
                throw new UnauthorizedAccessException("You do not have permission to cancel this booking.");

            if (booking.Status == BookingStatus.CheckedIn)
                throw new InvalidOperationException("Cannot cancel a booking that is currently checked in.");
            if (booking.Status == BookingStatus.CheckedOut)
                throw new InvalidOperationException("Cannot cancel a booking that has already been checked out.");
            if (booking.Status == BookingStatus.Cancelled)
                throw new InvalidOperationException("This booking is already cancelled.");

            var cancellationFee = CalculateCancellationFee(booking);

            booking.Status = BookingStatus.Cancelled;
            booking.CancellationFee = cancellationFee;
            booking.UpdatedAt = DateTime.UtcNow;

            var reasonNote = string.IsNullOrWhiteSpace(body?.Reason) ? "" : $" Reason: {body.Reason}";
            _db.AuditLogs.Add(new AuditLog
            {
                UserId = userId,
                Action = "BookingCancelled",
                EntityType = "Booking",
                EntityId = id.ToString(),
                Details = $"Booking {booking.ReferenceNumber} cancelled. Fee: £{cancellationFee:F2}.{reasonNote}",
                CreatedAt = DateTime.UtcNow
            });

            await _db.SaveChangesAsync();

            try
            {
                await _emailService.SendCancellationEmailAsync(
                    booking.Guest.Email!,
                    $"{booking.Guest.FirstName} {booking.Guest.LastName}",
                    booking.ReferenceNumber,
                    booking.Hotel?.Name ?? "Hotel",
                    booking.CheckInDate,
                    booking.CheckOutDate,
                    cancellationFee);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Cancellation email failed for booking {Reference}", booking.ReferenceNumber);
            }

            return ToDto(booking);
        }

        public async Task<BookingDto> MarkNoShowAsync(int id, string actorUserId)
        {
            var booking = await _db.Bookings
                .Include(b => b.Guest)
                .Include(b => b.Hotel)
                .Include(b => b.BookingRooms).ThenInclude(br => br.Room)
                .Include(b => b.BookingAncillaryServices).ThenInclude(bas => bas.AncillaryService)
                .Include(b => b.Payment)
                .FirstOrDefaultAsync(b => b.Id == id)
                ?? throw new KeyNotFoundException($"Booking {id} not found.");

            if (booking.Status != BookingStatus.Confirmed)
                throw new InvalidOperationException(
                    $"Only Confirmed bookings can be marked as no-show. Current status: {booking.Status}.");

            var noShowFee = booking.TotalPrice;

            booking.Status = BookingStatus.NoShow;
            booking.CancellationFee = noShowFee;
            booking.UpdatedAt = DateTime.UtcNow;

            if (booking.Payment != null)
            {
                booking.Payment.Amount = noShowFee;
                booking.Payment.Status = PaymentStatus.Completed;
                booking.Payment.Method = "NoShowFee";
                booking.Payment.ProcessedAt = DateTime.UtcNow;
            }
            else
            {
                _db.Payments.Add(new Payment
                {
                    BookingId = booking.Id,
                    Amount = noShowFee,
                    Method = "NoShowFee",
                    Status = PaymentStatus.Completed,
                    TransactionReference = $"NOSHOW-{booking.ReferenceNumber}",
                    ProcessedAt = DateTime.UtcNow
                });
            }

            _db.AuditLogs.Add(new AuditLog
            {
                UserId = actorUserId,
                Action = "BookingNoShow",
                EntityType = "Booking",
                EntityId = id.ToString(),
                Details = $"Booking {booking.ReferenceNumber} marked as no-show. Fee charged: £{noShowFee:F2}.",
                CreatedAt = DateTime.UtcNow
            });

            await _db.SaveChangesAsync();
            return ToDto(booking);
        }

        // ── Peak / pricing helpers ─────────────────────────────────────────────

        private static bool IsPeakMonth(int month) => month is 6 or 7 or 8 or 12;

        private static decimal NightlyRate(Room room, DateTime night) =>
            IsPeakMonth(night.Month) ? room.PricePeak : room.PriceOffPeak;

        private static decimal CalculateRoomTotal(IEnumerable<Room> rooms, DateTime checkIn, DateTime checkOut)
        {
            var total = 0m;
            var night = checkIn.Date;
            while (night < checkOut.Date)
            {
                total += rooms.Sum(r => NightlyRate(r, night));
                night = night.AddDays(1);
            }
            return total;
        }

        // ── Cancellation fee ───────────────────────────────────────────────────

        private static decimal CalculateCancellationFee(Booking booking)
        {
            var daysUntilCheckIn = (booking.CheckInDate.Date - DateTime.UtcNow.Date).TotalDays;

            // No-show: check-in date has already passed
            if (daysUntilCheckIn < 0)
                return booking.TotalPrice;

            // Free cancellation: 14+ days' notice
            if (daysUntilCheckIn >= 14)
                return 0m;

            // First night cost across all booked rooms at check-in date's rate
            var firstNightTotal = booking.BookingRooms
                .Sum(br => IsPeakMonth(booking.CheckInDate.Month) ? br.Room.PricePeak : br.Room.PriceOffPeak);

            // 3–14 days: 50 % of first night
            if (daysUntilCheckIn >= 3)
                return Math.Round(firstNightTotal * 0.5m, 2);

            // < 72 hours: 100 % of first night
            return firstNightTotal;
        }

        // ── Reference number ───────────────────────────────────────────────────

        private async Task<string> GenerateReferenceNumberAsync()
        {
            var year = DateTime.UtcNow.Year;
            var prefix = $"HMS-{year}-";

            for (var attempt = 0; attempt < 10; attempt++)
            {
                var count = await _db.Bookings.CountAsync(b => b.ReferenceNumber.StartsWith(prefix))
                            + 1 + attempt;
                var candidate = $"{prefix}{count:D5}";

                if (!await _db.Bookings.AnyAsync(b => b.ReferenceNumber == candidate))
                    return candidate;
            }

            // Extremely unlikely fallback — append microseconds to stay unique
            return $"{prefix}{DateTime.UtcNow:ffffff}";
        }

        // ── Query helpers ──────────────────────────────────────────────────────

        private Task<Booking?> LoadFullBookingAsync(int id) =>
            _db.Bookings
                .Include(b => b.Guest)
                .Include(b => b.Hotel)
                .Include(b => b.BookingRooms).ThenInclude(br => br.Room)
                .Include(b => b.BookingAncillaryServices).ThenInclude(bas => bas.AncillaryService)
                .FirstOrDefaultAsync(b => b.Id == id);

        // ── Mapping ────────────────────────────────────────────────────────────

        private static BookingDto ToDto(Booking b) => new()
        {
            Id = b.Id,
            ReferenceNumber = b.ReferenceNumber,
            GuestId = b.GuestId,
            GuestName = $"{b.Guest.FirstName} {b.Guest.LastName}",
            GuestEmail = b.Guest.Email ?? string.Empty,
            HotelId = b.HotelId,
            HotelName = b.Hotel?.Name ?? string.Empty,
            CheckInDate = b.CheckInDate,
            CheckOutDate = b.CheckOutDate,
            Nights = (int)(b.CheckOutDate.Date - b.CheckInDate.Date).TotalDays,
            TotalPrice = b.TotalPrice,
            Status = b.Status.ToString(),
            CancellationFee = b.CancellationFee,
            QrCodeUrl = b.QrCodeUrl,
            Rooms = b.BookingRooms.Select(br => new BookingRoomDto
            {
                RoomId = br.RoomId,
                RoomNumber = br.Room.RoomNumber,
                RoomType = br.Room.Type.ToString(),
                Floor = br.Room.Floor,
                PricePerNight = IsPeakMonth(b.CheckInDate.Month) ? br.Room.PricePeak : br.Room.PriceOffPeak,
                Capacity = br.Room.Capacity,
                PriceOffPeak = br.Room.PriceOffPeak,
                PricePeak = br.Room.PricePeak,
                Status = br.Room.Status.ToString(),
                Description = br.Room.Description,
                ImageUrls = string.IsNullOrEmpty(br.Room.ImageUrls) ? [] : [br.Room.ImageUrls]
            }).ToList(),
            AncillaryServices = b.BookingAncillaryServices.Select(bas => new BookingServiceDto
            {
                ServiceId = bas.AncillaryServiceId,
                ServiceName = bas.AncillaryService.Name,
                Quantity = bas.Quantity,
                UnitPrice = bas.AncillaryService.Price,
                TotalPrice = bas.TotalPrice
            }).ToList(),
            CreatedAt = b.CreatedAt,
            UpdatedAt = b.UpdatedAt
        };

        private static BookingListDto ToListDto(Booking b) => new()
        {
            Id = b.Id,
            ReferenceNumber = b.ReferenceNumber,
            GuestName = $"{b.Guest.FirstName} {b.Guest.LastName}",
            GuestEmail = b.Guest.Email ?? string.Empty,
            HotelName = b.Hotel?.Name ?? string.Empty,
            CheckInDate = b.CheckInDate,
            CheckOutDate = b.CheckOutDate,
            Nights = (int)(b.CheckOutDate.Date - b.CheckInDate.Date).TotalDays,
            TotalPrice = b.TotalPrice,
            Status = b.Status.ToString(),
            RoomCount = b.BookingRooms.Count,
            CreatedAt = b.CreatedAt
        };
    }
}
