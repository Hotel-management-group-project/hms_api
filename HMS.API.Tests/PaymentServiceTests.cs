using HMS.API.DTOs.Booking;
using HMS.API.DTOs.Payment;
using HMS.API.Models;
using HMS.API.Services;
using HMS.API.Tests.Helpers;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace HMS.API.Tests;

public class PaymentServiceTests
{
    // ── Shared helpers ─────────────────────────────────────────────────────

    private static BookingService BuildBookingSvc(HMS.API.Data.ApplicationDbContext db)
    {
        var qr = new Mock<IQRCodeService>();
        qr.Setup(q => q.GenerateBase64(It.IsAny<string>())).Returns("data:image/png;base64,fake");

        var email = new Mock<IEmailService>();
        email.Setup(e => e.SendBookingConfirmationAsync(
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<DateTime>(),
            It.IsAny<DateTime>(), It.IsAny<decimal>(), It.IsAny<string>()))
            .Returns(Task.CompletedTask);

        return new BookingService(db, qr.Object, email.Object, NullLogger<BookingService>.Instance);
    }

    private static PaymentService BuildPaymentSvc(HMS.API.Data.ApplicationDbContext db)
    {
        var email = new Mock<IEmailService>();
        email.Setup(e => e.SendBookingConfirmationAsync(
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<DateTime>(),
            It.IsAny<DateTime>(), It.IsAny<decimal>(), It.IsAny<string>()))
            .Returns(Task.CompletedTask);

        return new PaymentService(db, email.Object, NullLogger<PaymentService>.Instance);
    }

    private static AncillaryService SeedService(HMS.API.Data.ApplicationDbContext db, int id, string name, decimal price)
    {
        var svc = new AncillaryService { Id = id, Name = name, Price = price };
        db.AncillaryServices.Add(svc);
        db.SaveChanges();
        return svc;
    }

    // ── Test 1 ─────────────────────────────────────────────────────────────

    [Fact]
    public async Task Payment_AmountMatchesBookingTotal()
    {
        // Arrange
        using var db = TestDbContextFactory.Create(nameof(Payment_AmountMatchesBookingTotal));
        var (hotel, room, guest) = TestDbContextFactory.SeedBasic(db);
        var bookingSvc = BuildBookingSvc(db);
        var paymentSvc = BuildPaymentSvc(db);

        var checkIn = TestDbContextFactory.NextOffPeakDate(30);
        var booking = await bookingSvc.CreateAsync(guest.Id, new CreateBookingDto
        {
            HotelId = hotel.Id,
            RoomIds = [room.Id],
            CheckInDate = checkIn,
            CheckOutDate = checkIn.AddDays(2)
        });

        var payDto = new ProcessPaymentDto
        {
            BookingId = booking.Id,
            Amount = booking.TotalPrice,
            Method = "Mock"
        };

        // Act
        var result = await paymentSvc.ProcessAsync(payDto, guest.Id);

        // Assert — payment amount always stored as submitted amount regardless of mock success/failure
        Assert.Equal(booking.TotalPrice, result.Payment.Amount);
    }

    // ── Test 2 ─────────────────────────────────────────────────────────────

    [Fact]
    public async Task Payment_WithAncillaryServices_IncludesServiceTotal()
    {
        // Room: Standard Double, 2 nights off-peak = 2 × £120 = £240
        // Airport Transfer: £50
        // Expected total: £290

        // Arrange
        using var db = TestDbContextFactory.Create(nameof(Payment_WithAncillaryServices_IncludesServiceTotal));
        var (hotel, room, guest) = TestDbContextFactory.SeedBasic(db, priceOffPeak: 120m);
        SeedService(db, 1, "Airport Transfer (one-way)", 50m);
        var bookingSvc = BuildBookingSvc(db);

        var checkIn = TestDbContextFactory.NextOffPeakDate(30);
        var booking = await bookingSvc.CreateAsync(guest.Id, new CreateBookingDto
        {
            HotelId = hotel.Id,
            RoomIds = [room.Id],
            CheckInDate = checkIn,
            CheckOutDate = checkIn.AddDays(2),
            AncillaryServices =
            [
                new AncillaryServiceRequestDto { ServiceId = 1, Quantity = 1 }
            ]
        });

        // Assert
        Assert.Equal(290m, booking.TotalPrice);
    }

    // ── Test 3 ─────────────────────────────────────────────────────────────

    [Fact]
    public async Task Payment_WithBreakfast_TwoGuests_TwoNights_CorrectTotal()
    {
        // Room: Standard Double, 2 nights off-peak = 2 × £120 = £240
        // Breakfast: £20 × 2 guests × 2 days = quantity 4 = £80
        // Expected total: £320

        // Arrange
        using var db = TestDbContextFactory.Create(nameof(Payment_WithBreakfast_TwoGuests_TwoNights_CorrectTotal));
        var (hotel, room, guest) = TestDbContextFactory.SeedBasic(db, priceOffPeak: 120m);
        SeedService(db, 1, "Full English Breakfast (per person/day)", 20m);
        var bookingSvc = BuildBookingSvc(db);

        var checkIn = TestDbContextFactory.NextOffPeakDate(30);
        var booking = await bookingSvc.CreateAsync(guest.Id, new CreateBookingDto
        {
            HotelId = hotel.Id,
            RoomIds = [room.Id],
            CheckInDate = checkIn,
            CheckOutDate = checkIn.AddDays(2),
            AncillaryServices =
            [
                new AncillaryServiceRequestDto { ServiceId = 1, Quantity = 4 } // 2 guests × 2 days
            ]
        });

        // Assert
        Assert.Equal(320m, booking.TotalPrice);
    }

    // ── Test 4 ─────────────────────────────────────────────────────────────

    [Fact]
    public async Task Payment_WithAllAncillaryServices_CorrectTotal()
    {
        // Room: Standard Double, 1 night off-peak = £120
        // Airport Transfer: £50 × 1 = £50
        // Breakfast: £20 × 2 guests = £40
        // Spa: £35 × 2 guests = £70
        // Late Checkout: £40 × 1 = £40
        // Expected total: £120 + £50 + £40 + £70 + £40 = £320

        // Arrange
        using var db = TestDbContextFactory.Create(nameof(Payment_WithAllAncillaryServices_CorrectTotal));
        var (hotel, room, guest) = TestDbContextFactory.SeedBasic(db, priceOffPeak: 120m);
        SeedService(db, 1, "Airport Transfer (one-way)", 50m);
        SeedService(db, 2, "Full English Breakfast (per person/day)", 20m);
        SeedService(db, 3, "Spa Access (per person/day)", 35m);
        SeedService(db, 4, "Late Check-out (until 2PM)", 40m);
        var bookingSvc = BuildBookingSvc(db);

        var checkIn = TestDbContextFactory.NextOffPeakDate(30);
        var booking = await bookingSvc.CreateAsync(guest.Id, new CreateBookingDto
        {
            HotelId = hotel.Id,
            RoomIds = [room.Id],
            CheckInDate = checkIn,
            CheckOutDate = checkIn.AddDays(1), // 1 night
            AncillaryServices =
            [
                new AncillaryServiceRequestDto { ServiceId = 1, Quantity = 1 }, // transfer
                new AncillaryServiceRequestDto { ServiceId = 2, Quantity = 2 }, // 2 breakfasts
                new AncillaryServiceRequestDto { ServiceId = 3, Quantity = 2 }, // 2 spa
                new AncillaryServiceRequestDto { ServiceId = 4, Quantity = 1 }  // late checkout
            ]
        });

        // Assert
        Assert.Equal(320m, booking.TotalPrice);
    }
}
