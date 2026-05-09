// Student ID: S2401276
// Student Name: Mohamed Iyaadh Ahmed
// Module: Advanced Software Development (UFCF8S-30-2)

using HMS.API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HMS.API.Controllers
{
    [ApiController]
    [Route("api/upload")]
    [Authorize(Roles = "Admin,Manager")]
    public class UploadController : ControllerBase
    {
        private readonly IUploadService _uploadService;

        public UploadController(IUploadService uploadService)
        {
            _uploadService = uploadService;
        }

        /// <summary>
        /// POST /api/upload/hotel-image
        /// Uploads a hotel image to Cloudinary. Returns the secure URL.
        /// </summary>
        [HttpPost("hotel-image")]
        public async Task<IActionResult> UploadHotelImage(IFormFile file)
        {
            try
            {
                var url = await _uploadService.UploadImageAsync(file, "hotels");
                return Ok(new { url });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return StatusCode(500, new { message = ex.Message });
            }
        }

        /// <summary>
        /// POST /api/upload/room-image
        /// Uploads a room image to Cloudinary. Returns the secure URL.
        /// </summary>
        [HttpPost("room-image")]
        public async Task<IActionResult> UploadRoomImage(IFormFile file)
        {
            try
            {
                var url = await _uploadService.UploadImageAsync(file, "rooms");
                return Ok(new { url });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return StatusCode(500, new { message = ex.Message });
            }
        }
    }
}
