// Student ID: S2401276
// Student Name: Mohamed Iyaadh Ahmed
// Module: Advanced Software Development (UFCF8S-30-2)

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
