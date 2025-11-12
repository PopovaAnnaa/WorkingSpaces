using Microsoft.AspNetCore.Mvc;
using WorkingSpaces.Data;
using WorkingSpaces.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.AspNetCore.Authorization;
using WorkingSpaces.Models.Dto;
using Microsoft.AspNetCore.Authentication.JwtBearer;

namespace WorkingSpaces.Controllers.Api
{
    [ApiController]
    [ApiVersion("1.0")]
    [ApiVersion("2.0")]
    [Route("api/v{version:apiVersion}/accountapi")]
    public class AccountApiController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly IConfiguration _config;

        public AccountApiController(ApplicationDbContext context, IConfiguration config)
        {
            _context = context;
            _config = config;
        }

        private TimeZoneInfo GetKyivTimeZone()
        {
            try
            {
                return TimeZoneInfo.FindSystemTimeZoneById("Europe/Kyiv");
            }
            catch (TimeZoneNotFoundException)
            {
                return TimeZoneInfo.FindSystemTimeZoneById("E. Europe Standard Time");
            }
        }

        [MapToApiVersion("1.0")]
        [MapToApiVersion("2.0")]
        [HttpPost("register")]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            var userNameLower = model.UserName.ToLower();
            if (await _context.Users.AnyAsync(u => u.Username == userNameLower))
            {
                ModelState.AddModelError(nameof(model.UserName), "This username is already taken.");
            }
            var emailLower = model.Email.ToLower();
            if (await _context.Users.AnyAsync(u => u.Email == emailLower))
            {
                ModelState.AddModelError(nameof(model.Email), "This email is already in use.");
            }

            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            string passwordHash = BCrypt.Net.BCrypt.HashPassword(model.Password);
            var newUser = new User
            {
                UserId = Guid.NewGuid(),
                Username = userNameLower,
                FullName = model.FullName,
                Password = passwordHash,
                PhoneNumber = model.PhoneNumber,
                Email = emailLower
            };
            _context.Users.Add(newUser);

            try
            {
                await _context.SaveChangesAsync();
                return Ok(new { message = "User registered successfully" });
            }
            catch (DbUpdateException)
            {
                ModelState.AddModelError(string.Empty, "An error occurred. Username or email might be taken.");
                return BadRequest(ModelState);
            }
        }

        private string GenerateJwtToken(User user)
        {
            var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config["Jwt:Key"]!));
            var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, user.UserId.ToString()),
                new Claim(ClaimTypes.Name, user.Username),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim("FullName", user.FullName)
            };

            var token = new JwtSecurityToken(
                issuer: _config["Jwt:Issuer"],
                audience: _config["Jwt:Audience"],
                claims: claims,
                expires: DateTime.Now.AddHours(1),
                signingCredentials: credentials);

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        [MapToApiVersion("1.0")]
        [MapToApiVersion("2.0")]
        [HttpPost("login")]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var userNameLower = model.UserName.ToLower();
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Username == userNameLower);

            if (user == null || !BCrypt.Net.BCrypt.Verify(model.Password, user.Password))
            {
                return Unauthorized(new { message = "Invalid username or password" });
            }
            var tokenString = GenerateJwtToken(user);
            return Ok(new
            {
                token = tokenString,
                message = "Login successful"
            });
        }
        
        [HttpGet("profile")]
        [MapToApiVersion("1.0")]
        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
        public async Task<ActionResult<UserDtoV1>> ProfileV1()
        {
            var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userIdString)) return Unauthorized();

            var user = await _context.Users.FindAsync(Guid.Parse(userIdString));
            if (user == null) return NotFound();

            var userDto = new UserDtoV1
            {
                UserId = user.UserId,
                Username = user.Username,
                FullName = user.FullName,
                PhoneNumber = user.PhoneNumber,
                Email = user.Email
            };

            return Ok(userDto);
        }

        [HttpGet("profile")]
        [MapToApiVersion("2.0")]
        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
        public async Task<ActionResult<UserDtoV2>> ProfileV2()
        {
            var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userIdString)) return Unauthorized();

            var user = await _context.Users
                .Include(u => u.Bookings)           
                    .ThenInclude(b => b.Space)     
                .FirstOrDefaultAsync(u => u.UserId == Guid.Parse(userIdString));

            if (user == null) return NotFound();

            var kyivZone = GetKyivTimeZone();

            var userDtoV2 = new UserDtoV2
            {
                UserId = user.UserId,
                Username = user.Username,
                FullName = user.FullName,
                PhoneNumber = user.PhoneNumber,
                Email = user.Email,

                Bookings = user.Bookings.Select(b => new BookingDto
                {
                    BookingId = b.BookingId,
                    SpaceId = b.SpaceId,
                    SpaceName = b.Space.Name,
                    UserId = b.UserId,
                    UserFullName = user.FullName,
                    StartTime = TimeZoneInfo.ConvertTime(b.StartTime, kyivZone).DateTime,
                    EndTime = TimeZoneInfo.ConvertTime(b.EndTime, kyivZone).DateTime
                }).OrderBy(b => b.StartTime).ToList()
            };

            return Ok(userDtoV2);
        }
    }
}