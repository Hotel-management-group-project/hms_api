
using ClosedXML.Excel;
using HMS.API.Data;
using HMS.API.DTOs.Report;
using HMS.API.Models;
using iText.IO.Font.Constants;
using iText.Kernel.Colors;
using iText.Kernel.Font;
using iText.Kernel.Pdf;
using iText.Layout;
using iText.Layout.Element;
using iText.Layout.Properties;
using Microsoft.EntityFrameworkCore;

namespace HMS.API.Services
{
    public class ReportService : IReportService
    {
        private readonly ApplicationDbContext _db;

        public ReportService(ApplicationDbContext db)
        {
            _db = db;
        }

        // ── Occupancy ──────────────────────────────────────────────────────────

        public async Task<OccupancyReportDto> GetOccupancyAsync(string period, int? hotelId = null)
        {
            var query = _db.Hotels.Where(h => h.IsActive);
            if (hotelId.HasValue)
                query = query.Where(h => h.Id == hotelId.Value);

            var hotels = await query.Include(h => h.Rooms).ToListAsync();

            var now = DateTime.UtcNow;
            var result = new OccupancyReportDto { Period = period };

            foreach (var hotel in hotels)
            {
                var hotelDto = new HotelOccupancyDto
                {
                    HotelId = hotel.Id,
                    HotelName = hotel.Name
                };

                var periods = BuildDatePeriods(period, now);

                foreach (var (label, start, end) in periods)
                {
                    var totalRooms = hotel.Rooms.Count;
                    if (totalRooms == 0)
                    {
                        hotelDto.Periods.Add(new OccupancyPeriodDto { Label = label, TotalRooms = 0 });
                        continue;
                    }

                    var roomIds = hotel.Rooms.Select(r => r.Id).ToList();

                    var occupiedRoomNights = await _db.BookingRooms
                        .Include(br => br.Booking)
                        .Where(br =>
                            roomIds.Contains(br.RoomId) &&
                            (br.Booking.Status == BookingStatus.CheckedIn || br.Booking.Status == BookingStatus.CheckedOut) &&
                            br.Booking.CheckInDate < end &&
                            br.Booking.CheckOutDate > start)
                        .Select(br => br.RoomId)
                        .Distinct()
                        .CountAsync();

                    var occupancyRate = totalRooms > 0
                        ? Math.Round((double)occupiedRoomNights / totalRooms * 100, 1)
                        : 0;

                    hotelDto.Periods.Add(new OccupancyPeriodDto
                    {
                        Label = label,
                        TotalRooms = totalRooms,
                        OccupiedRooms = occupiedRoomNights,
                        OccupancyRate = occupancyRate
                    });
                }

                result.Hotels.Add(hotelDto);
            }

            return result;
        }

        // ── Revenue ────────────────────────────────────────────────────────────

        public async Task<RevenueReportDto> GetRevenueAsync(string period, int? hotelId = null)
        {
            var now = DateTime.UtcNow;
            var (start, end) = GetOverallDateRange(period, now);

            var bookingQuery = _db.Bookings
                .Include(b => b.BookingRooms).ThenInclude(br => br.Room)
                .Include(b => b.BookingAncillaryServices).ThenInclude(bas => bas.AncillaryService)
                .Where(b =>
                    (b.Status == BookingStatus.CheckedOut || b.Status == BookingStatus.CheckedIn || b.Status == BookingStatus.Confirmed) &&
                    b.CreatedAt >= start && b.CreatedAt < end);

            if (hotelId.HasValue)
                bookingQuery = bookingQuery.Where(b => b.HotelId == hotelId.Value);

            var bookings = await bookingQuery.ToListAsync();

            var roomRevenue = bookings.Sum(b =>
                b.TotalPrice - b.BookingAncillaryServices.Sum(s => s.TotalPrice));
            var ancillaryRevenue = bookings.Sum(b => b.BookingAncillaryServices.Sum(s => s.TotalPrice));

            var byRoomType = bookings
                .SelectMany(b => b.BookingRooms.Select(br => new { br.Room.Type, BookingTotal = b.TotalPrice - b.BookingAncillaryServices.Sum(s => s.TotalPrice) }))
                .GroupBy(x => x.Type)
                .Select(g => new RevenueByRoomTypeDto
                {
                    RoomType = g.Key.ToString(),
                    BookingCount = g.Count(),
                    Revenue = g.Sum(x => x.BookingTotal)
                })
                .OrderByDescending(x => x.Revenue)
                .ToList();

            var byService = bookings
                .SelectMany(b => b.BookingAncillaryServices.Select(s => new { s.AncillaryService.Name, s.Quantity, s.TotalPrice }))
                .GroupBy(x => x.Name)
                .Select(g => new RevenueByServiceDto
                {
                    ServiceName = g.Key,
                    QuantitySold = g.Sum(x => x.Quantity),
                    Revenue = g.Sum(x => x.TotalPrice)
                })
                .OrderByDescending(x => x.Revenue)
                .ToList();

            var periods = BuildDatePeriods(period, now);
            var periodDtos = new List<RevenuePeriodDto>();

            foreach (var (label, pStart, pEnd) in periods)
            {
                var pBookings = bookings.Where(b => b.CreatedAt >= pStart && b.CreatedAt < pEnd).ToList();
                periodDtos.Add(new RevenuePeriodDto
                {
                    Label = label,
                    Revenue = pBookings.Sum(b => b.TotalPrice),
                    Bookings = pBookings.Count
                });
            }

            return new RevenueReportDto
            {
                Period = period,
                TotalRevenue = roomRevenue + ancillaryRevenue,
                RoomRevenue = roomRevenue,
                AncillaryRevenue = ancillaryRevenue,
                ByRoomType = byRoomType,
                ByAncillaryService = byService,
                Periods = periodDtos
            };
        }

        // ── Demographics ───────────────────────────────────────────────────────

        public async Task<DemographicsReportDto> GetDemographicsAsync(int? hotelId = null)
        {
            var bookingQuery = _db.Bookings
                .Include(b => b.Guest)
                .Include(b => b.Hotel)
                .Where(b => b.Status != BookingStatus.Cancelled);

            if (hotelId.HasValue)
                bookingQuery = bookingQuery.Where(b => b.HotelId == hotelId.Value);

            var bookings = await bookingQuery.ToListAsync();

            var guestBookingCounts = bookings
                .GroupBy(b => b.GuestId)
                .Select(g => new { GuestId = g.Key, Count = g.Count() })
                .ToList();

            var repeatGuests = guestBookingCounts.Count(g => g.Count > 1);
            var newGuests = guestBookingCounts.Count(g => g.Count == 1);
            var totalGuests = guestBookingCounts.Count;

            var avgStay = bookings.Any()
                ? bookings.Average(b => (b.CheckOutDate - b.CheckInDate).TotalDays)
                : 0;

            var byLocation = bookings
                .GroupBy(b => b.Hotel.Location)
                .Select(g => new BookingsByLocationDto
                {
                    Location = g.Key,
                    Bookings = g.Count(),
                    Revenue = g.Sum(b => b.TotalPrice)
                })
                .OrderByDescending(x => x.Bookings)
                .ToList();

            var topGuests = bookings
                .GroupBy(b => b.GuestId)
                .Select(g => new
                {
                    Guest = g.First().Guest,
                    TotalBookings = g.Count(),
                    TotalSpent = g.Sum(b => b.TotalPrice)
                })
                .OrderByDescending(g => g.TotalSpent)
                .Take(10)
                .Select(g => new TopGuestDto
                {
                    GuestName = $"{g.Guest.FirstName} {g.Guest.LastName}",
                    Email = g.Guest.Email ?? string.Empty,
                    TotalBookings = g.TotalBookings,
                    TotalSpent = g.TotalSpent
                })
                .ToList();

            return new DemographicsReportDto
            {
                TotalGuests = totalGuests,
                RepeatGuests = repeatGuests,
                NewGuests = newGuests,
                RepeatGuestRate = totalGuests > 0 ? Math.Round((double)repeatGuests / totalGuests * 100, 1) : 0,
                AverageStayDuration = Math.Round(avgStay, 1),
                ByLocation = byLocation,
                TopGuests = topGuests
            };
        }

        // ── Summary ────────────────────────────────────────────────────────────

        public async Task<SummaryReportDto> GetSummaryAsync(int? hotelId = null)
        {
            var bookingQuery = _db.Bookings
                .Where(b => b.Status != BookingStatus.Cancelled);

            if (hotelId.HasValue)
                bookingQuery = bookingQuery.Where(b => b.HotelId == hotelId.Value);

            var totalRevenue = await bookingQuery.SumAsync(b => (decimal?)b.TotalPrice) ?? 0m;
            var totalBookings = await bookingQuery.CountAsync();
            var activeGuests = await bookingQuery.CountAsync(b => b.Status == BookingStatus.CheckedIn);

            var roomQuery = _db.Rooms.Where(r => r.Status != RoomStatus.OutOfService);
            if (hotelId.HasValue)
                roomQuery = roomQuery.Where(r => r.HotelId == hotelId.Value);

            var totalRooms = await roomQuery.CountAsync();
            var occupiedRooms = await roomQuery.CountAsync(r => r.Status == RoomStatus.Occupied);
            var occupancyRate = totalRooms > 0
                ? Math.Round((double)occupiedRooms / totalRooms * 100, 1)
                : 0;

            return new SummaryReportDto
            {
                TotalRevenue = totalRevenue,
                TotalBookings = totalBookings,
                OccupancyRate = occupancyRate,
                ActiveGuests = activeGuests
            };
        }

        // ── Export ─────────────────────────────────────────────────────────────

        public async Task<(byte[] Data, string ContentType, string FileName)> ExportAsync(ExportRequestDto request)
        {
            var period = request.Period.ToLower();
            var report = request.Report.ToLower();
            var type = request.Type.ToLower();
            var timestamp = DateTime.UtcNow.ToString("yyyyMMdd-HHmm");
            var fileName = $"HMS-{report}-{period}-{timestamp}";

            var hotelId = request.HotelId;

            if (type == "excel")
            {
                var bytes = report switch
                {
                    "revenue" => await ExportRevenueExcelAsync(period, hotelId),
                    "demographics" => await ExportDemographicsExcelAsync(hotelId),
                    _ => await ExportOccupancyExcelAsync(period, hotelId)
                };
                return (bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", $"{fileName}.xlsx");
            }
            else
            {
                var bytes = report switch
                {
                    "revenue" => await ExportRevenuePdfAsync(period, hotelId),
                    "demographics" => await ExportDemographicsPdfAsync(hotelId),
                    _ => await ExportOccupancyPdfAsync(period, hotelId)
                };
                return (bytes, "application/pdf", $"{fileName}.pdf");
            }
        }

        // ── PDF export helpers ─────────────────────────────────────────────────

        private async Task<byte[]> ExportOccupancyPdfAsync(string period, int? hotelId = null)
        {
            var data = await GetOccupancyAsync(period, hotelId);
            var ms = new MemoryStream();
            var doc = OpenPdfDocument(ms, out var document);

            AddPdfTitle(document, "Occupancy Report", $"Period: {period} | Generated: {DateTime.UtcNow:dd MMM yyyy HH:mm} UTC");

            foreach (var hotel in data.Hotels)
            {
                document.Add(new Paragraph(hotel.HotelName)
                    .SetFont(PdfFontFactory.CreateFont(StandardFonts.HELVETICA_BOLD))
                    .SetFontSize(11).SetMarginTop(12).SetMarginBottom(4));

                var table = new Table(UnitValue.CreatePercentArray([30f, 20f, 20f, 30f])).UseAllAvailableWidth();
                AddPdfHeader(table, "Period", "Total Rooms", "Occupied", "Occupancy %");

                foreach (var p in hotel.Periods)
                {
                    table.AddCell(MakePdfCell(p.Label));
                    table.AddCell(MakePdfCell(p.TotalRooms.ToString(), TextAlignment.CENTER));
                    table.AddCell(MakePdfCell(p.OccupiedRooms.ToString(), TextAlignment.CENTER));
                    table.AddCell(MakePdfCell($"{p.OccupancyRate:F1}%", TextAlignment.RIGHT));
                }
                document.Add(table);
            }

            document.Close();
            return ms.ToArray();
        }

        private async Task<byte[]> ExportRevenuePdfAsync(string period, int? hotelId = null)
        {
            var data = await GetRevenueAsync(period, hotelId);
            var ms = new MemoryStream();
            OpenPdfDocument(ms, out var document);

            AddPdfTitle(document, "Revenue Report", $"Period: {period} | Total: £{data.TotalRevenue:F2}");

            document.Add(new Paragraph("Revenue by Period")
                .SetFont(PdfFontFactory.CreateFont(StandardFonts.HELVETICA_BOLD)).SetFontSize(11).SetMarginTop(12).SetMarginBottom(4));

            var periodTable = new Table(UnitValue.CreatePercentArray([50f, 30f, 20f])).UseAllAvailableWidth();
            AddPdfHeader(periodTable, "Period", "Revenue", "Bookings");
            foreach (var p in data.Periods)
            {
                periodTable.AddCell(MakePdfCell(p.Label));
                periodTable.AddCell(MakePdfCell($"£{p.Revenue:F2}", TextAlignment.RIGHT));
                periodTable.AddCell(MakePdfCell(p.Bookings.ToString(), TextAlignment.CENTER));
            }
            document.Add(periodTable);

            document.Add(new Paragraph("Revenue by Room Type")
                .SetFont(PdfFontFactory.CreateFont(StandardFonts.HELVETICA_BOLD)).SetFontSize(11).SetMarginTop(12).SetMarginBottom(4));

            var roomTable = new Table(UnitValue.CreatePercentArray([40f, 30f, 30f])).UseAllAvailableWidth();
            AddPdfHeader(roomTable, "Room Type", "Bookings", "Revenue");
            foreach (var r in data.ByRoomType)
            {
                roomTable.AddCell(MakePdfCell(r.RoomType));
                roomTable.AddCell(MakePdfCell(r.BookingCount.ToString(), TextAlignment.CENTER));
                roomTable.AddCell(MakePdfCell($"£{r.Revenue:F2}", TextAlignment.RIGHT));
            }
            document.Add(roomTable);

            document.Close();
            return ms.ToArray();
        }

        private async Task<byte[]> ExportDemographicsPdfAsync(int? hotelId = null)
        {
            var data = await GetDemographicsAsync(hotelId);
            var ms = new MemoryStream();
            OpenPdfDocument(ms, out var document);

            AddPdfTitle(document, "Guest Demographics Report", $"Generated: {DateTime.UtcNow:dd MMM yyyy HH:mm} UTC");

            var regular = PdfFontFactory.CreateFont(StandardFonts.HELVETICA);
            var bold = PdfFontFactory.CreateFont(StandardFonts.HELVETICA_BOLD);
            var muted = new DeviceRgb(113, 128, 150);

            document.Add(new Paragraph($"Total Guests: {data.TotalGuests}  |  Repeat Guests: {data.RepeatGuests} ({data.RepeatGuestRate}%)  |  Avg Stay: {data.AverageStayDuration} nights")
                .SetFont(regular).SetFontSize(10).SetFontColor(muted).SetMarginBottom(12));

            document.Add(new Paragraph("Top 10 Guests by Spend")
                .SetFont(bold).SetFontSize(11).SetMarginBottom(4));

            var guestTable = new Table(UnitValue.CreatePercentArray([35f, 35f, 15f, 15f])).UseAllAvailableWidth();
            AddPdfHeader(guestTable, "Name", "Email", "Bookings", "Spent");
            foreach (var g in data.TopGuests)
            {
                guestTable.AddCell(MakePdfCell(g.GuestName));
                guestTable.AddCell(MakePdfCell(g.Email));
                guestTable.AddCell(MakePdfCell(g.TotalBookings.ToString(), TextAlignment.CENTER));
                guestTable.AddCell(MakePdfCell($"£{g.TotalSpent:F2}", TextAlignment.RIGHT));
            }
            document.Add(guestTable);

            document.Close();
            return ms.ToArray();
        }

        // ── Excel export helpers ───────────────────────────────────────────────

        private async Task<byte[]> ExportOccupancyExcelAsync(string period, int? hotelId = null)
        {
            var data = await GetOccupancyAsync(period, hotelId);
            using var wb = new XLWorkbook();

            foreach (var hotel in data.Hotels)
            {
                var ws = wb.Worksheets.Add(Truncate(hotel.HotelName, 31));
                ws.Cell(1, 1).Value = "Period";
                ws.Cell(1, 2).Value = "Total Rooms";
                ws.Cell(1, 3).Value = "Occupied Rooms";
                ws.Cell(1, 4).Value = "Occupancy %";
                StyleExcelHeader(ws, 1, 4);

                var row = 2;
                foreach (var p in hotel.Periods)
                {
                    ws.Cell(row, 1).Value = p.Label;
                    ws.Cell(row, 2).Value = p.TotalRooms;
                    ws.Cell(row, 3).Value = p.OccupiedRooms;
                    ws.Cell(row, 4).Value = p.OccupancyRate;
                    row++;
                }
                ws.Columns().AdjustToContents();
            }

            var ms = new MemoryStream();
            wb.SaveAs(ms);
            return ms.ToArray();
        }

        private async Task<byte[]> ExportRevenueExcelAsync(string period, int? hotelId = null)
        {
            var data = await GetRevenueAsync(period, hotelId);
            using var wb = new XLWorkbook();

            var wsPeriod = wb.Worksheets.Add("By Period");
            wsPeriod.Cell(1, 1).Value = "Period";
            wsPeriod.Cell(1, 2).Value = "Revenue (£)";
            wsPeriod.Cell(1, 3).Value = "Bookings";
            StyleExcelHeader(wsPeriod, 1, 3);
            var row = 2;
            foreach (var p in data.Periods)
            {
                wsPeriod.Cell(row, 1).Value = p.Label;
                wsPeriod.Cell(row, 2).Value = (double)p.Revenue;
                wsPeriod.Cell(row, 3).Value = p.Bookings;
                row++;
            }
            wsPeriod.Columns().AdjustToContents();

            var wsRoom = wb.Worksheets.Add("By Room Type");
            wsRoom.Cell(1, 1).Value = "Room Type";
            wsRoom.Cell(1, 2).Value = "Bookings";
            wsRoom.Cell(1, 3).Value = "Revenue (£)";
            StyleExcelHeader(wsRoom, 1, 3);
            row = 2;
            foreach (var r in data.ByRoomType)
            {
                wsRoom.Cell(row, 1).Value = r.RoomType;
                wsRoom.Cell(row, 2).Value = r.BookingCount;
                wsRoom.Cell(row, 3).Value = (double)r.Revenue;
                row++;
            }
            wsRoom.Columns().AdjustToContents();

            var wsSvc = wb.Worksheets.Add("By Service");
            wsSvc.Cell(1, 1).Value = "Service";
            wsSvc.Cell(1, 2).Value = "Qty Sold";
            wsSvc.Cell(1, 3).Value = "Revenue (£)";
            StyleExcelHeader(wsSvc, 1, 3);
            row = 2;
            foreach (var s in data.ByAncillaryService)
            {
                wsSvc.Cell(row, 1).Value = s.ServiceName;
                wsSvc.Cell(row, 2).Value = s.QuantitySold;
                wsSvc.Cell(row, 3).Value = (double)s.Revenue;
                row++;
            }
            wsSvc.Columns().AdjustToContents();

            var ms = new MemoryStream();
            wb.SaveAs(ms);
            return ms.ToArray();
        }

        private async Task<byte[]> ExportDemographicsExcelAsync(int? hotelId = null)
        {
            var data = await GetDemographicsAsync(hotelId);
            using var wb = new XLWorkbook();

            var wsSummary = wb.Worksheets.Add("Summary");
            wsSummary.Cell(1, 1).Value = "Metric";
            wsSummary.Cell(1, 2).Value = "Value";
            StyleExcelHeader(wsSummary, 1, 2);
            wsSummary.Cell(2, 1).Value = "Total Guests"; wsSummary.Cell(2, 2).Value = data.TotalGuests;
            wsSummary.Cell(3, 1).Value = "Repeat Guests"; wsSummary.Cell(3, 2).Value = data.RepeatGuests;
            wsSummary.Cell(4, 1).Value = "New Guests"; wsSummary.Cell(4, 2).Value = data.NewGuests;
            wsSummary.Cell(5, 1).Value = "Repeat Guest Rate (%)"; wsSummary.Cell(5, 2).Value = data.RepeatGuestRate;
            wsSummary.Cell(6, 1).Value = "Average Stay (nights)"; wsSummary.Cell(6, 2).Value = data.AverageStayDuration;
            wsSummary.Columns().AdjustToContents();

            var wsGuests = wb.Worksheets.Add("Top Guests");
            wsGuests.Cell(1, 1).Value = "Name";
            wsGuests.Cell(1, 2).Value = "Email";
            wsGuests.Cell(1, 3).Value = "Bookings";
            wsGuests.Cell(1, 4).Value = "Total Spent (£)";
            StyleExcelHeader(wsGuests, 1, 4);
            var row = 2;
            foreach (var g in data.TopGuests)
            {
                wsGuests.Cell(row, 1).Value = g.GuestName;
                wsGuests.Cell(row, 2).Value = g.Email;
                wsGuests.Cell(row, 3).Value = g.TotalBookings;
                wsGuests.Cell(row, 4).Value = (double)g.TotalSpent;
                row++;
            }
            wsGuests.Columns().AdjustToContents();

            var ms = new MemoryStream();
            wb.SaveAs(ms);
            return ms.ToArray();
        }

        // ── Shared PDF utilities ───────────────────────────────────────────────

        private static PdfDocument OpenPdfDocument(MemoryStream ms, out Document document)
        {
            var writer = new PdfWriter(ms);
            var pdf = new PdfDocument(writer);
            document = new Document(pdf);
            return pdf;
        }

        private static void AddPdfTitle(Document document, string title, string subtitle)
        {
            var dark = new DeviceRgb(26, 26, 46);
            var muted = new DeviceRgb(113, 128, 150);
            document.Add(new Paragraph(title)
                .SetFont(PdfFontFactory.CreateFont(StandardFonts.HELVETICA_BOLD))
                .SetFontSize(22).SetFontColor(dark).SetTextAlignment(TextAlignment.CENTER).SetMarginBottom(4));
            document.Add(new Paragraph(subtitle)
                .SetFont(PdfFontFactory.CreateFont(StandardFonts.HELVETICA))
                .SetFontSize(10).SetFontColor(muted).SetTextAlignment(TextAlignment.CENTER).SetMarginBottom(16));
        }

        private static void AddPdfHeader(Table table, params string[] headers)
        {
            var bold = PdfFontFactory.CreateFont(StandardFonts.HELVETICA_BOLD);
            var dark = new DeviceRgb(26, 26, 46);
            var light = new DeviceRgb(247, 250, 252);
            foreach (var h in headers)
                table.AddHeaderCell(new Cell()
                    .Add(new Paragraph(h).SetFont(bold).SetFontSize(9).SetFontColor(dark))
                    .SetBackgroundColor(light).SetPadding(6));
        }

        private static Cell MakePdfCell(string text, TextAlignment alignment = TextAlignment.LEFT) =>
            new Cell().Add(new Paragraph(text)
                .SetFont(PdfFontFactory.CreateFont(StandardFonts.HELVETICA)).SetFontSize(9))
                .SetPadding(5).SetTextAlignment(alignment);

        private static void StyleExcelHeader(IXLWorksheet ws, int row, int lastCol)
        {
            var range = ws.Range(ws.Cell(row, 1), ws.Cell(row, lastCol));
            range.Style.Font.Bold = true;
            range.Style.Fill.BackgroundColor = XLColor.FromArgb(26, 26, 46);
            range.Style.Font.FontColor = XLColor.White;
        }

        private static string Truncate(string value, int maxLength) =>
            value.Length <= maxLength ? value : value[..maxLength];

        // ── Date period helpers ────────────────────────────────────────────────

        private static List<(string Label, DateTime Start, DateTime End)> BuildDatePeriods(string period, DateTime now)
        {
            return period.ToLower() switch
            {
                "daily" => Enumerable.Range(0, 7)
                    .Select(i =>
                    {
                        var d = now.Date.AddDays(-i);
                        return (d.ToString("dd MMM"), d, d.AddDays(1));
                    })
                    .Reverse()
                    .ToList(),

                "yearly" => Enumerable.Range(0, 3)
                    .Select(i =>
                    {
                        var year = now.Year - i;
                        return (year.ToString(), new DateTime(year, 1, 1), new DateTime(year + 1, 1, 1));
                    })
                    .Reverse()
                    .ToList(),

                _ => Enumerable.Range(0, 12)
                    .Select(i =>
                    {
                        var d = new DateTime(now.Year, now.Month, 1).AddMonths(-i);
                        return (d.ToString("MMM yyyy"), d, d.AddMonths(1));
                    })
                    .Reverse()
                    .ToList()
            };
        }

        private static (DateTime Start, DateTime End) GetOverallDateRange(string period, DateTime now)
        {
            return period.ToLower() switch
            {
                "daily" => (now.Date.AddDays(-7), now.Date.AddDays(1)),
                "yearly" => (new DateTime(now.Year - 2, 1, 1), new DateTime(now.Year + 1, 1, 1)),
                _ => (new DateTime(now.Year, now.Month, 1).AddMonths(-11), new DateTime(now.Year, now.Month, 1).AddMonths(1))
            };
        }
    }
}
