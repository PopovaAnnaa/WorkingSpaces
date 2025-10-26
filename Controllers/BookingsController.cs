using WorkingSpaces.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

[Authorize]
public class BookingController : Controller
{
    private static readonly List<Space> _spaces = new List<Space>();
    private static readonly List<Booking> _bookings = new List<Booking>();

    static BookingController()
    {
        _spaces.AddRange(new List<Space>
            {
                new Space { SpaceId = 1, Name = "Кімната для нарад (12)", NumberOfSeats = 12, AvailableEquipment = Equipment.TV | Equipment.Board },
                new Space { SpaceId = 2, Name = "Конференц-зал (10)", NumberOfSeats = 10, AvailableEquipment = Equipment.Projector | Equipment.Computers },
                new Space { SpaceId = 3, Name = "Конфіденційна кімната (5)", NumberOfSeats = 5, AvailableEquipment = Equipment.Board },
            });
    }
        
    public IActionResult Index()
    {
        return View();
    }

    private string? ValidateBookingTime(int spaceId, DateTimeOffset startOffset, DateTimeOffset endOffset)
    {
        if (_spaces.All(s => s.SpaceId != spaceId))
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
    public IActionResult CheckAvailability()
    {
        ViewBag.Spaces = _spaces;
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult CheckAvailability(int spaceId, DateTime selectedDate, TimeSpan startTime, TimeSpan endTime)
    {
        var startDateTime = selectedDate.Date.Add(startTime);
        var endDateTime = selectedDate.Date.Add(endTime);
        var startOffset = new DateTimeOffset(startDateTime, TimeZoneInfo.Local.GetUtcOffset(startDateTime));
        var endOffset = new DateTimeOffset(endDateTime, TimeZoneInfo.Local.GetUtcOffset(endDateTime));
        string? validationError = ValidateBookingTime(spaceId, startOffset, endOffset);
        if (validationError != null)
        {
            ViewBag.Result = validationError;
            ViewBag.Spaces = _spaces;
            return View();
        }
        bool isOverlap;
        isOverlap = _bookings.Any(b =>
                b.Space.SpaceId == spaceId &&
                startOffset < b.EndTime &&
                endOffset > b.StartTime);
        if (isOverlap)
        {
            ViewBag.Result = "Error: This time is already taken.";
        }
        else
        {
            ViewBag.Result = "Success: This time is available!"; 
        }
        ViewBag.Spaces = _spaces;
        return View();
    }

    [HttpGet]
    public IActionResult BookRoom()
    {
        ViewBag.Spaces = _spaces;
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult BookRoom(int spaceId, DateTime selectedDate, TimeSpan startTime, TimeSpan endTime)
    {
        var startDateTime = selectedDate.Date.Add(startTime);
        var endDateTime = selectedDate.Date.Add(endTime);
        var startOffset = new DateTimeOffset(startDateTime, TimeZoneInfo.Local.GetUtcOffset(startDateTime));
        var endOffset = new DateTimeOffset(endDateTime, TimeZoneInfo.Local.GetUtcOffset(endDateTime));
        
        var space = _spaces.FirstOrDefault(s => s.SpaceId == spaceId);

        string? validationError = ValidateBookingTime(spaceId, startOffset, endOffset);
        if (validationError != null)
        {
            ViewBag.Result = validationError;
            ViewBag.Spaces = _spaces;
            return View();
        }

        var userEmail = User.FindFirstValue(ClaimTypes.Email);
        var fullName = User.FindFirstValue("FullName");

        if (userEmail == null || fullName == null)
        {
            return Unauthorized();
        }

        bool isOverlap = _bookings.Any(b =>
            b.Space.SpaceId == spaceId &&
            startOffset < b.EndTime &&
            endOffset > b.StartTime);

        if (isOverlap)
            {
                ViewBag.Result = "Error: This time is already taken.";
                ViewBag.Spaces = _spaces;
                return View();
            }
        var newBooking = new Booking
        {
            BookingId = (_bookings.Any() ? _bookings.Max(b => b.BookingId) : 0) + 1,
            Space = space!,
            UserEmail = userEmail!,
            UserFullName = fullName!,
            StartTime = startOffset,
            EndTime = endOffset
        };
        _bookings.Add(newBooking);
        ViewBag.Spaces = _spaces;
        return RedirectToAction("ListBookings");
    }

    [HttpGet]
    public IActionResult CancelBooking()
    {
        return View();
    }

    [HttpPost]
    // public IActionResult CancelBooking()
    // {
    //     return View();
    // }
    [HttpGet]
    public IActionResult ListBookings()
    {
        List<Booking> currentBookings;
        currentBookings = _bookings.OrderBy(b => b.StartTime).ToList();
        return View(currentBookings);
    }
}