using System.ComponentModel.DataAnnotations;
namespace WorkingSpaces.Models;

public class CreateBookingRequest
{
    [Required]
    public int SpaceId { get; set; }
    [Required]
    [EmailAddress]
    public string UserEmail { get; set; } = string.Empty;
    [Required]
    public string UserFullName { get; set; } = string.Empty;
    [Required]
    public DateTimeOffset StartTime { get; set; }
    [Required]
    public DateTimeOffset EndTime { get; set; }
}