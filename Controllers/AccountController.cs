using Microsoft.AspNetCore.Mvc;

namespace CoworkingBooking.Controllers
{
    public class AccountController : Controller
    {
        // Сторінка реєстрації
        public IActionResult Register()
        {
            return View();
        }

        // Сторінка входу
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
