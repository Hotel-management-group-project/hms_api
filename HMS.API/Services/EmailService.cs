// Student ID: S2401276
// Student Name: Mohamed Iyaadh Ahmed
// Module: Advanced Software Development (UFCF8S-30-2)

using Resend;

namespace HMS.API.Services
{
    public class EmailService : IEmailService
    {
        private readonly IResend _resend;
        private readonly string _fromEmail;
        private readonly ILogger<EmailService> _logger;

        public EmailService(IResend resend, IConfiguration config, ILogger<EmailService> logger)
        {
            _resend = resend;
            _fromEmail = config["ResendSettings:FromEmail"] ?? "HMS Hotel <noreply@hms-hotel.com>";
            _logger = logger;
        }

        public async Task SendBookingConfirmationAsync(
            string toEmail, string guestName, string referenceNumber,
            string hotelName, DateTime checkIn, DateTime checkOut,
            decimal total, string qrCodeBase64)
        {
            var message = new EmailMessage
            {
                From = _fromEmail,
                Subject = $"Booking Confirmed — {referenceNumber}"
            };
            message.To.Add(toEmail);
            message.HtmlBody = BuildConfirmationHtml(
                guestName, referenceNumber, hotelName, checkIn, checkOut, total, qrCodeBase64);

            try
            {
                await _resend.EmailSendAsync(message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send booking confirmation email to {Email} for {Reference}",
                    toEmail, referenceNumber);
            }
        }

        public async Task SendInvoiceAsync(
            string toEmail, string guestName, string referenceNumber,
            string hotelName, byte[] pdfBytes)
        {
            var message = new EmailMessage
            {
                From = _fromEmail,
                Subject = $"Invoice — {referenceNumber} | {hotelName}"
            };
            message.To.Add(toEmail);
            message.HtmlBody = BuildInvoiceHtml(guestName, referenceNumber, hotelName);
            message.Attachments ??= [];
            message.Attachments.Add(new EmailAttachment
            {
                Filename = $"Invoice-{referenceNumber}.pdf",
                Content = Convert.ToBase64String(pdfBytes),
                ContentType = "application/pdf"
            });

            try
            {
                await _resend.EmailSendAsync(message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send invoice email to {Email} for {Reference}",
                    toEmail, referenceNumber);
            }
        }

        // ── HTML templates ─────────────────────────────────────────────────────

        private static string BuildConfirmationHtml(
            string guestName, string referenceNumber, string hotelName,
            DateTime checkIn, DateTime checkOut, decimal total, string qrCodeBase64) => $"""
            <!DOCTYPE html>
            <html lang="en">
            <head><meta charset="UTF-8"><meta name="viewport" content="width=device-width,initial-scale=1"></head>
            <body style="font-family:Arial,sans-serif;background:#f4f4f4;margin:0;padding:0;">
              <table width="100%" cellpadding="0" cellspacing="0" style="background:#f4f4f4;padding:30px 0;">
                <tr><td align="center">
                  <table width="600" cellpadding="0" cellspacing="0" style="background:#fff;border-radius:8px;overflow:hidden;">
                    <tr><td style="background:#1a1a2e;padding:30px;text-align:center;">
                      <h1 style="color:#fff;margin:0;font-size:24px;">Booking Confirmed</h1>
                      <p style="color:#a0aec0;margin:8px 0 0;">{hotelName}</p>
                    </td></tr>
                    <tr><td style="padding:30px;">
                      <p style="color:#2d3748;font-size:16px;">Dear {guestName},</p>
                      <p style="color:#4a5568;">Your booking has been confirmed. We look forward to welcoming you.</p>
                      <table width="100%" cellpadding="12" cellspacing="0" style="background:#f7fafc;border-radius:6px;margin:20px 0;">
                        <tr><td style="color:#718096;font-size:14px;">Reference</td>
                            <td style="color:#1a202c;font-weight:bold;text-align:right;">{referenceNumber}</td></tr>
                        <tr><td style="color:#718096;font-size:14px;">Check-in</td>
                            <td style="color:#1a202c;text-align:right;">{checkIn:dddd, dd MMMM yyyy}</td></tr>
                        <tr><td style="color:#718096;font-size:14px;">Check-out</td>
                            <td style="color:#1a202c;text-align:right;">{checkOut:dddd, dd MMMM yyyy}</td></tr>
                        <tr><td style="color:#718096;font-size:14px;">Total Paid</td>
                            <td style="color:#1a202c;font-weight:bold;font-size:18px;text-align:right;">£{total:F2}</td></tr>
                      </table>
                      <p style="color:#4a5568;text-align:center;margin-top:24px;">Present this QR code at check-in:</p>
                      <div style="text-align:center;margin:16px 0;">
                        <img src="{qrCodeBase64}" alt="Check-in QR Code" style="width:200px;height:200px;border:1px solid #e2e8f0;border-radius:4px;" />
                      </div>
                      <p style="color:#718096;font-size:13px;text-align:center;">
                        If you need to make changes, please contact us at least 72 hours before your arrival.
                      </p>
                    </td></tr>
                    <tr><td style="background:#1a1a2e;padding:20px;text-align:center;">
                      <p style="color:#a0aec0;font-size:12px;margin:0;">© {DateTime.UtcNow.Year} HMS Hotel Management System</p>
                    </td></tr>
                  </table>
                </td></tr>
              </table>
            </body>
            </html>
            """;

        private static string BuildInvoiceHtml(string guestName, string referenceNumber, string hotelName) => $"""
            <!DOCTYPE html>
            <html lang="en">
            <head><meta charset="UTF-8"><meta name="viewport" content="width=device-width,initial-scale=1"></head>
            <body style="font-family:Arial,sans-serif;background:#f4f4f4;margin:0;padding:0;">
              <table width="100%" cellpadding="0" cellspacing="0" style="background:#f4f4f4;padding:30px 0;">
                <tr><td align="center">
                  <table width="600" cellpadding="0" cellspacing="0" style="background:#fff;border-radius:8px;overflow:hidden;">
                    <tr><td style="background:#1a1a2e;padding:30px;text-align:center;">
                      <h1 style="color:#fff;margin:0;font-size:24px;">Thank You for Your Stay</h1>
                      <p style="color:#a0aec0;margin:8px 0 0;">{hotelName}</p>
                    </td></tr>
                    <tr><td style="padding:30px;">
                      <p style="color:#2d3748;font-size:16px;">Dear {guestName},</p>
                      <p style="color:#4a5568;">
                        Thank you for staying with us. Your invoice for booking <strong>{referenceNumber}</strong>
                        is attached to this email as a PDF.
                      </p>
                      <p style="color:#4a5568;">We hope you enjoyed your stay and look forward to seeing you again.</p>
                    </td></tr>
                    <tr><td style="background:#1a1a2e;padding:20px;text-align:center;">
                      <p style="color:#a0aec0;font-size:12px;margin:0;">© {DateTime.UtcNow.Year} HMS Hotel Management System</p>
                    </td></tr>
                  </table>
                </td></tr>
              </table>
            </body>
            </html>
            """;
    }
}
