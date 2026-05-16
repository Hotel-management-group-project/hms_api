
using HMS.API.DTOs.Booking;
using iText.IO.Font.Constants;
using iText.Kernel.Colors;
using iText.Kernel.Font;
using iText.Kernel.Pdf;
using iText.Layout;
using iText.Layout.Element;
using iText.Layout.Properties;

namespace HMS.API.Services
{
    public class PdfService : IPdfService
    {
        public byte[] GenerateInvoice(BookingDto booking)
        {
            var ms = new MemoryStream();
            var writer = new PdfWriter(ms);
            var pdfDoc = new PdfDocument(writer);
            var document = new Document(pdfDoc);

            var regular = PdfFontFactory.CreateFont(StandardFonts.HELVETICA);
            var bold = PdfFontFactory.CreateFont(StandardFonts.HELVETICA_BOLD);
            var dark = new DeviceRgb(26, 26, 46);
            var muted = new DeviceRgb(113, 128, 150);
            var light = new DeviceRgb(247, 250, 252);

            // ── Header ─────────────────────────────────────────────────────────
            document.Add(new Paragraph("INVOICE")
                .SetFont(bold).SetFontSize(28)
                .SetFontColor(dark)
                .SetTextAlignment(TextAlignment.CENTER)
                .SetMarginBottom(4));

            document.Add(new Paragraph(booking.HotelName)
                .SetFont(regular).SetFontSize(14)
                .SetFontColor(muted)
                .SetTextAlignment(TextAlignment.CENTER)
                .SetMarginBottom(20));

            // ── Booking summary ────────────────────────────────────────────────
            var summaryTable = new Table(UnitValue.CreatePercentArray(new float[] { 50f, 50f }))
                .UseAllAvailableWidth()
                .SetMarginBottom(20);

            AddSummaryRow(summaryTable, "Reference Number", booking.ReferenceNumber, bold, regular);
            AddSummaryRow(summaryTable, "Guest Name", booking.GuestName, bold, regular);
            AddSummaryRow(summaryTable, "Guest Email", booking.GuestEmail, bold, regular);
            AddSummaryRow(summaryTable, "Check-in", booking.CheckInDate.ToString("dd MMMM yyyy"), bold, regular);
            AddSummaryRow(summaryTable, "Check-out", booking.CheckOutDate.ToString("dd MMMM yyyy"), bold, regular);
            AddSummaryRow(summaryTable, "Nights", booking.Nights.ToString(), bold, regular);

            document.Add(summaryTable);

            // ── Room charges ───────────────────────────────────────────────────
            document.Add(new Paragraph("Room Charges")
                .SetFont(bold).SetFontSize(13)
                .SetFontColor(dark)
                .SetMarginBottom(6));

            var roomTable = new Table(UnitValue.CreatePercentArray(new float[] { 15f, 35f, 20f, 10f, 20f }))
                .UseAllAvailableWidth()
                .SetMarginBottom(16);

            AddTableHeader(roomTable, bold, dark, light, "Room", "Type", "Per Night", "Nights", "Subtotal");

            var roomsTotal = 0m;
            foreach (var room in booking.Rooms)
            {
                var subtotal = room.PricePerNight * booking.Nights;
                roomsTotal += subtotal;
                roomTable.AddCell(MakeCell(room.RoomNumber, regular));
                roomTable.AddCell(MakeCell(FormatRoomType(room.RoomType), regular));
                roomTable.AddCell(MakeCell($"£{room.PricePerNight:F2}", regular, TextAlignment.RIGHT));
                roomTable.AddCell(MakeCell(booking.Nights.ToString(), regular, TextAlignment.CENTER));
                roomTable.AddCell(MakeCell($"£{subtotal:F2}", regular, TextAlignment.RIGHT));
            }

            document.Add(roomTable);

            // ── Ancillary services (only if any) ───────────────────────────────
            if (booking.AncillaryServices.Count > 0)
            {
                document.Add(new Paragraph("Additional Services")
                    .SetFont(bold).SetFontSize(13)
                    .SetFontColor(dark)
                    .SetMarginBottom(6));

                var serviceTable = new Table(UnitValue.CreatePercentArray(new float[] { 40f, 15f, 25f, 20f }))
                    .UseAllAvailableWidth()
                    .SetMarginBottom(16);

                AddTableHeader(serviceTable, bold, dark, light, "Service", "Qty", "Unit Price", "Total");

                foreach (var svc in booking.AncillaryServices)
                {
                    serviceTable.AddCell(MakeCell(svc.ServiceName, regular));
                    serviceTable.AddCell(MakeCell(svc.Quantity.ToString(), regular, TextAlignment.CENTER));
                    serviceTable.AddCell(MakeCell($"£{svc.UnitPrice:F2}", regular, TextAlignment.RIGHT));
                    serviceTable.AddCell(MakeCell($"£{svc.TotalPrice:F2}", regular, TextAlignment.RIGHT));
                }

                document.Add(serviceTable);
            }

            // ── Grand total ────────────────────────────────────────────────────
            var totalTable = new Table(UnitValue.CreatePercentArray(new float[] { 75f, 25f }))
                .UseAllAvailableWidth()
                .SetMarginTop(8);

            totalTable.AddCell(new Cell()
                .Add(new Paragraph("TOTAL").SetFont(bold).SetFontSize(14).SetFontColor(dark))
                .SetBorder(iText.Layout.Borders.Border.NO_BORDER)
                .SetTextAlignment(TextAlignment.RIGHT)
                .SetPadding(8));

            totalTable.AddCell(new Cell()
                .Add(new Paragraph($"£{booking.TotalPrice:F2}").SetFont(bold).SetFontSize(14).SetFontColor(dark))
                .SetBackgroundColor(light)
                .SetBorder(iText.Layout.Borders.Border.NO_BORDER)
                .SetTextAlignment(TextAlignment.RIGHT)
                .SetPadding(8));

            document.Add(totalTable);

            // ── Footer ─────────────────────────────────────────────────────────
            document.Add(new Paragraph($"\nGenerated on {DateTime.UtcNow:dd MMMM yyyy HH:mm} UTC")
                .SetFont(regular).SetFontSize(9)
                .SetFontColor(muted)
                .SetTextAlignment(TextAlignment.CENTER)
                .SetMarginTop(24));

            document.Close();
            return ms.ToArray();
        }

        // ── Helpers ────────────────────────────────────────────────────────────

        private static void AddSummaryRow(Table table, string label, string value,
            PdfFont bold, PdfFont regular)
        {
            table.AddCell(new Cell()
                .Add(new Paragraph(label).SetFont(bold).SetFontSize(10))
                .SetBorder(iText.Layout.Borders.Border.NO_BORDER)
                .SetPaddingBottom(4));
            table.AddCell(new Cell()
                .Add(new Paragraph(value).SetFont(regular).SetFontSize(10))
                .SetBorder(iText.Layout.Borders.Border.NO_BORDER)
                .SetPaddingBottom(4));
        }

        private static void AddTableHeader(Table table, PdfFont bold,
            DeviceRgb dark, DeviceRgb light, params string[] headers)
        {
            foreach (var h in headers)
            {
                table.AddHeaderCell(new Cell()
                    .Add(new Paragraph(h).SetFont(bold).SetFontSize(10).SetFontColor(dark))
                    .SetBackgroundColor(light)
                    .SetPadding(8));
            }
        }

        private static Cell MakeCell(string text, PdfFont font,
            TextAlignment alignment = TextAlignment.LEFT) =>
            new Cell().Add(new Paragraph(text).SetFont(font).SetFontSize(10))
                .SetPadding(7)
                .SetTextAlignment(alignment);

        private static string FormatRoomType(string type) => type switch
        {
            "StandardDouble" => "Standard Double",
            "DeluxeKing" => "Deluxe King",
            "FamilySuite" => "Family Suite",
            "Penthouse" => "Penthouse",
            _ => type
        };
    }
}
