// Student ID: S2401276
// Student Name: Mohamed Iyaadh Ahmed
// Module: Advanced Software Development (UFCF8S-30-2)

namespace HMS.API.DTOs.Report
{
    public class OccupancyReportDto
    {
        public string Period { get; set; } = string.Empty;
        public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;
        public List<HotelOccupancyDto> Hotels { get; set; } = [];
    }

    public class HotelOccupancyDto
    {
        public int HotelId { get; set; }
        public string HotelName { get; set; } = string.Empty;
        public List<OccupancyPeriodDto> Periods { get; set; } = [];
    }

    public class OccupancyPeriodDto
    {
        public string Label { get; set; } = string.Empty;
        public int TotalRooms { get; set; }
        public int OccupiedRooms { get; set; }
        public double OccupancyRate { get; set; }
    }
}
