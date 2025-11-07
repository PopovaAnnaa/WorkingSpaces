namespace WorkingSpaces.Models.Dto
{
    public class BookingDto
    {
        public int BookingId { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public int SpaceId { get; set; }
        public string SpaceName { get; set; } = string.Empty;
        public Guid UserId { get; set; }
        public string UserFullName { get; set; } = string.Empty;
    }
}