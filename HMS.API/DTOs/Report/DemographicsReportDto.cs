// Student ID: S2401276
// Student Name: Mohamed Iyaadh Ahmed
// Module: Advanced Software Development (UFCF8S-30-2)

namespace HMS.API.DTOs.Report
{
    public class DemographicsReportDto
    {
        public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;
        public int TotalGuests { get; set; }
        public int RepeatGuests { get; set; }
        public int NewGuests { get; set; }
        public double RepeatGuestRate { get; set; }
        public double AverageStayDuration { get; set; }
        public List<BookingsByLocationDto> ByLocation { get; set; } = [];
        public List<TopGuestDto> TopGuests { get; set; } = [];
    }

    public class BookingsByLocationDto
    {
        public string Location { get; set; } = string.Empty;
        public int Bookings { get; set; }
        public decimal Revenue { get; set; }
    }

    public class TopGuestDto
    {
        public string GuestName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public int TotalBookings { get; set; }
        public decimal TotalSpent { get; set; }
    }
}
