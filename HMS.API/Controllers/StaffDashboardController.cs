
using HMS.API.Data;
using HMS.API.DTOs.Report;
using HMS.API.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HMS.API.Controllers
{
    [ApiController]
    [Route("api/reports")]
    [Authorize(Roles = "FrontDesk,Manager,Admin")]
    public class StaffDashboardController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public StaffDashboardController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpGet("staff-dashboard")]
        public async Task<IActionResult> StaffDashboard()
        {
            var today = DateTime.UtcNow.Date;
            var tomorrow = today.AddDays(1);

            var todayArrivals = await _context.Bookings
                .CountAsync(b => b.CheckInDate >= today && b.CheckInDate < tomorrow
                                 && b.Status == BookingStatus.Confirmed);

            var todayDepartures = await _context.Bookings
                .CountAsync(b => b.CheckOutDate >= today && b.CheckOutDate < tomorrow
                                 && b.Status == BookingStatus.CheckedIn);

            var currentOccupancy = await _context.Rooms
                .CountAsync(r => r.Status == RoomStatus.Occupied);

            var totalRooms = await _context.Rooms.CountAsync();

            var occupancyRate = totalRooms > 0
                ? Math.Round((double)currentOccupancy / totalRooms * 100, 1)
                : 0.0;

            var pendingRequests = await _context.BookingAncillaryServices
                .CountAsync(bas => bas.Booking.Status == BookingStatus.CheckedIn);

            return Ok(new StaffDashboardDto
            {
                TodayArrivals = todayArrivals,
                TodayDepartures = todayDepartures,
                CurrentOccupancy = currentOccupancy,
                TotalRooms = totalRooms,
                OccupancyRate = occupancyRate,
                PendingRequests = pendingRequests
            });
        }
    }
}
