using Microsoft.AspNetCore.Mvc;
using WorkingSpaces.Data;

public class UsersController : Controller
{
    private readonly ApplicationDbContext _context;

    public UsersController(ApplicationDbContext context)
    {
        _context = context;
    }

    public IActionResult Index()
    {
        var users = _context.Users.ToList();
        return View(users);
    }

    public IActionResult Details(Guid id)
    {
        var user = _context.Users.FirstOrDefault(u => u.UserId == id);
        if (user == null)
            return NotFound();

        return View(user);
    }
}
