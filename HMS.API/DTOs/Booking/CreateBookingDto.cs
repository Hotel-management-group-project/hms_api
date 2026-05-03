// Student ID: S2401276
// Student Name: Mohamed Iyaadh Ahmed
// Module: Advanced Software Development (UFCF8S-30-2)

using System.ComponentModel.DataAnnotations;

namespace HMS.API.DTOs.Booking
{
    public class CreateBookingDto
    {
        [Required]
        public int HotelId { get; set; }

        [Required]
        [MinLength(1, ErrorMessage = "At least one room must be selected.")]
        public List<int> RoomIds { get; set; } = [];

        [Required]
        public DateTime CheckInDate { get; set; }

        [Required]
        public DateTime CheckOutDate { get; set; }

        public List<AncillaryServiceRequestDto>? AncillaryServices { get; set; }

        // Staff only: book on behalf of a specific guest by their userId.
        // If omitted, defaults to the authenticated user.
        public string? GuestId { get; set; }
    }

    public class AncillaryServiceRequestDto
    {
        [Required]
        public int ServiceId { get; set; }

        [Required]
        [Range(1, 100)]
        public int Quantity { get; set; }
    }
}
