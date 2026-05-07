// Student ID: S2401276
// Student Name: Mohamed Iyaadh Ahmed
// Module: Advanced Software Development (UFCF8S-30-2)

using HMS.API.Data;
using HMS.API.DTOs.AuditLog;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HMS.API.Controllers
{
    [ApiController]
    [Route("api/auditlogs")]
    [Authorize(Roles = "Manager,Admin")]
    public class AuditLogController : ControllerBase
    {
        private readonly ApplicationDbContext _db;

        public AuditLogController(ApplicationDbContext db)
        {
            _db = db;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll(
            [FromQuery] string? userId,
            [FromQuery] string? action,
            [FromQuery] string? entityType,
            [FromQuery] DateTime? from,
            [FromQuery] DateTime? to,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 50)
        {
            pageSize = Math.Clamp(pageSize, 1, 200);
            page = Math.Max(1, page);

            var query = _db.AuditLogs
                .Include(a => a.User)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(userId))
                query = query.Where(a => a.UserId == userId);

            if (!string.IsNullOrWhiteSpace(action))
                query = query.Where(a => a.Action.Contains(action));

            if (!string.IsNullOrWhiteSpace(entityType))
                query = query.Where(a => a.EntityType == entityType);

            if (from.HasValue)
                query = query.Where(a => a.CreatedAt >= from.Value.ToUniversalTime());

            if (to.HasValue)
                query = query.Where(a => a.CreatedAt <= to.Value.ToUniversalTime());

            var total = await query.CountAsync();
            var items = await query
                .OrderByDescending(a => a.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(a => new AuditLogDto
                {
                    Id = a.Id,
                    UserId = a.UserId,
                    UserName = a.User != null ? $"{a.User.FirstName} {a.User.LastName}" : null,
                    Action = a.Action,
                    EntityType = a.EntityType,
                    EntityId = a.EntityId,
                    IpAddress = a.IpAddress,
                    Details = a.Details,
                    CreatedAt = a.CreatedAt
                })
                .ToListAsync();

            return Ok(new PagedResult<AuditLogDto>
            {
                Items = items,
                TotalCount = total,
                Page = page,
                PageSize = pageSize
            });
        }

        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetById(int id)
        {
            var log = await _db.AuditLogs
                .Include(a => a.User)
                .FirstOrDefaultAsync(a => a.Id == id);

            if (log is null) return NotFound();

            return Ok(new AuditLogDto
            {
                Id = log.Id,
                UserId = log.UserId,
                UserName = log.User != null ? $"{log.User.FirstName} {log.User.LastName}" : null,
                Action = log.Action,
                EntityType = log.EntityType,
                EntityId = log.EntityId,
                IpAddress = log.IpAddress,
                Details = log.Details,
                CreatedAt = log.CreatedAt
            });
        }
    }
}
