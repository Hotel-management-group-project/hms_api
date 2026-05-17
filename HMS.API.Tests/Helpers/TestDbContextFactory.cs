using HMS.API.Data;
using HMS.API.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace HMS.API.Tests.Helpers;

public static class TestDbContextFactory
{
    public static ApplicationDbContext Create(string dbName)
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(dbName)
            .Options;

        var config = new ConfigurationBuilder().Build();
        return new ApplicationDbContext(options, config);
    }

    public static (Hotel hotel, Room room, User guest) SeedBasic(
        ApplicationDbContext db,
        string guestId = "guest-1",
        decimal priceOffPeak = 120m,
        decimal pricePeak = 180m)
    {
        var hotel = new Hotel
        {
            Id = 1,
            Name = "Test Hotel",
            Location = "Bristol",
            Address = "1 Test Street",
            Description = "Test",
            IsActive = true
        };

        var room = new Room
        {
            Id = 1,
            HotelId = 1,
            RoomNumber = "101",
            Type = RoomType.StandardDouble,
            Capacity = 2,
            PriceOffPeak = priceOffPeak,
            PricePeak = pricePeak,
            Status = RoomStatus.Available,
            Floor = 1
        };

        var guest = new User
        {
            Id = guestId,
            UserName = "guest@test.com",
            NormalizedUserName = "GUEST@TEST.COM",
            Email = "guest@test.com",
            NormalizedEmail = "GUEST@TEST.COM",
            FirstName = "Test",
            LastName = "Guest",
            Role = "Guest",
            IsActive = true
        };

        db.Hotels.Add(hotel);
        db.Rooms.Add(room);
        db.Users.Add(guest);
        db.SaveChanges();

        return (hotel, room, guest);
    }

    /// <summary>
    /// Returns the first future date whose month is off-peak (not Jun/Jul/Aug/Dec),
    /// at least <paramref name="daysOut"/> days from today.
    /// </summary>
    public static DateTime NextOffPeakDate(int daysOut = 30)
    {
        int[] peakMonths = [6, 7, 8, 12];
        var date = DateTime.UtcNow.Date.AddDays(daysOut);
        while (peakMonths.Contains(date.Month))
            date = new DateTime(date.Year, date.Month, 1).AddMonths(1);
        return date;
    }
}
