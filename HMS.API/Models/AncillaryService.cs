// Student ID: S2401276
// Student Name: Mohamed Iyaadh Ahmed
// Module: Advanced Software Development (UFCF8S-30-2)

namespace HMS.API.Models
{
    public class AncillaryService
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public string? Description { get; set; }
        public ICollection<BookingAncillaryService> BookingAncillaryServices { get; set; } = new List<BookingAncillaryService>();
    }
}