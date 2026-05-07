// Student ID: S2401276
// Student Name: Mohamed Iyaadh Ahmed
// Module: Advanced Software Development (UFCF8S-30-2)

using HMS.API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace HMS.API.Controllers
{
    [ApiController]
    [Route("api/checkout")]
    [Authorize(Roles = "FrontDesk,Manager,Admin")]
    public class CheckOutController : ControllerBase
    {
        private readonly ICheckInService _checkInService;

        public CheckOutController(ICheckInService checkInService)
        {
            _checkInService = checkInService;
        }

        [HttpPost("{bookingId:int}")]
        public async Task<IActionResult> CheckOut(int bookingId)
        {
            var staffUserId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
            var booking = await _checkInService.CheckOutAsync(bookingId, staffUserId);
            return Ok(booking);
        }
    }
}
