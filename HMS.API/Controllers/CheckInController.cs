
using HMS.API.DTOs.CheckIn;
using HMS.API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace HMS.API.Controllers
{
    [ApiController]
    [Route("api/checkin")]
    [Authorize(Roles = "FrontDesk,Manager,Admin")]
    public class CheckInController : ControllerBase
    {
        private readonly ICheckInService _checkInService;

        public CheckInController(ICheckInService checkInService)
        {
            _checkInService = checkInService;
        }

        [HttpPost("scan")]
        public async Task<IActionResult> Scan([FromBody] ScanQrDto dto)
        {
            var booking = await _checkInService.ScanAsync(dto.ReferenceNumber);
            return Ok(booking);
        }

        [HttpPost("{bookingId:int}")]
        public async Task<IActionResult> CheckIn(int bookingId)
        {
            var staffUserId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
            var booking = await _checkInService.CheckInAsync(bookingId, staffUserId);
            return Ok(booking);
        }
    }
}
