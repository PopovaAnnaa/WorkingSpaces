namespace WorkingSpaces.Models.Dto
{
    public class UpdateBookingDto
    {
        public int? SpaceId { get; set; }

        public DateTime? StartTime { get; set; }

        public DateTime? EndTime { get; set; }
    }
}