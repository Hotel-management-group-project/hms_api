
namespace HMS.API.Services
{
    public interface IEmailService
    {
        Task SendBookingConfirmationAsync(
            string toEmail, string guestName, string referenceNumber,
            string hotelName, string roomTypes,
            DateTime checkIn, DateTime checkOut,
            decimal total, string qrCodeBase64);

        Task SendInvoiceAsync(
            string toEmail, string guestName, string referenceNumber,
            string hotelName, DateTime checkIn, DateTime checkOut,
            IEnumerable<string> lineItems, decimal total,
            byte[] pdfBytes);

        Task SendCancellationEmailAsync(
            string toEmail, string guestName, string referenceNumber,
            string hotelName, DateTime checkIn, DateTime checkOut,
            decimal cancellationFee, decimal originalTotal);

        Task SendCheckInConfirmationEmailAsync(
            string toEmail, string guestName, string referenceNumber,
            string hotelName, DateTime checkOut,
            IEnumerable<string> roomNumbers,
            IEnumerable<string> ancillaryLines);

        Task SendPasswordChangeReminderEmailAsync(
            string toEmail, string fullName);
    }
}
