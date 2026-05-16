
using System.ComponentModel.DataAnnotations;

namespace HMS.API.DTOs.Waitlist
{
    public class WaitlistDto
    {
        public int Id { get; set; }
        public string GuestId { get; set; } = string.Empty;
        public string GuestName { get; set; } = string.Empty;
        public string GuestEmail { get; set; } = string.Empty;
        public int HotelId { get; set; }
        public string HotelName { get; set; } = string.Empty;
        public string RoomType { get; set; } = string.Empty;
        public DateTime CheckInDate { get; set; }
        public DateTime CheckOutDate { get; set; }
        public string Status { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
    }

    public class JoinWaitlistDto
    {
        [Required] public int HotelId { get; set; }
        [Required] public string RoomType { get; set; } = string.Empty;
        [Required] public DateTime CheckInDate { get; set; }
        [Required] public DateTime CheckOutDate { get; set; }
    }

    public class UpdateWaitlistStatusDto
    {
        [Required] public string Status { get; set; } = string.Empty;
    }
}
