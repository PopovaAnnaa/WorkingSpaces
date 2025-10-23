using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;

// [Authorize]
public class BookingController : Controller
{
    // Імітація бази даних бронювань у пам'яті
    private static readonly List<Booking> _bookings = new List<Booking>();

    // Головна сторінка /Booking
    public IActionResult Index()
    {
        return View();
    }

    // ================= CheckAvailability =================
    [HttpGet]
    public IActionResult CheckAvailability()
    {
        return View();
    }

    [HttpPost]
    public IActionResult CheckAvailability(DateTime date, string room)
    {
        bool isAvailable = !_bookings.Any(b => b.Room == room && b.Date == date);
        ViewBag.Result = isAvailable ? "Room is available" : "Room is already booked";
        return View();
    }

    // ================= BookRoom =================
    [HttpGet]
    public IActionResult BookRoom()
    {
        return View();
    }

    [HttpPost]
    public IActionResult BookRoom(DateTime date, string room, string username)
    {
        if (_bookings.Any(b => b.Room == room && b.Date == date))
        {
            ViewBag.Result = "Cannot book. Room already occupied.";
        }
        else
        {
            _bookings.Add(new Booking
            {
                Room = room,
                Date = date,
                Username = username
            });
            ViewBag.Result = "Booking successful!";
        }
        return View();
    }

    // ================= CancelBooking =================
    [HttpGet]
    public IActionResult CancelBooking()
    {
        return View();
    }

    [HttpPost]
    public IActionResult CancelBooking(DateTime date, string room, string username)
    {
        var booking = _bookings.FirstOrDefault(b => b.Room == room && b.Date == date && b.Username == username);
        if (booking != null)
        {
            _bookings.Remove(booking);
            ViewBag.Result = "Booking canceled successfully.";
        }
        else
        {
            ViewBag.Result = "No matching booking found.";
        }
        return View();
    }
}

// Модель бронювання
public class Booking
{
    public string Room { get; set; } = string.Empty;
    public DateTime Date { get; set; }
    public string Username { get; set; } = string.Empty;
}
