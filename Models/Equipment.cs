namespace WorkingSpaces.Models;

[Flags] 
public enum Equipment
{
    None = 0,
    TV = 1,
    Projector = 2,
    Board = 4,
    Computers = 8,
}
