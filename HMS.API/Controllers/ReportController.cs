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

        [HttpGet("occupancy")]
        public async Task<IActionResult> Occupancy(
            [FromQuery] string period = "monthly",
            [FromQuery] int? hotelId = null)
        {
            var result = await _reportService.GetOccupancyAsync(period, hotelId);
            return Ok(result);
        }

        [HttpGet("revenue")]
        public async Task<IActionResult> Revenue(
            [FromQuery] string period = "monthly",
            [FromQuery] int? hotelId = null)
        {
            var result = await _reportService.GetRevenueAsync(period, hotelId);
            return Ok(result);
        }

        [HttpGet("demographics")]
        public async Task<IActionResult> Demographics([FromQuery] int? hotelId = null)
        {
            var result = await _reportService.GetDemographicsAsync(hotelId);
            return Ok(result);
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
