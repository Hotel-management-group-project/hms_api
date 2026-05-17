using HMS.API.Models;
using HMS.API.Services;

namespace HMS.API.Tests;

public class CancellationFeeTests
{
    private static readonly DateTime Now = new(2026, 5, 17, 12, 0, 0, DateTimeKind.Utc);

    private static Booking MakeBooking(DateTime checkIn, BookingStatus status = BookingStatus.Confirmed, decimal totalPrice = 500m)
    {
        var room = new Room { Id = 1, PriceOffPeak = 120m, PricePeak = 180m };
        return new Booking
        {
            Id = 1,
            Status = status,
            CheckInDate = checkIn,
            CheckOutDate = checkIn.AddDays(2),
            TotalPrice = totalPrice,
            BookingRooms = [new BookingRoom { RoomId = 1, Room = room }]
        };
    }

    // ── NoShow ─────────────────────────────────────────────────────────────

    [Fact]
    public void NoShow_Returns100PercentOfBookingTotal()
    {
        var booking = MakeBooking(Now.AddDays(5), BookingStatus.NoShow, totalPrice: 500m);
        var fee = CancellationFeeCalculator.CalculateCancellationFee(booking, Now);
        Assert.Equal(500m, fee);
    }

    [Fact]
    public void NoShow_SameDayCheckIn_Returns100PercentOfBookingTotal()
    {
        // Same-day no-show would have been missed by the old daysUntilCheckIn < 0 guard
        var booking = MakeBooking(Now.Date, BookingStatus.NoShow, totalPrice: 360m);
        var fee = CancellationFeeCalculator.CalculateCancellationFee(booking, Now);
        Assert.Equal(360m, fee);
    }

    // ── More than 14 days ──────────────────────────────────────────────────

    [Fact]
    public void MoreThan14Days_ReturnsZeroFee()
    {
        var booking = MakeBooking(Now.AddDays(15));
        var fee = CancellationFeeCalculator.CalculateCancellationFee(booking, Now);
        Assert.Equal(0m, fee);
    }

    [Fact]
    public void Exactly14Days_Returns50PercentOfFirstNight()
    {
        // 14 days falls in the 3–14 bracket, NOT the free tier (spec: "more than 14 days")
        var booking = MakeBooking(Now.AddDays(14));
        var fee = CancellationFeeCalculator.CalculateCancellationFee(booking, Now);
        // May is off-peak → £120 first night → 50% = £60
        Assert.Equal(60m, fee);
    }

    // ── 3 to 14 days ──────────────────────────────────────────────────────

    [Fact]
    public void Between3And14Days_Returns50PercentOfFirstNightOffPeak()
    {
        var checkIn = new DateTime(2026, 9, 1, 0, 0, 0, DateTimeKind.Utc); // off-peak
        var refNow = checkIn.AddDays(-7);
        var booking = MakeBooking(checkIn);
        var fee = CancellationFeeCalculator.CalculateCancellationFee(booking, refNow);
        Assert.Equal(60m, fee); // 50% of £120
    }

    [Fact]
    public void Between3And14Days_Returns50PercentOfFirstNightPeak()
    {
        var checkIn = new DateTime(2026, 7, 1, 0, 0, 0, DateTimeKind.Utc); // July — peak
        var refNow = checkIn.AddDays(-7);
        var booking = MakeBooking(checkIn);
        var fee = CancellationFeeCalculator.CalculateCancellationFee(booking, refNow);
        Assert.Equal(90m, fee); // 50% of £180
    }

    // ── Less than 72 hours ────────────────────────────────────────────────

    [Fact]
    public void LessThan72Hours_Returns100PercentOfFirstNight()
    {
        var checkIn = new DateTime(2026, 9, 1, 0, 0, 0, DateTimeKind.Utc); // off-peak
        var refNow = checkIn.AddDays(-2);
        var booking = MakeBooking(checkIn);
        var fee = CancellationFeeCalculator.CalculateCancellationFee(booking, refNow);
        Assert.Equal(120m, fee);
    }

    [Fact]
    public void CheckInToday_Returns100PercentOfFirstNight()
    {
        var checkIn = new DateTime(2026, 9, 1, 0, 0, 0, DateTimeKind.Utc);
        var refNow = checkIn;
        var booking = MakeBooking(checkIn);
        var fee = CancellationFeeCalculator.CalculateCancellationFee(booking, refNow);
        Assert.Equal(120m, fee);
    }
}
