using HMS.API.DTOs.Booking;
using HMS.API.Models;
using HMS.API.Services;
using HMS.API.Tests.Helpers;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace HMS.API.Tests;

public class BookingServiceTests
{
    private static BookingService BuildService(HMS.API.Data.ApplicationDbContext db)
    {
        var qr = new Mock<IQRCodeService>();
        qr.Setup(q => q.GenerateBase64(It.IsAny<string>())).Returns("data:image/png;base64,fake");

        var email = new Mock<IEmailService>();
        email.Setup(e => e.SendBookingConfirmationAsync(
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<DateTime>(),
            It.IsAny<DateTime>(), It.IsAny<decimal>(), It.IsAny<string>()))
            .Returns(Task.CompletedTask);
        email.Setup(e => e.SendCancellationEmailAsync(
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
            It.IsAny<string>(), It.IsAny<DateTime>(), It.IsAny<DateTime>(),
            It.IsAny<decimal>(), It.IsAny<decimal>()))
            .Returns(Task.CompletedTask);

        return new BookingService(db, qr.Object, email.Object, NullLogger<BookingService>.Instance);
    }

    // ── Test 1 ─────────────────────────────────────────────────────────────

    [Fact]
    public async Task CreateBookingAsync_ValidDates_ReturnsBookingDto()
    {
        // Arrange
        using var db = TestDbContextFactory.Create(nameof(CreateBookingAsync_ValidDates_ReturnsBookingDto));
        var (hotel, room, guest) = TestDbContextFactory.SeedBasic(db);
        var svc = BuildService(db);

        var checkIn = TestDbContextFactory.NextOffPeakDate(30);
        var dto = new CreateBookingDto
        {
            HotelId = hotel.Id,
            RoomIds = [room.Id],
            CheckInDate = checkIn,
            CheckOutDate = checkIn.AddDays(2)
        };

        // Act
        var result = await svc.CreateAsync(guest.Id, dto);

        // Assert
        Assert.NotNull(result);
        Assert.Matches(@"^HMS-\d{4}-\d{5}$", result.ReferenceNumber);
        Assert.True(result.TotalPrice > 0);
        Assert.Equal("Pending", result.Status);
    }

    // ── Test 2 ─────────────────────────────────────────────────────────────

    [Fact]
    public async Task CreateBookingAsync_CheckInInPast_ThrowsException()
    {
        // Arrange
        using var db = TestDbContextFactory.Create(nameof(CreateBookingAsync_CheckInInPast_ThrowsException));
        var (hotel, room, guest) = TestDbContextFactory.SeedBasic(db);
        var svc = BuildService(db);

        var dto = new CreateBookingDto
        {
            HotelId = hotel.Id,
            RoomIds = [room.Id],
            CheckInDate = DateTime.UtcNow.Date.AddDays(-1),
            CheckOutDate = DateTime.UtcNow.Date.AddDays(1)
        };

        // Act & Assert
        await Assert.ThrowsAnyAsync<ArgumentException>(() => svc.CreateAsync(guest.Id, dto));
    }

    // ── Test 3 ─────────────────────────────────────────────────────────────

    [Fact]
    public async Task CreateBookingAsync_CheckOutBeforeCheckIn_ThrowsException()
    {
        // Arrange
        using var db = TestDbContextFactory.Create(nameof(CreateBookingAsync_CheckOutBeforeCheckIn_ThrowsException));
        var (hotel, room, guest) = TestDbContextFactory.SeedBasic(db);
        var svc = BuildService(db);

        var checkIn = DateTime.UtcNow.Date.AddDays(10);
        var dto = new CreateBookingDto
        {
            HotelId = hotel.Id,
            RoomIds = [room.Id],
            CheckInDate = checkIn,
            CheckOutDate = checkIn.AddDays(-1)
        };

        // Act & Assert
        await Assert.ThrowsAnyAsync<ArgumentException>(() => svc.CreateAsync(guest.Id, dto));
    }

    // ── Test 4 ─────────────────────────────────────────────────────────────

    [Fact]
    public async Task CreateBookingAsync_RoomAlreadyBooked_ThrowsException()
    {
        // Arrange
        using var db = TestDbContextFactory.Create(nameof(CreateBookingAsync_RoomAlreadyBooked_ThrowsException));
        var (hotel, room, guest) = TestDbContextFactory.SeedBasic(db);
        var svc = BuildService(db);

        var checkIn = TestDbContextFactory.NextOffPeakDate(30);
        var checkOut = checkIn.AddDays(3);

        // Seed a confirmed booking that occupies the room on those dates
        var existing = new Booking
        {
            GuestId = guest.Id,
            HotelId = hotel.Id,
            ReferenceNumber = "HMS-2026-00001",
            CheckInDate = checkIn,
            CheckOutDate = checkOut,
            TotalPrice = 240m,
            Status = BookingStatus.Confirmed
        };
        db.Bookings.Add(existing);
        await db.SaveChangesAsync();
        db.BookingRooms.Add(new BookingRoom { BookingId = existing.Id, RoomId = room.Id });
        await db.SaveChangesAsync();

        var dto = new CreateBookingDto
        {
            HotelId = hotel.Id,
            RoomIds = [room.Id],
            CheckInDate = checkIn.AddDays(1),   // overlapping window
            CheckOutDate = checkOut.AddDays(1)
        };

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() => svc.CreateAsync(guest.Id, dto));
    }

    // ── Test 5 ─────────────────────────────────────────────────────────────

    [Fact]
    public async Task CancelBookingAsync_AlreadyCheckedIn_ThrowsException()
    {
        // Arrange
        using var db = TestDbContextFactory.Create(nameof(CancelBookingAsync_AlreadyCheckedIn_ThrowsException));
        var (hotel, room, guest) = TestDbContextFactory.SeedBasic(db);
        var svc = BuildService(db);

        var checkIn = TestDbContextFactory.NextOffPeakDate(30);
        var booking = new Booking
        {
            GuestId = guest.Id,
            HotelId = hotel.Id,
            ReferenceNumber = "HMS-2026-00002",
            CheckInDate = checkIn,
            CheckOutDate = checkIn.AddDays(2),
            TotalPrice = 240m,
            Status = BookingStatus.CheckedIn
        };
        db.Bookings.Add(booking);
        db.BookingRooms.Add(new BookingRoom { Booking = booking, Room = room });
        await db.SaveChangesAsync();

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => svc.CancelAsync(booking.Id, guest.Id, isStaff: false, body: null));
    }

    // ── Test 6 ─────────────────────────────────────────────────────────────

    [Fact]
    public async Task CancelBookingAsync_ValidBookingMoreThan14Days_SetsCancelledWithZeroFee()
    {
        // Arrange
        using var db = TestDbContextFactory.Create(nameof(CancelBookingAsync_ValidBookingMoreThan14Days_SetsCancelledWithZeroFee));
        var (hotel, room, guest) = TestDbContextFactory.SeedBasic(db);
        var svc = BuildService(db);

        var checkIn = TestDbContextFactory.NextOffPeakDate(20); // 20+ days out → > 14 days → free
        var booking = new Booking
        {
            GuestId = guest.Id,
            HotelId = hotel.Id,
            ReferenceNumber = "HMS-2026-00003",
            CheckInDate = checkIn,
            CheckOutDate = checkIn.AddDays(2),
            TotalPrice = 240m,
            Status = BookingStatus.Confirmed
        };
        db.Bookings.Add(booking);
        db.BookingRooms.Add(new BookingRoom { Booking = booking, Room = room });
        await db.SaveChangesAsync();

        // Act
        var result = await svc.CancelAsync(booking.Id, guest.Id, isStaff: false, body: null);

        // Assert
        Assert.Equal("Cancelled", result.Status);
        Assert.Equal(0m, result.CancellationFee);
    }
}
