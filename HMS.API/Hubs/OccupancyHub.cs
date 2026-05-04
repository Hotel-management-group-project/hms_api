// Student ID: S2401276
// Student Name: Mohamed Iyaadh Ahmed
// Module: Advanced Software Development (UFCF8S-30-2)

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace HMS.API.Hubs
{
    public class OccupancyUpdateDto
    {
        public int HotelId { get; set; }
        public string HotelName { get; set; } = string.Empty;
        public int TotalRooms { get; set; }
        public int OccupiedRooms { get; set; }
        public int AvailableRooms { get; set; }
        public int CleaningRooms { get; set; }
        public int OutOfServiceRooms { get; set; }
        public double OccupancyRate { get; set; }
        public DateTime UpdatedAt { get; set; }
    }

    [Authorize(Roles = "FrontDesk,Manager,Admin")]
    public class OccupancyHub : Hub
    {
        private const string StaffGroup = "StaffDashboard";

        public override async Task OnConnectedAsync()
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, StaffGroup);
            await base.OnConnectedAsync();
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, StaffGroup);
            await base.OnDisconnectedAsync(exception);
        }

        // Clients call this to request a specific hotel's current occupancy on connect
        public async Task RequestOccupancyUpdate(int hotelId)
        {
            await Clients.Caller.SendAsync("OccupancyRequested", hotelId);
        }
    }
}
