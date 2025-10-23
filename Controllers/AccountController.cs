using Microsoft.AspNetCore.Mvc;
using System.Collections.Concurrent;
using WorkingSpaces.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;

namespace WorkingSpaces.Controllers
{
    public class AccountController : Controller
    {
        private static readonly ConcurrentDictionary<Guid, User> _inMemoryUsers =
            new ConcurrentDictionary<Guid, User>();

        // ==================== Локальна реєстрація ====================
        [HttpGet]
        public IActionResult Register()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Register(RegisterViewModel model)
        {
            if (_inMemoryUsers.Values.Any(u => u.Username.Equals(model.UserName, StringComparison.OrdinalIgnoreCase)))
            {
                ModelState.AddModelError(nameof(model.UserName), "This username is already taken.");
            }
            if (_inMemoryUsers.Values.Any(u => u.Email.Equals(model.Email, StringComparison.OrdinalIgnoreCase)))
            {
                ModelState.AddModelError(nameof(model.Email), "This email is already in use.");
            }

            if (ModelState.IsValid)
            {
                string passwordHash = BCrypt.Net.BCrypt.HashPassword(model.Password);
                var newUser = new User
                {
                    UserId = Guid.NewGuid(),
                    Username = model.UserName.ToLower(),
                    FullName = model.FullName,
                    Password = passwordHash,
                    PhoneNumber = model.PhoneNumber,
                    Email = model.Email.ToLower()
                };
                _inMemoryUsers.TryAdd(newUser.UserId, newUser);

                return RedirectToAction("Login");
            }
            return View(model);
        }

        // ==================== Локальний логін ====================
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

            var user = _inMemoryUsers.Values.FirstOrDefault(u =>
                u.Username.Equals(model.UserName, StringComparison.OrdinalIgnoreCase));

            if (user == null || !BCrypt.Net.BCrypt.Verify(model.Password, user.Password))
            {
                ModelState.AddModelError(string.Empty, "Invalid username or password.");
                return View(model);
            }

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, user.Username),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim("FullName", user.FullName)
            };

            var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);

            var authProperties = new AuthenticationProperties
            {
                IsPersistent = true
            };

            // Виправлено: додаємо null-forgiving оператори
            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                new ClaimsPrincipal(claimsIdentity!),
                authProperties!);

            return RedirectToAction("Index", "Home");
        }

        // ==================== Логін через зовнішнього провайдера (Okta) ====================
        [HttpGet]
        public IActionResult ExternalLogin(string provider, string? returnUrl = null)
        {
            var redirectUrl = Url.Action(nameof(ExternalLoginCallback), "Account", new { returnUrl })!;
            var properties = new AuthenticationProperties { RedirectUri = redirectUrl };
            return Challenge(properties, provider);
        }

        [HttpGet]
        public async Task<IActionResult> ExternalLoginCallback(string? returnUrl = null)
        {
            var result = await HttpContext.AuthenticateAsync(OpenIdConnectDefaults.AuthenticationScheme);
            if (result?.Principal == null)
                return RedirectToAction("Login");

            var claims = result.Principal.Claims.ToList();
            var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);

            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                new ClaimsPrincipal(claimsIdentity!));

            return RedirectToAction("Index", "Home");
        }

        // ==================== Вихід ====================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Login");
        }

        // ==================== Профіль ====================
        [Authorize]
        public IActionResult Profile()
        {
            return View();
        }
    }
}
