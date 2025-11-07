using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WorkingSpaces.Data;
using WorkingSpaces.Models;

public class SpacesController : Controller
{
    private readonly ApplicationDbContext _context;

    public SpacesController(ApplicationDbContext context)
    {
        _context = context;
    }

    public IActionResult Index()
    {
        var spaces = _context.Spaces
            .ToList();

        return View(spaces);
    }

    public IActionResult Details(int id)
    {
        var space = _context.Spaces
            .FirstOrDefault(s => s.SpaceId == id);

        if (space == null)
            return NotFound();

        return View(space);
    }
}
