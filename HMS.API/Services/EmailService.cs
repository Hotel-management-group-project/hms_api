
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
            string hotelName, string roomTypes,
            DateTime checkIn, DateTime checkOut,
            decimal total, string qrCodeBase64)
        {
            var message = new EmailMessage
            {
                From = _fromEmail,
                Subject = $"Your booking is confirmed — {hotelName}"
            };
            message.To.Add(toEmail);
            message.HtmlBody = BuildConfirmationHtml(
                guestName, referenceNumber, hotelName, roomTypes, checkIn, checkOut, total, qrCodeBase64);

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
            string hotelName, DateTime checkIn, DateTime checkOut,
            IEnumerable<string> lineItems, decimal total,
            byte[] pdfBytes)
        {
            var message = new EmailMessage
            {
                From = _fromEmail,
                Subject = $"Thank you for staying — {hotelName}"
            };
            message.To.Add(toEmail);
            message.HtmlBody = BuildInvoiceHtml(guestName, referenceNumber, hotelName, checkIn, checkOut, lineItems, total);
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

        public async Task SendCancellationEmailAsync(
            string toEmail, string guestName, string referenceNumber,
            string hotelName, DateTime checkIn, DateTime checkOut,
            decimal cancellationFee, decimal originalTotal)
        {
            var message = new EmailMessage
            {
                From = _fromEmail,
                Subject = $"Booking cancellation confirmed — {hotelName}"
            };
            message.To.Add(toEmail);
            message.HtmlBody = BuildCancellationHtml(
                guestName, referenceNumber, hotelName, checkIn, checkOut, cancellationFee, originalTotal);

            try
            {
                await _resend.EmailSendAsync(message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send cancellation email to {Email} for {Reference}",
                    toEmail, referenceNumber);
            }
        }

        public async Task SendCheckInConfirmationEmailAsync(
            string toEmail, string guestName, string referenceNumber,
            string hotelName, DateTime checkOut,
            IEnumerable<string> roomNumbers,
            IEnumerable<string> ancillaryLines)
        {
            var message = new EmailMessage
            {
                From = _fromEmail,
                Subject = $"Welcome! You're checked in — {hotelName}"
            };
            message.To.Add(toEmail);
            message.HtmlBody = BuildCheckInHtml(guestName, referenceNumber, hotelName, checkOut, roomNumbers, ancillaryLines);

            try
            {
                await _resend.EmailSendAsync(message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send check-in confirmation email to {Email} for {Reference}",
                    toEmail, referenceNumber);
            }
        }

        public async Task SendPasswordChangeReminderEmailAsync(string toEmail, string fullName)
        {
            var message = new EmailMessage
            {
                From = _fromEmail,
                Subject = "Action Required: Password Change Due"
            };
            message.To.Add(toEmail);
            message.HtmlBody = BuildPasswordChangeHtml(fullName);

            try
            {
                await _resend.EmailSendAsync(message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send password change reminder email to {Email}", toEmail);
            }
        }

        // ── HTML templates ─────────────────────────────────────────────────────

        private static string BuildConfirmationHtml(
            string guestName, string referenceNumber, string hotelName, string roomTypes,
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
                        <tr><td style="color:#718096;font-size:14px;">Hotel</td>
                            <td style="color:#1a202c;text-align:right;">{hotelName}</td></tr>
                        <tr><td style="color:#718096;font-size:14px;">Room Type</td>
                            <td style="color:#1a202c;text-align:right;">{roomTypes}</td></tr>
                        <tr><td style="color:#718096;font-size:14px;">Check-in</td>
                            <td style="color:#1a202c;text-align:right;">{checkIn:dddd, dd MMMM yyyy}</td></tr>
                        <tr><td style="color:#718096;font-size:14px;">Check-out</td>
                            <td style="color:#1a202c;text-align:right;">{checkOut:dddd, dd MMMM yyyy}</td></tr>
                        <tr><td style="color:#718096;font-size:14px;">Total Amount</td>
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

        private static string BuildCancellationHtml(
            string guestName, string referenceNumber, string hotelName,
            DateTime checkIn, DateTime checkOut, decimal cancellationFee, decimal originalTotal)
        {
            var refundAmount = originalTotal - cancellationFee;
            var refundNote = cancellationFee == 0
                ? "<p style=\"color:#38a169;\">No cancellation fee applies — full refund of <strong>£" + refundAmount.ToString("F2") + "</strong> will be processed.</p>"
                : cancellationFee >= originalTotal
                    ? "<p style=\"color:#718096;font-size:13px;\">No refund is due — the cancellation fee covers the full booking amount.</p>"
                    : "<p style=\"color:#4a5568;\">Refund of <strong>£" + refundAmount.ToString("F2") + "</strong> will be processed after deducting the cancellation fee.</p>";

            return $"""
            <!DOCTYPE html>
            <html lang="en">
            <head><meta charset="UTF-8"><meta name="viewport" content="width=device-width,initial-scale=1"></head>
            <body style="font-family:Arial,sans-serif;background:#f4f4f4;margin:0;padding:0;">
              <table width="100%" cellpadding="0" cellspacing="0" style="background:#f4f4f4;padding:30px 0;">
                <tr><td align="center">
                  <table width="600" cellpadding="0" cellspacing="0" style="background:#fff;border-radius:8px;overflow:hidden;">
                    <tr><td style="background:#c53030;padding:30px;text-align:center;">
                      <h1 style="color:#fff;margin:0;font-size:24px;">Booking Cancellation Confirmed</h1>
                      <p style="color:#fed7d7;margin:8px 0 0;">{hotelName}</p>
                    </td></tr>
                    <tr><td style="padding:30px;">
                      <p style="color:#2d3748;font-size:16px;">Dear {guestName},</p>
                      <p style="color:#4a5568;">Your booking cancellation has been confirmed. Here are the details:</p>
                      <table width="100%" cellpadding="12" cellspacing="0" style="background:#f7fafc;border-radius:6px;margin:20px 0;">
                        <tr><td style="color:#718096;font-size:14px;">Booking Reference</td>
                            <td style="color:#1a202c;font-weight:bold;text-align:right;">{referenceNumber}</td></tr>
                        <tr><td style="color:#718096;font-size:14px;">Original Check-in</td>
                            <td style="color:#1a202c;text-align:right;">{checkIn:dddd, dd MMMM yyyy}</td></tr>
                        <tr><td style="color:#718096;font-size:14px;">Original Check-out</td>
                            <td style="color:#1a202c;text-align:right;">{checkOut:dddd, dd MMMM yyyy}</td></tr>
                        <tr><td style="color:#718096;font-size:14px;">Cancellation Fee</td>
                            <td style="color:#c53030;font-weight:bold;text-align:right;">£{cancellationFee:F2}</td></tr>
                        <tr><td style="color:#718096;font-size:14px;">Refund Amount</td>
                            <td style="color:#38a169;font-weight:bold;font-size:18px;text-align:right;">£{refundAmount:F2}</td></tr>
                      </table>
                      {refundNote}
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

        private static string BuildCheckInHtml(
            string guestName, string referenceNumber, string hotelName, DateTime checkOut,
            IEnumerable<string> roomNumbers, IEnumerable<string> ancillaryLines)
        {
            var roomRows = string.Concat(roomNumbers.Select(r =>
                $"<tr><td colspan=\"2\" style=\"color:#1a202c;font-size:14px;padding:6px 12px;\">{r}</td></tr>"));

            var ancillarySection = "";
            var lines = ancillaryLines.ToList();
            if (lines.Count > 0)
            {
                var ancillaryRows = string.Concat(lines.Select(l =>
                    $"<tr><td colspan=\"2\" style=\"color:#1a202c;font-size:14px;padding:6px 12px;\">{l}</td></tr>"));
                ancillarySection = $"""
                    <p style="color:#2d3748;font-weight:bold;margin:20px 0 8px;">Ancillary Services</p>
                    <table width="100%" cellpadding="0" cellspacing="0" style="background:#f7fafc;border-radius:6px;">
                      {ancillaryRows}
                    </table>
                    """;
            }

            return $"""
            <!DOCTYPE html>
            <html lang="en">
            <head><meta charset="UTF-8"><meta name="viewport" content="width=device-width,initial-scale=1"></head>
            <body style="font-family:Arial,sans-serif;background:#f4f4f4;margin:0;padding:0;">
              <table width="100%" cellpadding="0" cellspacing="0" style="background:#f4f4f4;padding:30px 0;">
                <tr><td align="center">
                  <table width="600" cellpadding="0" cellspacing="0" style="background:#fff;border-radius:8px;overflow:hidden;">
                    <tr><td style="background:#1a1a2e;padding:30px;text-align:center;">
                      <h1 style="color:#fff;margin:0;font-size:24px;">Welcome to {hotelName}!</h1>
                      <p style="color:#a0aec0;margin:8px 0 0;">We hope you enjoy your stay</p>
                    </td></tr>
                    <tr><td style="padding:30px;">
                      <p style="color:#2d3748;font-size:16px;">Dear {guestName},</p>
                      <p style="color:#4a5568;">You have successfully checked in. Here are your stay details:</p>
                      <table width="100%" cellpadding="12" cellspacing="0" style="background:#f7fafc;border-radius:6px;margin:20px 0;">
                        <tr><td style="color:#718096;font-size:14px;">Reference</td>
                            <td style="color:#1a202c;font-weight:bold;text-align:right;">{referenceNumber}</td></tr>
                        <tr><td style="color:#718096;font-size:14px;">Check-out Date</td>
                            <td style="color:#1a202c;text-align:right;">{checkOut:dddd, dd MMMM yyyy}</td></tr>
                      </table>
                      <p style="color:#2d3748;font-weight:bold;margin:20px 0 8px;">Your Room(s)</p>
                      <table width="100%" cellpadding="0" cellspacing="0" style="background:#f7fafc;border-radius:6px;">
                        {roomRows}
                      </table>
                      {ancillarySection}
                      <p style="color:#4a5568;margin-top:20px;">If you need any assistance during your stay, please do not hesitate to contact the front desk.</p>
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

        private static string BuildPasswordChangeHtml(string fullName) => $"""
            <!DOCTYPE html>
            <html lang="en">
            <head><meta charset="UTF-8"><meta name="viewport" content="width=device-width,initial-scale=1"></head>
            <body style="font-family:Arial,sans-serif;background:#f4f4f4;margin:0;padding:0;">
              <table width="100%" cellpadding="0" cellspacing="0" style="background:#f4f4f4;padding:30px 0;">
                <tr><td align="center">
                  <table width="600" cellpadding="0" cellspacing="0" style="background:#fff;border-radius:8px;overflow:hidden;">
                    <tr><td style="background:#d69e2e;padding:30px;text-align:center;">
                      <h1 style="color:#fff;margin:0;font-size:24px;">Password Change Required</h1>
                      <p style="color:#fefcbf;margin:8px 0 0;">Action required on your HMS account</p>
                    </td></tr>
                    <tr><td style="padding:30px;">
                      <p style="color:#2d3748;font-size:16px;">Dear {fullName},</p>
                      <p style="color:#4a5568;">
                        Your HMS account password is due for its 6-month security renewal. As an Admin or Manager,
                        you are required to update your password to maintain account security.
                      </p>
                      <p style="color:#4a5568;">
                        Please log in and change your password at your earliest convenience. You will be prompted
                        to do so on your next login.
                      </p>
                      <p style="color:#718096;font-size:13px;margin-top:24px;">
                        If you have any questions, please contact your system administrator.
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

        private static string BuildInvoiceHtml(
            string guestName, string referenceNumber, string hotelName,
            DateTime checkIn, DateTime checkOut,
            IEnumerable<string> lineItems, decimal total)
        {
            var chargeRows = string.Concat(lineItems.Select(l =>
                $"<tr><td style=\"color:#4a5568;font-size:14px;padding:8px 12px;\">{l}</td></tr>"));

            return $"""
            <!DOCTYPE html>
            <html lang="en">
            <head><meta charset="UTF-8"><meta name="viewport" content="width=device-width,initial-scale=1"></head>
            <body style="font-family:Arial,sans-serif;background:#f4f4f4;margin:0;padding:0;">
              <table width="100%" cellpadding="0" cellspacing="0" style="background:#f4f4f4;padding:30px 0;">
                <tr><td align="center">
                  <table width="600" cellpadding="0" cellspacing="0" style="background:#fff;border-radius:8px;overflow:hidden;">
                    <tr><td style="background:#1a1a2e;padding:30px;text-align:center;">
                      <h1 style="color:#fff;margin:0;font-size:24px;">Thank You for Staying</h1>
                      <p style="color:#a0aec0;margin:8px 0 0;">{hotelName}</p>
                    </td></tr>
                    <tr><td style="padding:30px;">
                      <p style="color:#2d3748;font-size:16px;">Dear {guestName},</p>
                      <p style="color:#4a5568;">Thank you for staying with us. Here is a summary of your stay:</p>
                      <table width="100%" cellpadding="12" cellspacing="0" style="background:#f7fafc;border-radius:6px;margin:20px 0;">
                        <tr><td style="color:#718096;font-size:14px;">Reference</td>
                            <td style="color:#1a202c;font-weight:bold;text-align:right;">{referenceNumber}</td></tr>
                        <tr><td style="color:#718096;font-size:14px;">Check-in</td>
                            <td style="color:#1a202c;text-align:right;">{checkIn:dddd, dd MMMM yyyy}</td></tr>
                        <tr><td style="color:#718096;font-size:14px;">Check-out</td>
                            <td style="color:#1a202c;text-align:right;">{checkOut:dddd, dd MMMM yyyy}</td></tr>
                      </table>
                      <p style="color:#2d3748;font-weight:bold;margin:20px 0 8px;">Charges</p>
                      <table width="100%" cellpadding="0" cellspacing="0" style="background:#f7fafc;border-radius:6px;">
                        {chargeRows}
                        <tr><td style="color:#1a202c;font-weight:bold;font-size:16px;padding:12px;border-top:2px solid #e2e8f0;">
                          Total Paid: £{total:F2}
                        </td></tr>
                      </table>
                      <p style="color:#4a5568;margin-top:20px;">Your full invoice is attached as a PDF. We hope to see you again soon.</p>
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
}
