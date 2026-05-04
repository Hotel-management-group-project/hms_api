// Student ID: S2401276
// Student Name: Mohamed Iyaadh Ahmed
// Module: Advanced Software Development (UFCF8S-30-2)

namespace HMS.API.DTOs.Report
{
    public class RevenueReportDto
    {
        public string Period { get; set; } = string.Empty;
        public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;
        public decimal TotalRevenue { get; set; }
        public decimal RoomRevenue { get; set; }
        public decimal AncillaryRevenue { get; set; }
        public List<RevenueByRoomTypeDto> ByRoomType { get; set; } = [];
        public List<RevenueByServiceDto> ByAncillaryService { get; set; } = [];
        public List<RevenuePeriodDto> Periods { get; set; } = [];
    }

    public class RevenueByRoomTypeDto
    {
        public string RoomType { get; set; } = string.Empty;
        public int BookingCount { get; set; }
        public decimal Revenue { get; set; }
    }

    public class RevenueByServiceDto
    {
        public string ServiceName { get; set; } = string.Empty;
        public int QuantitySold { get; set; }
        public decimal Revenue { get; set; }
    }

    public class RevenuePeriodDto
    {
        public string Label { get; set; } = string.Empty;
        public decimal Revenue { get; set; }
        public int Bookings { get; set; }
    }
}
