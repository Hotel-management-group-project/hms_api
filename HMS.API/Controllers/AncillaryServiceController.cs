// Student ID: S2401276
// Student Name: Mohamed Iyaadh Ahmed
// Module: Advanced Software Development (UFCF8S-30-2)

using HMS.API.Data;
using HMS.API.DTOs.AncillaryService;
using HMS.API.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HMS.API.Controllers
{
    [ApiController]
    [Route("api/ancillary-services")]
    public class AncillaryServiceController : ControllerBase
    {
        private readonly ApplicationDbContext _db;

        public AncillaryServiceController(ApplicationDbContext db)
        {
            _db = db;
        }

        [HttpGet]
        [Authorize]
        public async Task<IActionResult> GetAll()
        {
            var services = await _db.AncillaryServices
                .OrderBy(s => s.Name)
                .Select(s => new AncillaryServiceDto
                {
                    Id = s.Id,
                    Name = s.Name,
                    Price = s.Price,
                    Description = s.Description
                })
                .ToListAsync();

            return Ok(services);
        }

        [HttpGet("{id:int}")]
        [Authorize]
        public async Task<IActionResult> GetById(int id)
        {
            var service = await _db.AncillaryServices.FindAsync(id);
            if (service is null) return NotFound();

            return Ok(new AncillaryServiceDto
            {
                Id = service.Id,
                Name = service.Name,
                Price = service.Price,
                Description = service.Description
            });
        }

        [HttpPost]
        [Authorize(Roles = "Manager,Admin")]
        public async Task<IActionResult> Create([FromBody] CreateAncillaryServiceDto dto)
        {
            var service = new AncillaryService
            {
                Name = dto.Name,
                Price = dto.Price,
                Description = dto.Description
            };

            _db.AncillaryServices.Add(service);
            await _db.SaveChangesAsync();

            return CreatedAtAction(nameof(GetById), new { id = service.Id }, new AncillaryServiceDto
            {
                Id = service.Id,
                Name = service.Name,
                Price = service.Price,
                Description = service.Description
            });
        }

        [HttpPut("{id:int}")]
        [Authorize(Roles = "Manager,Admin")]
        public async Task<IActionResult> Update(int id, [FromBody] UpdateAncillaryServiceDto dto)
        {
            var service = await _db.AncillaryServices.FindAsync(id);
            if (service is null) return NotFound();

            service.Name = dto.Name;
            service.Price = dto.Price;
            service.Description = dto.Description;

            await _db.SaveChangesAsync();

            return Ok(new AncillaryServiceDto
            {
                Id = service.Id,
                Name = service.Name,
                Price = service.Price,
                Description = service.Description
            });
        }

        [HttpDelete("{id:int}")]
        [Authorize(Roles = "Manager,Admin")]
        public async Task<IActionResult> Delete(int id)
        {
            var service = await _db.AncillaryServices.FindAsync(id);
            if (service is null) return NotFound();

            _db.AncillaryServices.Remove(service);
            await _db.SaveChangesAsync();

            return NoContent();
        }
    }
}
