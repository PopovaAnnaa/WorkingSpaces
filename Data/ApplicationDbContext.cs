using Microsoft.EntityFrameworkCore;
using WorkingSpaces.Models;

namespace WorkingSpaces.Data;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }
    public DbSet<User> Users { get; set; }
    public DbSet<Space> Spaces { get; set; }
    public DbSet<Booking> Bookings { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.Entity<Space>().HasData(
            new Space
            {
                SpaceId = 1,
                Name = "Meeting room (12)",
                NumberOfSeats = 12,
                AvailableEquipment = Equipment.TV | Equipment.Board
            },
            new Space
            {
                SpaceId = 2,
                Name = "Conference hall (10)",
                NumberOfSeats = 10,
                AvailableEquipment = Equipment.Projector | Equipment.Computers
            },
            new Space
            {
                SpaceId = 3,
                Name = "Confidential room (5)",
                NumberOfSeats = 5,
                AvailableEquipment = Equipment.Board
            }
        );
    }
}