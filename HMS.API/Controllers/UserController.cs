// Student ID: S2401276
// Student Name: Mohamed Iyaadh Ahmed
// Module: Advanced Software Development (UFCF8S-30-2)

using HMS.API.DTOs.User;
using HMS.API.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace HMS.API.Controllers
{
    [ApiController]
    [Route("api/users")]
    [Authorize(Roles = "Manager,Admin")]
    public class UserController : ControllerBase
    {
        private readonly UserManager<User> _userManager;

        public UserController(UserManager<User> userManager)
        {
            _userManager = userManager;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var callerRole = User.FindFirstValue(ClaimTypes.Role)
                ?? User.FindFirstValue("role")
                ?? string.Empty;

            IQueryable<User> query = _userManager.Users;

            if (callerRole == "Manager")
                query = query.Where(u => u.Role == "FrontDesk");

            var users = await query
                .OrderBy(u => u.LastName)
                .Select(u => new UserDto
                {
                    Id = u.Id,
                    FirstName = u.FirstName,
                    LastName = u.LastName,
                    Email = u.Email ?? string.Empty,
                    PhoneNumber = u.PhoneNumber,
                    Role = u.Role,
                    IsActive = u.IsActive,
                    CreatedAt = u.CreatedAt,
                    LastPasswordChange = u.LastPasswordChange
                })
                .ToListAsync();

            return Ok(users);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user is null) return NotFound();

            var callerRole = User.FindFirstValue(ClaimTypes.Role)
                ?? User.FindFirstValue("role")
                ?? string.Empty;

            if (callerRole == "Manager" && user.Role != "FrontDesk")
                return Forbid();

            return Ok(ToDto(user));
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateUserDto dto)
        {
            var callerRole = User.FindFirstValue(ClaimTypes.Role)
                ?? User.FindFirstValue("role")
                ?? string.Empty;

            if (callerRole == "Manager" && dto.Role != "FrontDesk")
                return Forbid();

            var allowedRoles = new[] { "Guest", "FrontDesk", "Manager", "Admin" };
            if (!allowedRoles.Contains(dto.Role))
                return BadRequest(new { message = $"Invalid role '{dto.Role}'." });

            var user = new User
            {
                FirstName = dto.FirstName,
                LastName = dto.LastName,
                Email = dto.Email,
                UserName = dto.Email,
                PhoneNumber = dto.PhoneNumber,
                Role = dto.Role,
                IsActive = true
            };

            var result = await _userManager.CreateAsync(user, dto.Password);
            if (!result.Succeeded)
                return BadRequest(new { errors = result.Errors.Select(e => e.Description) });

            await _userManager.AddToRoleAsync(user, dto.Role);

            return CreatedAtAction(nameof(GetById), new { id = user.Id }, ToDto(user));
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(string id, [FromBody] UpdateUserDto dto)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user is null) return NotFound();

            var callerRole = User.FindFirstValue(ClaimTypes.Role)
                ?? User.FindFirstValue("role")
                ?? string.Empty;

            if (callerRole == "Manager" && user.Role != "FrontDesk")
                return Forbid();

            user.FirstName = dto.FirstName;
            user.LastName = dto.LastName;
            user.PhoneNumber = dto.PhoneNumber;

            var result = await _userManager.UpdateAsync(user);
            if (!result.Succeeded)
                return BadRequest(new { errors = result.Errors.Select(e => e.Description) });

            return Ok(ToDto(user));
        }

        [HttpPatch("{id}/status")]
        public async Task<IActionResult> UpdateStatus(string id, [FromBody] UpdateUserStatusDto dto)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user is null) return NotFound();

            var callerRole = User.FindFirstValue(ClaimTypes.Role)
                ?? User.FindFirstValue("role")
                ?? string.Empty;

            if (callerRole == "Manager" && user.Role != "FrontDesk")
                return Forbid();

            user.IsActive = dto.IsActive;
            var result = await _userManager.UpdateAsync(user);
            if (!result.Succeeded)
                return BadRequest(new { errors = result.Errors.Select(e => e.Description) });

            return Ok(new { id = user.Id, isActive = user.IsActive });
        }

        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Delete(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user is null) return NotFound();

            user.IsActive = false;
            var result = await _userManager.UpdateAsync(user);
            if (!result.Succeeded)
                return BadRequest(new { errors = result.Errors.Select(e => e.Description) });

            return NoContent();
        }

        private static UserDto ToDto(User u) => new()
        {
            Id = u.Id,
            FirstName = u.FirstName,
            LastName = u.LastName,
            Email = u.Email ?? string.Empty,
            PhoneNumber = u.PhoneNumber,
            Role = u.Role,
            IsActive = u.IsActive,
            CreatedAt = u.CreatedAt,
            LastPasswordChange = u.LastPasswordChange
        };
    }
}
