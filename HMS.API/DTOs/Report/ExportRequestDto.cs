
namespace HMS.API.DTOs.Report
{
    public class ExportRequestDto
    {
        public string Type { get; set; } = "pdf";
        public string Report { get; set; } = "occupancy";
        public string Period { get; set; } = "monthly";
        public int? HotelId { get; set; }
    }
}
