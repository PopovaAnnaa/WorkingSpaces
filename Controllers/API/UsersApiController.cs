using Microsoft.AspNetCore.Mvc;
using WorkingSpaces.Data;
using WorkingSpaces.Models;
using WorkingSpaces.Models.Dto;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authentication.JwtBearer;

namespace WorkingSpaces.Controllers.Api
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    public class UsersController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public UsersController(ApplicationDbContext context)
        {
            _context = context;
        }
        [HttpGet]
        public async Task<ActionResult<IEnumerable<UserDto>>> GetUsers()
        {
            var users = await _context.Users.ToListAsync();
            var userDtos = users.Select(user => new UserDto
            {
                UserId = user.UserId,
                Username = user.Username,
                FullName = user.FullName,
                PhoneNumber = user.PhoneNumber,
                Email = user.Email
            }).ToList();

            return Ok(userDtos);
        }
        [HttpGet("{id}")]
        public async Task<ActionResult<UserDto>> GetUserById(Guid id)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.UserId == id);

            if (user == null)
            {
                return NotFound();
            }
            var userDto = new UserDto
            {
                UserId = user.UserId,
                Username = user.Username,
                FullName = user.FullName,
                PhoneNumber = user.PhoneNumber,
                Email = user.Email
            };

            return Ok(userDto);
        }
        [HttpPatch("{id}")]
        public async Task<IActionResult> UpdateUser(Guid id, UpdateUserDto updateUserDto)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null)
            {
                return NotFound();
            }

            if (!string.IsNullOrWhiteSpace(updateUserDto.UserName))
            {
                var newUsernameLower = updateUserDto.UserName.ToLower();
                if (user.Username != newUsernameLower &&
                    await _context.Users.AnyAsync(u => u.Username == newUsernameLower && u.UserId != id))
                {
                    ModelState.AddModelError(nameof(updateUserDto.UserName), "This username is already taken.");
                }
            }
            if (!string.IsNullOrWhiteSpace(updateUserDto.Email))
            {
                var newEmailLower = updateUserDto.Email.ToLower();
                if (user.Email != newEmailLower &&
                    await _context.Users.AnyAsync(u => u.Email == newEmailLower && u.UserId != id))
                {
                    ModelState.AddModelError(nameof(updateUserDto.Email), "This email is already in use.");
                }
            }
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }
            if (!string.IsNullOrWhiteSpace(updateUserDto.UserName))
            {
                user.Username = updateUserDto.UserName.ToLower();
            }
            if (!string.IsNullOrWhiteSpace(updateUserDto.FullName))
            {
                user.FullName = updateUserDto.FullName;
            }
            if (!string.IsNullOrWhiteSpace(updateUserDto.PhoneNumber))
            {
                user.PhoneNumber = updateUserDto.PhoneNumber;
            }
            if (!string.IsNullOrWhiteSpace(updateUserDto.Email))
            {
                user.Email = updateUserDto.Email.ToLower();
            }
            _context.Entry(user).State = EntityState.Modified;
            await _context.SaveChangesAsync();

            return NoContent();
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteUser(Guid id)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null)
            {
                return NotFound();
            }

            _context.Users.Remove(user);
            await _context.SaveChangesAsync();

            return NoContent();
        }
    }
}