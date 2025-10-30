namespace WorkingSpaces.Models;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

public class Space
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.None)]
    public int SpaceId { get; set; }
    [Required]
    [StringLength(100, ErrorMessage = "The name is too long (maximum 100 characters)")]
    public string Name { get; set; } = string.Empty;
    [Range(1, int.MaxValue, ErrorMessage = "Number of seats must be at least 1.")]
    public int NumberOfSeats { get; set; }
    
    public Equipment AvailableEquipment { get; set; }

    public ICollection<Booking> Bookings { get; set; } = new List<Booking>();
}