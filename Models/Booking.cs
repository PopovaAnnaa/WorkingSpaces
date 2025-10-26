namespace WorkingSpaces.Models;

public class Booking
{
    public int BookingId { get; set; }
    public required Space Space { get; set; }
    public required string UserEmail { get; set; }
    public required string UserFullName { get; set; }
    public required DateTimeOffset StartTime { get; set; }
    public required DateTimeOffset EndTime { get; set; }
}