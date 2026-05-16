// Student ID: S2401276
// Student Name: Mohamed Iyaadh Ahmed
// Module: Advanced Software Development (UFCF8S-30-2)

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
