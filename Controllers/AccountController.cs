using Microsoft.AspNetCore.Mvc;
using WorkingSpaces.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using WorkingSpaces.Data;
using Microsoft.EntityFrameworkCore;

namespace WorkingSpaces.Controllers
{
    public class AccountController : Controller
    {
        private readonly ApplicationDbContext _context;
        public AccountController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public IActionResult Register()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
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

            if (ModelState.IsValid)
            {
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
                    return RedirectToAction("Login");
                }
                catch (DbUpdateException) 
                {
                    ModelState.AddModelError(string.Empty, "This username or email is already taken.");
                    return View(model);
                }
            }
            return View(model);
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

            var userNameLower = model.UserName.ToLower();
            var user = await _context.Users.FirstOrDefaultAsync(u =>
                u.Username == userNameLower);

            if (user == null || !BCrypt.Net.BCrypt.Verify(model.Password, user.Password))
            {
                ModelState.AddModelError(string.Empty, "Invalid username or password.");
                return View(model);
            }

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.UserId.ToString()),
                new Claim(ClaimTypes.Name, user.Username),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim("FullName", user.FullName)
            };

            var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);

            var authProperties = new AuthenticationProperties
            {
                IsPersistent = true
            };

            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                new ClaimsPrincipal(claimsIdentity),
                authProperties);

            return RedirectToAction("Index", "Booking");
        }

        [HttpGet]
        public IActionResult ExternalLogin(string provider, string? returnUrl = null)
        {
            var redirectUrl = Url.Action(nameof(ExternalLoginCallback), "Account", new { returnUrl });
            var properties = new AuthenticationProperties { RedirectUri = redirectUrl };
            return Challenge(properties, provider); 
        }

        [HttpGet]
        public async Task<IActionResult> ExternalLoginCallback(string? returnUrl = null)
        {
            var result = await HttpContext.AuthenticateAsync("Okta");

            if (result?.Principal == null)
            {
                Console.WriteLine("ExternalLoginCallback failed: Principal is null");
                return RedirectToAction("Login");
            }

            var claimsIdentity = new ClaimsIdentity(result.Principal.Claims, CookieAuthenticationDefaults.AuthenticationScheme);

            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                new ClaimsPrincipal(claimsIdentity));

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
        public IActionResult Profile()
        {
            var username = User.FindFirstValue(ClaimTypes.Name);
            var email = User.FindFirstValue(ClaimTypes.Email);
            var fullName = User.FindFirstValue("FullName");

            var model = new ProfileViewModel
            {
                Username = username ?? "",
                Email = email ?? "",
                FullName = fullName ?? "",
            };

            return View(model);
        }
    }
}
