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

    [HttpGet]
    public IActionResult CheckAvailability()
    {
        return View();
    }

    // [HttpPost]
    // public IActionResult CheckAvailability()
    // {
    //     return View();
    // }

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
        var space = _spaces.FirstOrDefault(s => s.SpaceId == spaceId);
        if (space == null)
        {
            ViewBag.Result = "Error: Room not found.";
            ViewBag.Spaces = _spaces;
            return View();
        }
        var startDateTime = selectedDate.Date.Add(startTime);
        var endDateTime = selectedDate.Date.Add(endTime);
        var startOffset = new DateTimeOffset(startDateTime, TimeZoneInfo.Local.GetUtcOffset(startDateTime));
        var endOffset = new DateTimeOffset(endDateTime, TimeZoneInfo.Local.GetUtcOffset(endDateTime));
        if (startOffset.Hour < 8 || endOffset.Hour > 23)
        {
            ViewBag.Result = "Error: Invalid time (8:00-23:00).";
            ViewBag.Spaces = _spaces;
            return View();
        }
        if (startOffset < DateTimeOffset.Now)
        {
            ViewBag.Result = "Error: Invalid time (in the past tense).";
            ViewBag.Spaces = _spaces;
            return View();
        }
        if (endOffset <= startOffset)
        {
            ViewBag.Result = "Error: Invalid time (end later than start).";

        }
        if ((endOffset - startOffset).TotalMinutes < 15)
        {
            ViewBag.Result = "Error: Reservation minimum 15 minutes.";
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
            Space = space,
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