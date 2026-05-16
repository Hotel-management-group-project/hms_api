
namespace HMS.API.DTOs.Report
{
    public class OccupancyDataPoint
    {
        public string Period { get; set; } = string.Empty;
        public double OccupancyRate { get; set; }
        public int TotalRooms { get; set; }
        public int Occupied { get; set; }
    }

    public class RevenueDataPoint
    {
        public string Period { get; set; } = string.Empty;
        public decimal Revenue { get; set; }
        public int BookingCount { get; set; }
    }

    public class DemographicItem
    {
        public string Label { get; set; } = string.Empty;
        public double Value { get; set; }
        public double Percentage { get; set; }
    }

    public class SummaryReportDto
    {
        public decimal TotalRevenue { get; set; }
        public int TotalBookings { get; set; }
        public double OccupancyRate { get; set; }
        public int ActiveGuests { get; set; }
    }
}
