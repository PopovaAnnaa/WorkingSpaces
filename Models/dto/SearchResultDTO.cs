namespace WorkingSpaces.Models.Dto
{
    public class SearchResultDto
    {
        public int BookingId { get; set; }
        public string? UserName { get; set; }
        public string? SpaceName { get; set; }
        public DateTime StartTime { get; set; } 
        public DateTime EndTime { get; set; }
    }
}