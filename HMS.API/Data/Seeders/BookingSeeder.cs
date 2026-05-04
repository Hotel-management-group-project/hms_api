// Student ID: S2401276
// Student Name: Mohamed Iyaadh Ahmed
// Module: Advanced Software Development (UFCF8S-30-2)

using HMS.API.Models;
using Microsoft.EntityFrameworkCore;

namespace HMS.API.Data.Seeders
{
    public static class BookingSeeder
    {
        public static async Task SeedAsync(ApplicationDbContext db)
        {
            if (db.Bookings.Any()) return;

            var today = DateTime.UtcNow.Date;

            // ── Look up seed data dependencies ────────────────────────────────

            var hotels = await db.Hotels.OrderBy(h => h.Id).ToListAsync();
            if (hotels.Count < 3) return;

            var guest = await db.Users.FirstOrDefaultAsync(u => u.Email == "guest@hms.com");
            if (guest is null) return;

            // Rooms grouped by hotel and type, ordered by room number
            var allRooms = await db.Rooms.OrderBy(r => r.RoomNumber).ToListAsync();

            Room RoomOf(int hotelId, RoomType type, int skip = 0) =>
                allRooms.Where(r => r.HotelId == hotelId && r.Type == type)
                        .Skip(skip).First();

            var services = await db.AncillaryServices.ToListAsync();
            var breakfast = services.First(s => s.Name == "Full English Breakfast");
            var spa = services.First(s => s.Name == "Spa Access");
            var transfer = services.First(s => s.Name == "Airport Transfer");
            var lateCheckout = services.First(s => s.Name == "Late Check-out");

            // ── Build bookings ─────────────────────────────────────────────────

            var bookings = new List<(Booking Booking, Room[] Rooms, (AncillaryService Svc, int Qty)[] Services)>();

            // 1. Hotel 1 — Standard — CheckedIn (today, 4 nights)
            var b1Room = RoomOf(hotels[0].Id, RoomType.StandardDouble);
            bookings.Add((
                MakeBooking("HMS-2024-00001", guest.Id, hotels[0].Id,
                    today.AddDays(-1), today.AddDays(3), BookingStatus.CheckedIn),
                [b1Room],
                []
            ));

            // 2. Hotel 2 — Deluxe — CheckedIn (today-2, 4 nights) + Breakfast×2
            var b2Room = RoomOf(hotels[1].Id, RoomType.DeluxeKing);
            bookings.Add((
                MakeBooking("HMS-2024-00002", guest.Id, hotels[1].Id,
                    today.AddDays(-2), today.AddDays(2), BookingStatus.CheckedIn),
                [b2Room],
                [(breakfast, 2)]
            ));

            // 3. Hotel 1 — Deluxe — Confirmed (next week, 3 nights)
            bookings.Add((
                MakeBooking("HMS-2024-00003", guest.Id, hotels[0].Id,
                    today.AddDays(7), today.AddDays(10), BookingStatus.Confirmed),
                [RoomOf(hotels[0].Id, RoomType.DeluxeKing)],
                []
            ));

            // 4. Hotel 2 — Family Suite — Confirmed (2 weeks, 7 nights) + Spa×2 + Breakfast×2
            bookings.Add((
                MakeBooking("HMS-2024-00004", guest.Id, hotels[1].Id,
                    today.AddDays(14), today.AddDays(21), BookingStatus.Confirmed),
                [RoomOf(hotels[1].Id, RoomType.FamilySuite)],
                [(spa, 2), (breakfast, 2)]
            ));

            // 5. Hotel 3 — Standard — Confirmed (next month, 3 nights)
            bookings.Add((
                MakeBooking("HMS-2024-00005", guest.Id, hotels[2].Id,
                    today.AddDays(30), today.AddDays(33), BookingStatus.Confirmed),
                [RoomOf(hotels[2].Id, RoomType.StandardDouble)],
                []
            ));

            // 6. Hotel 1 — Family Suite — CheckedOut (last month, 3 nights)
            bookings.Add((
                MakeBooking("HMS-2024-00006", guest.Id, hotels[0].Id,
                    today.AddDays(-30), today.AddDays(-27), BookingStatus.CheckedOut),
                [RoomOf(hotels[0].Id, RoomType.FamilySuite)],
                []
            ));

            // 7. Hotel 2 — Deluxe — CheckedOut (2 months ago, 4 nights) + Airport Transfer
            bookings.Add((
                MakeBooking("HMS-2024-00007", guest.Id, hotels[1].Id,
                    today.AddDays(-60), today.AddDays(-56), BookingStatus.CheckedOut),
                [RoomOf(hotels[1].Id, RoomType.DeluxeKing, skip: 1)],
                [(transfer, 1)]
            ));

            // 8. Hotel 3 — Penthouse — CheckedOut (2 weeks ago, 3 nights) + Late Check-out
            bookings.Add((
                MakeBooking("HMS-2024-00008", guest.Id, hotels[2].Id,
                    today.AddDays(-14), today.AddDays(-11), BookingStatus.CheckedOut),
                [RoomOf(hotels[2].Id, RoomType.Penthouse)],
                [(lateCheckout, 1)]
            ));

            // 9. Hotel 1 — Standard — Cancelled (within 3 days, 3 nights — 100% first night fee)
            var b9CheckIn = today.AddDays(2);
            var b9Room = RoomOf(hotels[0].Id, RoomType.StandardDouble, skip: 2);
            var b9Total = NightlyRate(b9Room, b9CheckIn) * 3;
            var b9 = MakeBooking("HMS-2024-00009", guest.Id, hotels[0].Id,
                b9CheckIn, today.AddDays(5), BookingStatus.Cancelled,
                cancellationFee: NightlyRate(b9Room, b9CheckIn));
            bookings.Add((b9, [b9Room], []));

            // 10. Hotel 3 — Family Suite — Confirmed (2 months out, 7 nights) + Spa×4
            bookings.Add((
                MakeBooking("HMS-2024-00010", guest.Id, hotels[2].Id,
                    today.AddDays(60), today.AddDays(67), BookingStatus.Confirmed),
                [RoomOf(hotels[2].Id, RoomType.FamilySuite)],
                [(spa, 4)]
            ));

            // ── Persist ────────────────────────────────────────────────────────

            foreach (var (booking, rooms, services2) in bookings)
            {
                // Calculate total price: room charges + ancillary
                var roomTotal = rooms.Sum(r => NightlyRate(r, booking.CheckInDate)
                    * (booking.CheckOutDate - booking.CheckInDate).Days);
                var svcTotal = services2.Sum(s => s.Svc.Price * s.Qty);
                booking.TotalPrice = roomTotal + svcTotal;

                db.Bookings.Add(booking);
                await db.SaveChangesAsync();

                // BookingRooms
                foreach (var room in rooms)
                    db.BookingRooms.Add(new BookingRoom { BookingId = booking.Id, RoomId = room.Id });

                // BookingAncillaryServices
                foreach (var (svc, qty) in services2)
                    db.BookingAncillaryServices.Add(new BookingAncillaryService
                    {
                        BookingId = booking.Id,
                        AncillaryServiceId = svc.Id,
                        Quantity = qty,
                        TotalPrice = svc.Price * qty
                    });

                // Payment for all non-Pending, non-Cancelled statuses
                if (booking.Status is BookingStatus.Confirmed or BookingStatus.CheckedIn or BookingStatus.CheckedOut)
                {
                    db.Payments.Add(new Payment
                    {
                        BookingId = booking.Id,
                        Amount = booking.TotalPrice,
                        Method = "Mock",
                        Status = PaymentStatus.Completed,
                        TransactionReference = $"HMS-PAY-SEED{booking.Id:D2}",
                        ProcessedAt = booking.CreatedAt.AddMinutes(5)
                    });
                }

                // Set rooms to Occupied for active check-ins
                if (booking.Status == BookingStatus.CheckedIn)
                {
                    foreach (var room in rooms)
                        room.Status = RoomStatus.Occupied;
                }

                await db.SaveChangesAsync();
            }
        }

        // ── Helpers ────────────────────────────────────────────────────────────

        private static Booking MakeBooking(
            string reference, string guestId, int hotelId,
            DateTime checkIn, DateTime checkOut,
            BookingStatus status, decimal cancellationFee = 0)
        {
            var created = status switch
            {
                BookingStatus.CheckedOut => checkOut.AddDays(-1),
                BookingStatus.CheckedIn  => checkIn,
                BookingStatus.Cancelled  => checkIn.AddDays(-3),
                _                        => DateTime.UtcNow.AddMinutes(-10)
            };

            return new Booking
            {
                ReferenceNumber = reference,
                GuestId = guestId,
                HotelId = hotelId,
                CheckInDate = checkIn,
                CheckOutDate = checkOut,
                Status = status,
                TotalPrice = 0,   // set after room calculation
                CancellationFee = cancellationFee,
                CreatedAt = created,
                UpdatedAt = created
            };
        }

        private static bool IsPeakMonth(int month) => month is 6 or 7 or 8 or 12;

        private static decimal NightlyRate(Room room, DateTime checkIn) =>
            IsPeakMonth(checkIn.Month) ? room.PricePeak : room.PriceOffPeak;
    }
}
