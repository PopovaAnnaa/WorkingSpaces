namespace WorkingSpaces.Models.Dto
{
    public class UserDtoV2
    {
        public Guid UserId { get; set; }
        public string Username { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string? PhoneNumber { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;

        public ICollection<BookingDto> Bookings { get; set; } = new List<BookingDto>();
    }
}