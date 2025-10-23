using Microsoft.AspNetCore.Mvc;
using System.Collections.Concurrent;
using WorkingSpaces.Models;

namespace WorkingSpaces.Controllers
{
    public class AccountController : Controller
    {
        private static readonly ConcurrentDictionary<Guid, User> _inMemoryUsers =
            new ConcurrentDictionary<Guid, User>();
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

        [HttpGet]
        public IActionResult Login()
        {
            return View();
        }

        // Профіль користувача
        public IActionResult Profile()
        {
            return View();
        }
    }
}
