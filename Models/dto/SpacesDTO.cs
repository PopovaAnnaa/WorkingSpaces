using WorkingSpaces.Models;

namespace WorkingSpaces.Models.Dto
{
    public class SpaceDto
{
    public int SpaceId { get; set; }
    public string Name { get; set; } = string.Empty;
    public int NumberOfSeats { get; set; }

    // хранится как строка для JSON
    public string AvailableEquipment { get; set; } = string.Empty;

    // дополнительное свойство для удобной работы в C#
    public List<Equipment> EquipmentList => AvailableEquipment
        .Split(',', StringSplitOptions.RemoveEmptyEntries)
        .Select(e => Enum.TryParse<Equipment>(e.Trim(), out var value) ? value : Equipment.None)
        .ToList();
}

}
