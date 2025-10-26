namespace WorkingSpaces.Models;

public class Space
{
    public int SpaceId { get; set; }
    public string Name { get; set; } = string.Empty;
    public int NumberOfSeats { get; set; }
    public Equipment AvailableEquipment { get; set; }
}