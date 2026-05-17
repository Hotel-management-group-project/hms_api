using HMS.API.Models;

namespace HMS.API.Services;

public static class CancellationFeeCalculator
{
    public static bool IsPeakMonth(int month) => month is 6 or 7 or 8 or 12;

    public static decimal NightlyRate(Room room, DateTime night) =>
        IsPeakMonth(night.Month) ? room.PricePeak : room.PriceOffPeak;

    public static decimal CalculateRoomTotal(IEnumerable<Room> rooms, DateTime checkIn, DateTime checkOut)
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

    // The `now` parameter allows tests to inject a fixed clock without relying on DateTime.UtcNow.
    public static decimal CalculateCancellationFee(Booking booking, DateTime? now = null)
    {
        if (booking.Status == BookingStatus.NoShow)
            return booking.TotalPrice;

        var today = (now ?? DateTime.UtcNow).Date;
        var daysUntilCheckIn = (booking.CheckInDate.Date - today).TotalDays;

        if (daysUntilCheckIn > 14)
            return 0m;

        var firstNightTotal = booking.BookingRooms
            .Sum(br => IsPeakMonth(booking.CheckInDate.Month) ? br.Room.PricePeak : br.Room.PriceOffPeak);

        if (daysUntilCheckIn >= 3)
            return Math.Round(firstNightTotal * 0.5m, 2);

        return firstNightTotal;
    }
}
