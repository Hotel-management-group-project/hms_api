// Student ID: S2401276
// Student Name: Mohamed Iyaadh Ahmed
// Module: Advanced Software Development (UFCF8S-30-2)

using System.ComponentModel.DataAnnotations;

namespace HMS.API.DTOs.Booking
{
    // Staff-only: confirms a Pending booking.
    // CheckedIn/CheckedOut transitions are handled by CheckInController/CheckOutController.
    // Cancellation (with fee logic) is handled by DELETE /api/bookings/{id}.
    public class UpdateBookingDto
    {
        [Required]
        public string Status { get; set; } = string.Empty;
    }
}
