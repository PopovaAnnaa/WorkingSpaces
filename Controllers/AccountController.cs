using Microsoft.AspNetCore.Mvc;
using WorkingSpaces.Models;
using WorkingSpaces.Models.Dto;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using WorkingSpaces.Data;
using Microsoft.EntityFrameworkCore;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Security.Claims;

namespace WorkingSpaces.Controllers
{
    public class AccountController : Controller
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly string _apiBaseUrl;
        public AccountController(IHttpClientFactory httpClientFactory, IConfiguration configuration)
        {
            _httpClientFactory = httpClientFactory;
            _apiBaseUrl = configuration.GetValue<string>("ApiBaseUrl")
                          ?? throw new ArgumentNullException("There is no ApiBaseUrl in appsettings.json");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var client = _httpClientFactory.CreateClient();

            var response = await client.PostAsJsonAsync($"{_apiBaseUrl}/api/accountapi/register", model);

            if (response.IsSuccessStatusCode)
            {
                return RedirectToAction("Login");
            }
            else
            {
                try
                {
                    var errorData = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>();
                    if (errorData != null && errorData.Errors.Any())
                    {
                        foreach (var error in errorData.Errors)
                        {
                            foreach (var message in error.Value)
                            {
                                ModelState.AddModelError(error.Key, message);
                            }
                        }
                    }
                    else
                    {
                        ModelState.AddModelError(string.Empty, "An error occurred while registering.");
                    }
                }
                catch
                {
                    ModelState.AddModelError(string.Empty, "An unexpected API error occurred.");
                }
                return View(model);
            }
        }

        [HttpGet]
        public IActionResult Login()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var client = _httpClientFactory.CreateClient();

            var loginResponse = await client.PostAsJsonAsync($"{_apiBaseUrl}/api/accountapi/login", model);

            if (!loginResponse.IsSuccessStatusCode)
            {
                ModelState.AddModelError(string.Empty, "Incorrect username or password.");
                return View(model);
            }

            var loginResult = await loginResponse.Content.ReadFromJsonAsync<LoginApiResponse>();
            if (loginResult == null || string.IsNullOrEmpty(loginResult.Token))
            {
                ModelState.AddModelError(string.Empty, "Failed to get token from API.");
                return View(model);
            }

            var token = loginResult.Token;

            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var profileResponse = await client.GetAsync($"{_apiBaseUrl}/api/accountapi/profile");

            if (!profileResponse.IsSuccessStatusCode)
            {
                ModelState.AddModelError(string.Empty, "Login successful, but unable to retrieve profile.");
                return View(model);
            }

            var userProfile = await profileResponse.Content.ReadFromJsonAsync<UserDto>();
            if (userProfile == null)
            {
                ModelState.AddModelError(string.Empty, "Failed to read profile data.");
                return View(model);
            }

            var claims = new List<Claim>
            {
                new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.NameIdentifier, userProfile.UserId.ToString()),
                new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.Name, userProfile.Username),
                new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.Email, userProfile.Email),
                new System.Security.Claims.Claim("FullName", userProfile.FullName),
                
                new System.Security.Claims.Claim("jwt_token", token)
            };

            var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            var authProperties = new AuthenticationProperties { IsPersistent = true };

            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                new System.Security.Claims.ClaimsPrincipal(claimsIdentity),
                authProperties);

            return RedirectToAction("Index", "Booking");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Login");
        }

        [Authorize] 
        public async Task<IActionResult> Profile()
        {
            var token = User.FindFirstValue("jwt_token");

            if (string.IsNullOrEmpty(token))
            {
                await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                return RedirectToAction("Login");
            }

            var client = _httpClientFactory.CreateClient();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var response = await client.GetAsync($"{_apiBaseUrl}/api/accountapi/profile");

            if (response.IsSuccessStatusCode)
            {
                var userDto = await response.Content.ReadFromJsonAsync<UserDto>();
                if (userDto == null)
                {
                    return RedirectToAction("Login");
                }
                var model = new ProfileViewModel
                {
                    Username = userDto.Username,
                    Email = userDto.Email,
                    FullName = userDto.FullName,
                };

                return View(model);
            }
            else if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
            {
                await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                return RedirectToAction("Login");
            }
            else
            {
                TempData["Error"] = "Failed to load profile from API.";
                return RedirectToAction("Index", "Booking");
            }
        }
    }

    public class LoginApiResponse
    {
        [System.Text.Json.Serialization.JsonPropertyName("token")]
        public string Token { get; set; } = string.Empty;

        [System.Text.Json.Serialization.JsonPropertyName("message")]
        public string Message { get; set; } = string.Empty;
    }
}