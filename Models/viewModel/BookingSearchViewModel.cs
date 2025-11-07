namespace WorkspaceApp.Models
{
    public class BookingSearchViewModel
    {
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }

        public string? Username { get; set; }
        public string? SpaceName { get; set; }

        public List<BookingResult>? Results { get; set; }
    }

    public class BookingResult
    {
        public int BookingId { get; set; }
        public string? UserName { get; set; }
        public string? SpaceName { get; set; }
        public DateTimeOffset StartTime { get; set; } 
        public DateTimeOffset EndTime { get; set; }
    }
}
