using System.ComponentModel.DataAnnotations;

namespace WorkingSpaces.Models.Dto
{
    public class CreateBookingDto
    {
        [Required]
        public int SpaceId { get; set; }

        [Required]
        public DateTime StartTime { get; set; }

        [Required]
        public DateTime EndTime { get; set; }
    }
}