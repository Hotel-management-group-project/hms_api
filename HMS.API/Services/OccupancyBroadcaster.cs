
using HMS.API.Data;
using HMS.API.Hubs;
using HMS.API.Models;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace HMS.API.Services
{
    public class OccupancyBroadcaster : IOccupancyBroadcaster
    {
        private readonly IHubContext<OccupancyHub> _hub;
        private readonly ApplicationDbContext _db;

        public OccupancyBroadcaster(IHubContext<OccupancyHub> hub, ApplicationDbContext db)
        {
            _hub = hub;
            _db = db;
        }

        public async Task BroadcastAsync(int hotelId)
        {
            var hotel = await _db.Hotels.FindAsync(hotelId);
            if (hotel is null) return;

            var rooms = await _db.Rooms
                .Where(r => r.HotelId == hotelId)
                .ToListAsync();

            var total = rooms.Count;
            var occupied = rooms.Count(r => r.Status == RoomStatus.Occupied);
            var available = rooms.Count(r => r.Status == RoomStatus.Available);
            var cleaning = rooms.Count(r => r.Status == RoomStatus.Cleaning);
            var outOfService = rooms.Count(r => r.Status == RoomStatus.OutOfService);

            var update = new OccupancyUpdateDto
            {
                HotelId = hotelId,
                HotelName = hotel.Name,
                TotalRooms = total,
                OccupiedRooms = occupied,
                AvailableRooms = available,
                CleaningRooms = cleaning,
                OutOfServiceRooms = outOfService,
                OccupancyRate = total > 0 ? Math.Round((double)occupied / total * 100, 1) : 0,
                UpdatedAt = DateTime.UtcNow
            };

            await _hub.Clients.Group("StaffDashboard")
                .SendAsync("ReceiveOccupancyUpdate", update);
        }
    }
}
