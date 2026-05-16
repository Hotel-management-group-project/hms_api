
using HMS.API.DTOs.Booking;

namespace HMS.API.Services
{
    public interface IPdfService
    {
        byte[] GenerateInvoice(BookingDto booking);
    }
}
