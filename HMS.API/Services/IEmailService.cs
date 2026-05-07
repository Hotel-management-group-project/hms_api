// Student ID: S2401276
// Student Name: Mohamed Iyaadh Ahmed
// Module: Advanced Software Development (UFCF8S-30-2)

namespace HMS.API.Services
{
    public interface IEmailService
    {
        Task SendBookingConfirmationAsync(
            string toEmail, string guestName, string referenceNumber,
            string hotelName, DateTime checkIn, DateTime checkOut,
            decimal total, string qrCodeBase64);

        Task SendInvoiceAsync(
            string toEmail, string guestName, string referenceNumber,
            string hotelName, byte[] pdfBytes);

        Task SendCancellationEmailAsync(
            string toEmail, string guestName, string referenceNumber,
            string hotelName, DateTime checkIn, DateTime checkOut,
            decimal cancellationFee);

        Task SendCheckInConfirmationEmailAsync(
            string toEmail, string guestName, string referenceNumber,
            string hotelName, DateTime checkOut);

        Task SendPasswordChangeReminderEmailAsync(
            string toEmail, string fullName);
    }
}
