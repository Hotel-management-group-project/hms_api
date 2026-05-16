
namespace HMS.API.DTOs.Report
{
    public class StaffDashboardDto
    {
        public int TodayArrivals { get; set; }
        public int TodayDepartures { get; set; }
        public int CurrentOccupancy { get; set; }
        public int TotalRooms { get; set; }
        public double OccupancyRate { get; set; }
        public int PendingRequests { get; set; }
    }
}
