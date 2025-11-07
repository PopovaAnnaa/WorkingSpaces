using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WorkingSpaces.Data;
using WorkingSpaces.Models.Dto; 
using Microsoft.AspNetCore.Authorization; 
using Microsoft.AspNetCore.Authentication.JwtBearer; 

namespace WorkingSpaces.Controllers.Api
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    public class SearchController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public SearchController(ApplicationDbContext context)
        {
            _context = context;
        }
        private TimeZoneInfo GetKyivTimeZone()
        {
            try { return TimeZoneInfo.FindSystemTimeZoneById("Europe/Kyiv"); }
            catch (TimeZoneNotFoundException) { return TimeZoneInfo.FindSystemTimeZoneById("E. Europe Standard Time"); }
        }

        [HttpPost]
        public async Task<ActionResult<IEnumerable<SearchResultDto>>> SearchBookings([FromBody] BookingSearchDto model)
        {
            var kyivZone = GetKyivTimeZone();
        
            var query = from b in _context.Bookings
                        join u in _context.Users on b.UserId equals u.UserId
                        join s in _context.Spaces on b.SpaceId equals s.SpaceId
                        select new
                        {
                            b.BookingId,
                            UserName = u.Username,
                            SpaceName = s.Name,
                            b.StartTime,
                            b.EndTime    
                        };
            var allBookingsInMemory = await query.ToListAsync();

            var list = allBookingsInMemory.Select(x => new SearchResultDto
                 {
                     BookingId = x.BookingId,
                     UserName = x.UserName,
                     SpaceName = x.SpaceName,
                     StartTime = TimeZoneInfo.ConvertTime(x.StartTime, kyivZone).DateTime,
                     EndTime = TimeZoneInfo.ConvertTime(x.EndTime, kyivZone).DateTime
                 });

            if (model.StartDate.HasValue)
            {
                list = list.Where(x => x.StartTime >= model.StartDate.Value);
            }

            if (model.EndDate.HasValue)
            {
                list = list.Where(x => x.EndTime <= model.EndDate.Value);
            }

            if (!string.IsNullOrWhiteSpace(model.Username))
            {
                list = list.Where(x => x.UserName != null && x.UserName.ToLower().Contains(model.Username.ToLower()));
            }

            if (!string.IsNullOrWhiteSpace(model.SpaceName))
            {
                list = list.Where(x => x.SpaceName != null && x.SpaceName.ToLower().Contains(model.SpaceName.ToLower()));
            }
            var resultDtos = list.ToList();

            return Ok(resultDtos);
        }
    }
}