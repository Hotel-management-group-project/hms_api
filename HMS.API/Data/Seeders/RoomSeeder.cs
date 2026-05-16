
using HMS.API.Models;
using Microsoft.EntityFrameworkCore;

namespace HMS.API.Data.Seeders
{
    public static class RoomSeeder
    {
        public static async Task SeedAsync(ApplicationDbContext db)
        {
            if (db.Rooms.Any()) return;

            var hotels = await db.Hotels.OrderBy(h => h.Id).ToListAsync();
            if (hotels.Count < 3) return;

            var rooms = new List<Room>();

            foreach (var (hotel, hotelIndex) in hotels.Select((h, i) => (h, i)))
            {
                rooms.AddRange(BuildHotelRooms(hotel.Id, hotelIndex));
            }

            db.Rooms.AddRange(rooms);
            await db.SaveChangesAsync();
        }

        private static IEnumerable<Room> BuildHotelRooms(int hotelId, int hotelIndex)
        {
            // Floor 1: 5 Standard Double (101–105)
            // Floor 2: 4 Deluxe King   (201–204)
            // Floor 3: 3 Family Suite  (301–303)
            // Floor 10: 2 Penthouse    (1001–1002)

            var rooms = new List<Room>();

            for (var i = 1; i <= 5; i++)
            {
                // Each hotel has 1 Occupied and 1 Cleaning room among standard rooms
                var status = i switch
                {
                    1 => RoomStatus.Occupied,
                    2 => RoomStatus.Cleaning,
                    _ => RoomStatus.Available
                };

                rooms.Add(new Room
                {
                    HotelId = hotelId,
                    RoomNumber = $"10{i}",
                    Type = RoomType.StandardDouble,
                    Capacity = 2,
                    PriceOffPeak = 120m,
                    PricePeak = 180m,
                    Status = status,
                    Description = $"Comfortable Standard Double room with city views, king-sized bed, and en-suite bathroom.",
                    Floor = 1,
                    CreatedAt = DateTime.UtcNow
                });
            }

            for (var i = 1; i <= 4; i++)
            {
                rooms.Add(new Room
                {
                    HotelId = hotelId,
                    RoomNumber = $"20{i}",
                    Type = RoomType.DeluxeKing,
                    Capacity = 2,
                    PriceOffPeak = 180m,
                    PricePeak = 250m,
                    Status = RoomStatus.Available,
                    Description = "Spacious Deluxe King room featuring premium furnishings, rainfall shower, and panoramic views.",
                    Floor = 2,
                    CreatedAt = DateTime.UtcNow
                });
            }

            for (var i = 1; i <= 3; i++)
            {
                rooms.Add(new Room
                {
                    HotelId = hotelId,
                    RoomNumber = $"30{i}",
                    Type = RoomType.FamilySuite,
                    Capacity = 4,
                    PriceOffPeak = 240m,
                    PricePeak = 320m,
                    Status = RoomStatus.Available,
                    Description = "Generous Family Suite with two bedrooms, a lounge area, and a large private terrace.",
                    Floor = 3,
                    CreatedAt = DateTime.UtcNow
                });
            }

            for (var i = 1; i <= 2; i++)
            {
                rooms.Add(new Room
                {
                    HotelId = hotelId,
                    RoomNumber = $"100{i}",
                    Type = RoomType.Penthouse,
                    Capacity = 4,
                    PriceOffPeak = 500m,
                    PricePeak = 750m,
                    Status = RoomStatus.Available,
                    Description = "Iconic Penthouse spanning the entire top floor with private pool, butler service, and 360° views.",
                    Floor = 10,
                    CreatedAt = DateTime.UtcNow
                });
            }

            return rooms;
        }
    }
}
