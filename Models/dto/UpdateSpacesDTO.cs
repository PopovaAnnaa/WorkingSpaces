using System.ComponentModel.DataAnnotations;
namespace WorkingSpaces.Models.Dto
{
    public class UpdateSpaceDto
    {
        
        [StringLength(100)]
        public string? Name { get; set; }

        [Range(1, int.MaxValue)]
        public int? NumberOfSeats { get; set; }
        
        public Equipment? AvailableEquipment { get; set; }
    }
}