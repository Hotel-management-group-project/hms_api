// Student ID: S2401276
// Student Name: Mohamed Iyaadh Ahmed
// Module: Advanced Software Development (UFCF8S-30-2)

using HMS.API.DTOs.Report;
using HMS.API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HMS.API.Controllers
{
    [ApiController]
    [Route("api/reports")]
    [Authorize(Roles = "Manager,Admin")]
    public class ReportController : ControllerBase
    {
        private readonly IReportService _reportService;

        public ReportController(IReportService reportService)
        {
            _reportService = reportService;
        }

        [HttpGet("summary")]
        public async Task<IActionResult> Summary([FromQuery] int? hotelId = null)
        {
            var result = await _reportService.GetSummaryAsync(hotelId);
            return Ok(result);
        }

        [HttpGet("occupancy")]
        public async Task<IActionResult> Occupancy(
            [FromQuery] string period = "monthly",
            [FromQuery] int? hotelId = null)
        {
            var result = await _reportService.GetOccupancyAsync(period, hotelId);

            // Flatten: aggregate across all hotels per period, preserving period order
            var periodLabels = result.Hotels.FirstOrDefault()?.Periods.Select(p => p.Label).ToList() ?? [];

            var flat = periodLabels.Select(label =>
            {
                var matching = result.Hotels
                    .SelectMany(h => h.Periods.Where(p => p.Label == label))
                    .ToList();
                var totalRooms = matching.Sum(p => p.TotalRooms);
                var occupied = matching.Sum(p => p.OccupiedRooms);
                return new OccupancyDataPoint
                {
                    Period = label,
                    OccupancyRate = totalRooms > 0 ? Math.Round((double)occupied / totalRooms * 100, 1) : 0,
                    TotalRooms = totalRooms,
                    Occupied = occupied
                };
            }).ToList();

            return Ok(flat);
        }

        [HttpGet("revenue")]
        public async Task<IActionResult> Revenue(
            [FromQuery] string period = "monthly",
            [FromQuery] int? hotelId = null)
        {
            var result = await _reportService.GetRevenueAsync(period, hotelId);

            var flat = result.Periods.Select(p => new RevenueDataPoint
            {
                Period = p.Label,
                Revenue = p.Revenue,
                BookingCount = p.Bookings
            }).ToList();

            return Ok(flat);
        }

        [HttpGet("demographics")]
        public async Task<IActionResult> Demographics([FromQuery] int? hotelId = null)
        {
            var result = await _reportService.GetDemographicsAsync(hotelId);

            var total = result.ByLocation.Sum(l => l.Bookings);
            var flat = result.ByLocation.Select(l => new DemographicItem
            {
                Label = l.Location,
                Value = l.Bookings,
                Percentage = total > 0 ? Math.Round((double)l.Bookings / total * 100, 1) : 0
            }).ToList();

            return Ok(flat);
        }

        [HttpGet("export")]
        public async Task<IActionResult> Export(
            [FromQuery] string type = "pdf",
            [FromQuery] string report = "occupancy",
            [FromQuery] string period = "monthly",
            [FromQuery] int? hotelId = null)
        {
            var request = new ExportRequestDto { Type = type, Report = report, Period = period, HotelId = hotelId };
            var (data, contentType, fileName) = await _reportService.ExportAsync(request);
            return File(data, contentType, fileName);
        }
    }
}
