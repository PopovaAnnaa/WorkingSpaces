using System.ComponentModel.DataAnnotations;
namespace WorkingSpaces.Models.Dto
{
    public class CreateSpaceDto
    {
        [Required]
        [StringLength(100)]
        public string Name { get; set; } = string.Empty;

        [Required]
        [Range(1, int.MaxValue)]
        public int NumberOfSeats { get; set; }
        
        public Equipment AvailableEquipment { get; set; }
    }
}