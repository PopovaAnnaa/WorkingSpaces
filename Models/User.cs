namespace WorkingSpaces.Models;
using System.ComponentModel.DataAnnotations;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using Microsoft.EntityFrameworkCore;

public class User
{
    [Key]
    public Guid UserId { get; set; }
    [Required]
    [StringLength(50)]
    public required string Username { get; set; }
    [Required]
    [StringLength(500)]
    public required string FullName { get; set; }
    [Required]
    [StringLength(100)]
    public required string Password { get; set; }
    [Required]
    [Phone]
    [StringLength(14)]
    public required string PhoneNumber { get; set; }
    [Required]
    [EmailAddress]
    [StringLength(256)]
    public required string Email { get; set; }

    public ICollection<Booking> Bookings { get; set; } = new List<Booking>();
}