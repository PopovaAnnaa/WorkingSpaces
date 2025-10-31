using WorkingSpaces.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using WorkingSpaces.Data;
using Microsoft.EntityFrameworkCore;

[Authorize]
public class BookingController : Controller
{
    private readonly ApplicationDbContext _context;
    public BookingController(ApplicationDbContext context)
    {
        _context = context;
    }
        
    public IActionResult Index()
    {
        return View();
    }

    private async Task<string?> ValidateBookingTime(int spaceId, DateTimeOffset startOffset, DateTimeOffset endOffset)
    {
        if (!await _context.Spaces.AnyAsync(s => s.SpaceId == spaceId))
        {
            return "Error: Room not found.";
        }
        if (startOffset.Hour < 8 || endOffset.Hour > 23)
        {
            return "Error: Invalid time (8:00-23:00).";
        }
        if (startOffset < DateTimeOffset.Now)
        {
            return "Error: Invalid time (in the past tense).";
        }
        if (endOffset <= startOffset)
        {
            return "Error: Invalid time (end later than start).";
        }
        if ((endOffset - startOffset).TotalMinutes < 15)
        {
            return "Error: Reservation minimum 15 minutes.";
        }
        return null;
    }
    
    [HttpGet]
    public async Task<IActionResult> CheckAvailability()
    {
        ViewBag.Spaces = await _context.Spaces.OrderBy(s => s.Name).ToListAsync();
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CheckAvailability(int spaceId, DateTime selectedDate, TimeSpan startTime, TimeSpan endTime)
    {
        ViewBag.Spaces = await _context.Spaces.OrderBy(s => s.Name).ToListAsync();
        var startDateTime = selectedDate.Date.Add(startTime);
        var endDateTime = selectedDate.Date.Add(endTime);
        var startOffset = new DateTimeOffset(startDateTime, TimeZoneInfo.Local.GetUtcOffset(startDateTime));
        var endOffset = new DateTimeOffset(endDateTime, TimeZoneInfo.Local.GetUtcOffset(endDateTime));
        string? validationError = await ValidateBookingTime(spaceId, startOffset, endOffset);
        if (validationError != null)
        {
            ViewBag.Result = validationError;
            return View();
        }

        var startOffsetUtc = startOffset.ToUniversalTime();
        var endOffsetUtc = endOffset.ToUniversalTime();
        bool isOverlap;
        if (_context.Database.IsSqlite())
        {
            var bookingsForRoom = await _context.Bookings
            .Where(b => b.SpaceId == spaceId)
            .ToListAsync();
            isOverlap = bookingsForRoom.Any(b =>
            startOffsetUtc < b.EndTime &&
            endOffsetUtc > b.StartTime);
        }
        else
        {
            isOverlap = await _context.Bookings.AnyAsync(b =>
            b.SpaceId == spaceId &&
            startOffsetUtc < b.EndTime &&
            endOffsetUtc > b.StartTime);
        }
        if (isOverlap)
        {
            ViewBag.Result = "Error: This time is already taken.";
        }
        else
        {
            ViewBag.Result = "Success: This time is available!"; 
        }
        return View();
    }

    [HttpGet]
    public async Task<IActionResult> BookRoom()
    {
        ViewBag.Spaces = await _context.Spaces.OrderBy(s => s.Name).ToListAsync();
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> BookRoom(int spaceId, DateTime selectedDate, TimeSpan startTime, TimeSpan endTime)
    {
        ViewBag.Spaces = await _context.Spaces.OrderBy(s => s.Name).ToListAsync();
        
        var startDateTime = selectedDate.Date.Add(startTime);
        var endDateTime = selectedDate.Date.Add(endTime);
        var startOffset = new DateTimeOffset(startDateTime, TimeZoneInfo.Local.GetUtcOffset(startDateTime));
        var endOffset = new DateTimeOffset(endDateTime, TimeZoneInfo.Local.GetUtcOffset(endDateTime));

        string? validationError = await ValidateBookingTime(spaceId, startOffset, endOffset);
        if (validationError != null)
        {
            ViewBag.Result = validationError;
            return View();
        }

        var startOffsetUtc = startOffset.ToUniversalTime();
        var endOffsetUtc = endOffset.ToUniversalTime();

        bool isOverlap;
        if (_context.Database.IsSqlite())
        {
            var bookingsForRoom = await _context.Bookings
            .Where(b => b.SpaceId == spaceId)
            .ToListAsync();

            isOverlap = bookingsForRoom.Any(b =>
            startOffsetUtc < b.EndTime &&
            endOffsetUtc > b.StartTime);
        }
        else
        {
            isOverlap = await _context.Bookings.AnyAsync(b =>
            b.SpaceId == spaceId &&
            startOffsetUtc < b.EndTime &&
            endOffsetUtc > b.StartTime);
        }
        if (isOverlap)
        {
            ViewBag.Result = "Error: This time is already taken.";
            return View();
        }
            
        var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userIdString))
        {
            return Unauthorized(); 
        }
        var userId = Guid.Parse(userIdString);
        var user = await _context.Users.FindAsync(userId);
        var space = await _context.Spaces.FindAsync(spaceId);
        if (user == null || space == null)
        {
            ViewBag.Result = "Error: User or Space not found.";
            return View();
        }

        var newBooking = new Booking
        {
            SpaceId = spaceId,
            UserId = userId,
            StartTime = startOffsetUtc, 
            EndTime = endOffsetUtc,  
            User = user,
            Space = space
        };
        _context.Bookings.Add(newBooking);
        await _context.SaveChangesAsync();
        return RedirectToAction("ListBookings");
    }

    [HttpGet]
    public async Task<IActionResult> CancelBooking()
    {
        var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userIdString))
        {
            return Unauthorized(); 
        }
        var userId = Guid.Parse(userIdString);

        List<Booking> myBookings;
        var query = _context.Bookings
            .Include(b => b.Space)
            .Include(b => b.User)
            .Where(b => b.UserId == userId);
        if (_context.Database.IsSqlite())
        {
            var bookingsFromDb = await query.ToListAsync();
            myBookings = bookingsFromDb.OrderBy(b => b.StartTime).ToList();
        }
        else
        {
            myBookings = await query
            .OrderBy(b => b.StartTime)
            .ToListAsync();
        }
            return View(myBookings);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CancelBooking(int bookingId)
    {
        var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userIdString))
        {
            return Unauthorized();
        }
        var userId = Guid.Parse(userIdString);
        
        var booking = await _context.Bookings.FirstOrDefaultAsync(b => b.BookingId == bookingId);
        if (booking == null)
        {
            TempData["CancelResult"] = "Error: Booking not found.";
        }
        else if (booking.UserId != userId)
    {
        TempData["CancelResult"] = "Error: You cannot cancel someone else's booking.";
    }
        else
        {
            _context.Bookings.Remove(booking);
            await _context.SaveChangesAsync();
            TempData["CancelResult"] = "Success: Booking canceled.";
        }
        return RedirectToAction("CancelBooking");
    }

    [HttpGet]
    public async Task<IActionResult> ListBookings()
    {
        List<Booking> currentBookings;
        var query = _context.Bookings
            .Include(b => b.Space)
            .Include(b => b.User);
        if (_context.Database.IsSqlite())
        {
            var bookingsFromDb = await query.ToListAsync();
            currentBookings = bookingsFromDb.OrderBy(b => b.StartTime).ToList();
        }
        else
        {
            currentBookings = await query
                .OrderBy(b => b.StartTime)
                .ToListAsync();
        }
        return View(currentBookings);
    }

    public IActionResult Details(int id)
    {
        var booking = _context.Bookings
            .Where(b => b.BookingId == id)
            .Select(b => new Booking
            {
                BookingId = b.BookingId,
                StartTime = b.StartTime,
                EndTime = b.EndTime,
                Space = b.Space,
                User = b.User
            })
            .FirstOrDefault();

        if (booking == null)
        {
            return NotFound();
        }

        return View(booking);
    }
}