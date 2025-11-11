using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WorkingSpaces.Data;
using WorkingSpaces.Models;
using WorkingSpaces.Models.Dto;
using Microsoft.AspNetCore.Authorization; 
using Microsoft.AspNetCore.Authentication.JwtBearer; 
using System.Security.Claims;

namespace WorkingSpaces.Controllers.Api
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    public class BookingsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public BookingsController(ApplicationDbContext context)
        {
            _context = context;
        }

        private async Task<IActionResult?> ValidateBookingTime(int spaceId, DateTimeOffset startOffset, DateTimeOffset endOffset, int? bookingIdToExclude = null)
        {
            if (!await _context.Spaces.AnyAsync(s => s.SpaceId == spaceId))
            {
                return NotFound(new { message = "Error: Room not found." });
            }
            if (startOffset.Hour < 8 || endOffset.Hour > 23)
            {
                return BadRequest(new { message = "Error: Invalid time (8:00-23:00)." });
            }
            if (startOffset < DateTimeOffset.Now)
            {
                return BadRequest(new { message = "Error: Invalid time (in the past tense)." });
            }
            if (endOffset <= startOffset)
            {
                return BadRequest(new { message = "Error: Invalid time (end later than start)." });
            }
            if ((endOffset - startOffset).TotalMinutes < 15)
            {
                return BadRequest(new { message = "Error: Reservation minimum 15 minutes." });
            }

            var startOffsetUtc = startOffset.ToUniversalTime();
            var endOffsetUtc = endOffset.ToUniversalTime();
            bool isOverlap;
            if (_context.Database.IsSqlite())
            {
                var query = _context.Bookings.Where(b => b.SpaceId == spaceId);
                if (bookingIdToExclude.HasValue)
                {
                    query = query.Where(b => b.BookingId != bookingIdToExclude.Value);
                }
                var bookingsForRoom = await query.ToListAsync();
                isOverlap = bookingsForRoom.Any(b => startOffsetUtc < b.EndTime && endOffsetUtc > b.StartTime);
            }
            else
            {
                var query = _context.Bookings.AsQueryable();
                if (bookingIdToExclude.HasValue)
                {
                    query = query.Where(b => b.BookingId != bookingIdToExclude.Value);
                }
                isOverlap = await query.AnyAsync(b =>
                    b.SpaceId == spaceId &&
                    startOffsetUtc < b.EndTime &&
                    endOffsetUtc > b.StartTime);
            }

            if (isOverlap)
            {
                return Conflict(new { message = "Error: This time is already taken." });
            }

            return null;
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

        [HttpPost]
        public async Task<IActionResult> BookRoom(CreateBookingDto dto)
        {
            var kyivZone = GetKyivTimeZone();
            var startOffset = new DateTimeOffset(dto.StartTime, kyivZone.GetUtcOffset(dto.StartTime));
            var endOffset = new DateTimeOffset(dto.EndTime, kyivZone.GetUtcOffset(dto.EndTime));

            var validationError = await ValidateBookingTime(dto.SpaceId, startOffset, endOffset);
            if (validationError != null)
            {
                return validationError;
            }

            var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userIdString) || !Guid.TryParse(userIdString, out var userId))
            {
                return Unauthorized();
            }

            var newBooking = new Booking
            {
                SpaceId = dto.SpaceId,
                UserId = userId,
                StartTime = startOffset.ToUniversalTime(), 
                EndTime = endOffset.ToUniversalTime()
            };

            _context.Bookings.Add(newBooking);
            await _context.SaveChangesAsync();
            
            await _context.Entry(newBooking).Reference(b => b.Space).LoadAsync();
            await _context.Entry(newBooking).Reference(b => b.User).LoadAsync();
            
            var bookingDto = new BookingDto
            {
                BookingId = newBooking.BookingId,
                StartTime = TimeZoneInfo.ConvertTime(newBooking.StartTime, kyivZone).DateTime,
                EndTime = TimeZoneInfo.ConvertTime(newBooking.EndTime, kyivZone).DateTime,
                SpaceId = newBooking.SpaceId,
                SpaceName = newBooking.Space.Name,
                UserId = newBooking.UserId,
                UserFullName = newBooking.User.FullName
            };

            return CreatedAtAction(nameof(GetBookingById), new { id = newBooking.BookingId }, bookingDto);
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<BookingDto>>> ListBookings()
        {
            var kyivZone = GetKyivTimeZone();
            var query = _context.Bookings
                .Include(b => b.Space) 
                .Include(b => b.User);

            List<Booking> currentBookings;
            if (_context.Database.IsSqlite())
            {
                var bookingsFromDb = await query.ToListAsync();
                currentBookings = bookingsFromDb.OrderBy(b => b.StartTime).ToList();
            }
            else
            {
                currentBookings = await query.OrderBy(b => b.StartTime).ToListAsync();
            }

            var bookingDtos = currentBookings.Select(b => new BookingDto
            {
                BookingId = b.BookingId,
                StartTime = TimeZoneInfo.ConvertTime(b.StartTime, kyivZone).DateTime,
                EndTime = TimeZoneInfo.ConvertTime(b.EndTime, kyivZone).DateTime,
                SpaceId = b.SpaceId,
                SpaceName = b.Space.Name,
                UserId = b.UserId,
                UserFullName = b.User.FullName
            });

            return Ok(bookingDtos);
        }

        [HttpGet("my-bookings")]
        public async Task<ActionResult<IEnumerable<BookingDto>>> GetMyBookings()
        {
            var kyivZone = GetKyivTimeZone();
            var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!Guid.TryParse(userIdString, out var userId))
            {
                return Unauthorized();
            }

            var query = _context.Bookings
                .Include(b => b.Space)
                .Include(b => b.User)
                .Where(b => b.UserId == userId);

            List<Booking> myBookings;
            if (_context.Database.IsSqlite())
            {
                var bookingsFromDb = await query.ToListAsync();
                myBookings = bookingsFromDb.OrderBy(b => b.StartTime).ToList();
            }
            else
            {
                myBookings = await query.OrderBy(b => b.StartTime).ToListAsync();
            }

            var bookingDtos = myBookings.Select(b => new BookingDto
            {
                BookingId = b.BookingId,
                StartTime = TimeZoneInfo.ConvertTime(b.StartTime, kyivZone).DateTime,
                EndTime = TimeZoneInfo.ConvertTime(b.EndTime, kyivZone).DateTime,
                SpaceId = b.SpaceId,
                SpaceName = b.Space.Name,
                UserId = b.UserId,
                UserFullName = b.User.FullName
            });

            return Ok(bookingDtos);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<BookingDto>> GetBookingById(int id)
        {
            var kyivZone = GetKyivTimeZone();
            var bookingDto = await _context.Bookings
                .Include(b => b.Space)
                .Include(b => b.User)
                .Where(b => b.BookingId == id)
                .Select(b => new BookingDto
                {
                    BookingId = b.BookingId,
                    StartTime = TimeZoneInfo.ConvertTime(b.StartTime, kyivZone).DateTime,
                    EndTime = TimeZoneInfo.ConvertTime(b.EndTime, kyivZone).DateTime,
                    SpaceId = b.SpaceId,
                    SpaceName = b.Space.Name,
                    UserId = b.UserId,
                    UserFullName = b.User.FullName
                })
                .FirstOrDefaultAsync();

            if (bookingDto == null)
            {
                return NotFound();
            }

            return Ok(bookingDto);
        }

        [HttpPatch("{id}")]
        public async Task<IActionResult> UpdateBooking(int id, UpdateBookingDto dto)
        {
            var booking = await _context.Bookings.FindAsync(id);
            if (booking == null)
            {
                return NotFound(new { message = "Error: Booking not found." });
            }

            var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!Guid.TryParse(userIdString, out var userId) || booking.UserId != userId)
            {
                return Forbid();
            }
            var kyivZone = GetKyivTimeZone();

            DateTimeOffset newStartTimeOffset;
            if (dto.StartTime.HasValue)
            {
                newStartTimeOffset = new DateTimeOffset(dto.StartTime.Value, kyivZone.GetUtcOffset(dto.StartTime.Value));
            }
            else
            {
                newStartTimeOffset = booking.StartTime;
            }

            DateTimeOffset newEndTimeOffset;
            if (dto.EndTime.HasValue)
            {
                newEndTimeOffset = new DateTimeOffset(dto.EndTime.Value, kyivZone.GetUtcOffset(dto.EndTime.Value));
            }
            else
            {
                newEndTimeOffset = booking.EndTime;
            }

            var newSpaceId = dto.SpaceId ?? booking.SpaceId;

            var validationError = await ValidateBookingTime(newSpaceId, newStartTimeOffset, newEndTimeOffset, id);
            if (validationError != null)
            {
                return validationError;
            }

            booking.SpaceId = newSpaceId;
            booking.StartTime = newStartTimeOffset.ToUniversalTime();
            booking.EndTime = newEndTimeOffset.ToUniversalTime();

            _context.Entry(booking).State = EntityState.Modified;
            await _context.SaveChangesAsync();

            return NoContent();
        }
        
        
        
        [HttpDelete("{bookingId}")]
        public async Task<IActionResult> CancelBooking(int bookingId)
        {
            var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!Guid.TryParse(userIdString, out var userId))
            {
                return Unauthorized();
            }

            var booking = await _context.Bookings.FindAsync(bookingId);
            if (booking == null)
            {
                return NotFound(new { message = "Error: Booking not found." });
            }
            if (booking.UserId != userId)
            {
                return Forbid(); 
            }

            _context.Bookings.Remove(booking);
            await _context.SaveChangesAsync();
            return NoContent(); 
        }
    }
}