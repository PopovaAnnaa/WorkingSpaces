namespace WorkingSpaces.Models;

public class User
{
    public Guid UserId { get; set; }
    public required string Username { get; set; }
    public required string FullName { get; set; }
    public required string Password { get; set; } 
    public required string PhoneNumber { get; set; }
    public required string Email { get; set; }
}