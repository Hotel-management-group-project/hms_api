// Student ID: S2401276
// Student Name: Mohamed Iyaadh Ahmed
// Module: Advanced Software Development (UFCF8S-30-2)

using HMS.API.DTOs.Payment;
using HMS.API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace HMS.API.Controllers
{
    [ApiController]
    [Route("api/payments")]
    [Authorize]
    public class PaymentController : ControllerBase
    {
        private readonly IPaymentService _paymentService;

        public PaymentController(IPaymentService paymentService)
        {
            _paymentService = paymentService;
        }

        [HttpPost("process")]
        public async Task<IActionResult> Process([FromBody] ProcessPaymentDto dto)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
            var result = await _paymentService.ProcessAsync(dto, userId);
            return result.Success ? Ok(result) : UnprocessableEntity(result);
        }

        [HttpGet("{bookingId:int}")]
        [Authorize(Roles = "FrontDesk,Manager,Admin")]
        public async Task<IActionResult> GetByBooking(int bookingId)
        {
            var payment = await _paymentService.GetByBookingAsync(bookingId);
            return payment is null ? NotFound() : Ok(payment);
        }
    }
}
