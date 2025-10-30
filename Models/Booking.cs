namespace WorkingSpaces.Models;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

[Index(nameof(SpaceId), nameof(StartTime), nameof(EndTime))]
[Index(nameof(UserId))]
public class Booking
{
    [Key]
    public int BookingId { get; set; }
    [Required]
    public int SpaceId { get; set; }

    [ForeignKey(nameof(SpaceId))] 
    public Space Space { get; set; } = null!;
    [Required]
    public Guid UserId { get; set; }

    [ForeignKey(nameof(UserId))]
    public User User { get; set; } = null!;
    [Required]
    public required DateTimeOffset StartTime { get; set; }
    [Required]
    public required DateTimeOffset EndTime { get; set; }
}