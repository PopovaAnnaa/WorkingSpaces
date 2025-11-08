using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WorkingSpaces.Data;
using WorkspaceApp.Models;

namespace WorkingSpaces.Controllers
{
    public class SearchController : Controller
    {
        private readonly ApplicationDbContext _context;

        public SearchController(ApplicationDbContext context)
        {
            _context = context;
        }

        public IActionResult Index()
        {
            return View(new BookingSearchViewModel());
        }

        [HttpPost]
        public IActionResult Index(BookingSearchViewModel model)
        {
            var query = from b in _context.Bookings
                        join u in _context.Users on b.UserId equals u.UserId
                        join s in _context.Spaces on b.SpaceId equals s.SpaceId
                        select new
                        {
                            b.BookingId,
                            u.Username,
                            s.Name,
                            b.StartTime,
                            b.EndTime
                        };

            var list = query.AsEnumerable()
                            .Select(x => new BookingResult
                            {
                                BookingId = x.BookingId,
                                UserName = x.Username,
                                SpaceName = x.Name,
                                StartTime = x.StartTime,
                                EndTime = x.EndTime
                            });

            if (model.StartDate.HasValue)
                list = list.Where(x => x.StartTime >= model.StartDate.Value);

            if (model.EndDate.HasValue)
                list = list.Where(x => x.EndTime <= model.EndDate.Value);

            if (!string.IsNullOrWhiteSpace(model.Username))
                list = list.Where(x => x.UserName!.Contains(model.Username));

            if (!string.IsNullOrWhiteSpace(model.SpaceName))
                list = list.Where(x => x.SpaceName!.Contains(model.SpaceName));

            model.Results = list.ToList();

            return View(model);
        }
    }
}
