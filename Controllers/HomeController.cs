using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using WorkingSpaces.Models;

namespace WorkingSpaces.Controllers
{
    public class HomeController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}