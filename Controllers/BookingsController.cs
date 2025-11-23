using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using WorkingSpaces.Models.Dto;
using System.Security.Claims;
using WorkingSpaces.Models;

namespace WorkingSpaces.Controllers
{
    [Authorize]
    public class BookingController : Controller
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly string _apiBaseUrl;

        public BookingController(IHttpClientFactory httpClientFactory, IConfiguration configuration)
        {
            _httpClientFactory = httpClientFactory;
            _apiBaseUrl = configuration.GetValue<string>("ApiBaseUrl")
                          ?? throw new ArgumentNullException("ApiBaseUrl not found in appsettings.json");
        }

        public IActionResult Index()
        {
            return View(); 
        }

        public async Task<IActionResult> ListBookings()
        {
            var token = User.FindFirstValue("jwt_token");
            if (string.IsNullOrEmpty(token))
                return RedirectToAction("Login", "Account");

            var client = _httpClientFactory.CreateClient();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var response = await client.GetAsync($"{_apiBaseUrl}/api/bookings");

            if (!response.IsSuccessStatusCode)
            {
                TempData["Error"] = "Failed to load booking list.";
                return View(new List<BookingDto>());
            }

            var bookings = await response.Content.ReadFromJsonAsync<List<BookingDto>>() ?? new List<BookingDto>();
            return View(bookings);
        }

        public async Task<IActionResult> MyBookings()
        {
            var token = User.FindFirstValue("jwt_token");
            if (string.IsNullOrEmpty(token))
                return RedirectToAction("Login", "Account");

            var client = _httpClientFactory.CreateClient();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var response = await client.GetAsync($"{_apiBaseUrl}/api/bookings/my-bookings");

            if (!response.IsSuccessStatusCode)
            {
                TempData["Error"] = "Failed to load your bookings.";
                return View(new List<BookingDto>());
            }

            var myBookings = await response.Content.ReadFromJsonAsync<List<BookingDto>>() ?? new List<BookingDto>();
            return View(myBookings);
        }

        public async Task<IActionResult> Details(int id)
        {
            var token = User.FindFirstValue("jwt_token");
            var client = _httpClientFactory.CreateClient();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var response = await client.GetAsync($"{_apiBaseUrl}/api/bookings/{id}");

            if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                return NotFound();

            if (!response.IsSuccessStatusCode)
            {
                TempData["Error"] = "Error loading booking data.";
                return RedirectToAction("Index");
            }

            var booking = await response.Content.ReadFromJsonAsync<BookingDto>();
            return View(booking);
        }

        [HttpGet]
        public async Task<IActionResult> BookRoom()
        {
            var httpClient = _httpClientFactory.CreateClient();
            httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", User.FindFirstValue("jwt_token"));

            var responseSpaces = await httpClient.GetAsync($"{_apiBaseUrl}/api/spaces");
            var json = await responseSpaces.Content.ReadAsStringAsync();
            Console.WriteLine(json);

            var spaces = await responseSpaces.Content.ReadFromJsonAsync<List<SpaceDto>>() ?? new List<SpaceDto>();

            ViewBag.Spaces = spaces;

            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> BookRoom(CreateBookingDto model)
        {
            if (!ModelState.IsValid)
            {
                var token = User.FindFirstValue("jwt_token");
                var client = _httpClientFactory.CreateClient();
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
                var responseSpaces = await client.GetAsync($"{_apiBaseUrl}/api/spaces");
                ViewBag.Spaces = await responseSpaces.Content.ReadFromJsonAsync<List<SpaceDto>>() ?? new List<SpaceDto>();

                return View(model);
            }

            var httpClient = _httpClientFactory.CreateClient();
            httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", User.FindFirstValue("jwt_token"));

            var responseBooking = await httpClient.PostAsJsonAsync($"{_apiBaseUrl}/api/bookings", model);

            if (responseBooking.IsSuccessStatusCode)
                return RedirectToAction("MyBookings");

            var error = await responseBooking.Content.ReadFromJsonAsync<Dictionary<string, string>>();
            if (error != null && error.ContainsKey("message"))
                ModelState.AddModelError(string.Empty, error["message"]);
            else
                ModelState.AddModelError(string.Empty, "Error while creating the booking.");

            var responseSpaces2 = await httpClient.GetAsync($"{_apiBaseUrl}/api/spaces");
            ViewBag.Spaces = await responseSpaces2.Content.ReadFromJsonAsync<List<SpaceDto>>() ?? new List<SpaceDto>();

            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var token = User.FindFirstValue("jwt_token");
            var client = _httpClientFactory.CreateClient();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var response = await client.GetAsync($"{_apiBaseUrl}/api/bookings/{id}");
            if (!response.IsSuccessStatusCode)
                return RedirectToAction("MyBookings");

            var booking = await response.Content.ReadFromJsonAsync<BookingDto>();

            if (booking == null)
            {
                TempData["Error"] = "Failed to load booking data.";
                return RedirectToAction("MyBookings");
            }

            var updateDto = new UpdateBookingDto
            {
                SpaceId = booking.SpaceId,
                StartTime = booking.StartTime,
                EndTime = booking.EndTime
            };

            return View(updateDto);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, UpdateBookingDto model)
        {
            var token = User.FindFirstValue("jwt_token");
            var client = _httpClientFactory.CreateClient();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var response = await client.PatchAsJsonAsync($"{_apiBaseUrl}/api/bookings/{id}", model);

            if (response.IsSuccessStatusCode)
                return RedirectToAction("MyBookings");

            var error = await response.Content.ReadFromJsonAsync<Dictionary<string, string>>();
            if (error != null && error.ContainsKey("message"))
                ModelState.AddModelError(string.Empty, error["message"]);
            else
                ModelState.AddModelError(string.Empty, "Error while updating the booking.");

            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> CancelBooking()
        {
            var token = User.FindFirstValue("jwt_token");
            if (string.IsNullOrEmpty(token))
                return RedirectToAction("Login", "Account");

            var client = _httpClientFactory.CreateClient();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var response = await client.GetAsync($"{_apiBaseUrl}/api/bookings/my-bookings");
            var bookings = new List<BookingDto>();
            if (response.IsSuccessStatusCode)
            {
                bookings = await response.Content.ReadFromJsonAsync<List<BookingDto>>() ?? new List<BookingDto>();
            }

            return View(bookings);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CancelBooking(int bookingId)
        {
            var token = User.FindFirstValue("jwt_token");
            var client = _httpClientFactory.CreateClient();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var response = await client.DeleteAsync($"{_apiBaseUrl}/api/bookings/{bookingId}");

            if (!response.IsSuccessStatusCode)
            {
                TempData["Error"] = "Failed to delete booking.";
            }

            return RedirectToAction("MyBookings");
        }
    }
}
