namespace WorkingSpaces.Models.Dto
{
    public class BookingSearchDto
    {
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public string? Username { get; set; }
        public string? SpaceName { get; set; }
    }
}