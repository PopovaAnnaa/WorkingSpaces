using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WorkingSpaces.Data;
using WorkingSpaces.Models;
using WorkingSpaces.Models.Dto;
using Microsoft.AspNetCore.Authorization; 
using Microsoft.AspNetCore.Authentication.JwtBearer;

namespace WorkingSpaces.Controllers.Api
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    public class SpacesController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public SpacesController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<SpaceDto>>> GetSpaces()
        {
            var spaces = await _context.Spaces
                .Select(s => new SpaceDto
                {
                    SpaceId = s.SpaceId,
                    Name = s.Name,
                    NumberOfSeats = s.NumberOfSeats,
                    AvailableEquipment = s.AvailableEquipment.ToString()
                })
                .ToListAsync();

            return Ok(spaces);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<SpaceDto>> GetSpaceById(int id)
        {
            var spaceDto = await _context.Spaces
                .Where(s => s.SpaceId == id)
                .Select(s => new SpaceDto
                {
                    SpaceId = s.SpaceId,
                    Name = s.Name,
                    NumberOfSeats = s.NumberOfSeats,
                    AvailableEquipment = s.AvailableEquipment.ToString() 
                })
                .FirstOrDefaultAsync();

            if (spaceDto == null)
            {
                return NotFound();
            }

            return Ok(spaceDto);
        }

        [HttpPost]
        public async Task<ActionResult<SpaceDto>> CreateSpace([FromBody] CreateSpaceDto createDto)
        {
            var space = new Space
            {
                Name = createDto.Name,
                NumberOfSeats = createDto.NumberOfSeats,
                AvailableEquipment = createDto.AvailableEquipment ?? Equipment.None
            };

            _context.Spaces.Add(space);
            await _context.SaveChangesAsync();

            var spaceDto = new SpaceDto
            {
                SpaceId = space.SpaceId,
                Name = space.Name,
                NumberOfSeats = space.NumberOfSeats,
                AvailableEquipment = space.AvailableEquipment.ToString() 
            };

            return CreatedAtAction(nameof(GetSpaceById), new { id = space.SpaceId }, spaceDto);
        }

        [HttpPatch("{id}")]
        public async Task<IActionResult> UpdateSpace(int id, UpdateSpaceDto updateDto)
        {
            var space = await _context.Spaces.FindAsync(id);
            if (space == null) return NotFound();

            if (!string.IsNullOrWhiteSpace(updateDto.Name))
                space.Name = updateDto.Name;

            if (updateDto.NumberOfSeats.HasValue)
                space.NumberOfSeats = updateDto.NumberOfSeats.Value;

            if (updateDto.AvailableEquipment.HasValue)
                space.AvailableEquipment = updateDto.AvailableEquipment.Value;

            _context.Entry(space).State = EntityState.Modified;
            await _context.SaveChangesAsync();
            return NoContent();
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteSpace(int id)
        {
            var space = await _context.Spaces.FindAsync(id);
            if (space == null)
            {
                return NotFound();
            }

            _context.Spaces.Remove(space);
            await _context.SaveChangesAsync();

            return NoContent();
        }
    }
}
