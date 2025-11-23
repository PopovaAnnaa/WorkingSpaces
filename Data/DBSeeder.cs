using Bogus;
using WorkingSpaces.Models;

namespace WorkingSpaces.Data
{
    public static class DbSeeder
    {
        public static void Seed(ApplicationDbContext context)
        {
            if (context.Bookings.Any())
            {
                return; 
            }
            Console.WriteLine("База порожня. Починаю заповнення даних...");

            var userFaker = new Faker<User>()
                .RuleFor(u => u.UserId, f => Guid.NewGuid())
                .RuleFor(u => u.Username, f => f.Internet.UserName())
                .RuleFor(u => u.FullName, f => f.Name.FullName())
                .RuleFor(u => u.Email, f => f.Internet.Email())
                .RuleFor(u => u.PhoneNumber, f => f.Phone.PhoneNumber("+380#########"))
                .RuleFor(u => u.Password, f => "$2a$11$H/wO0/..gQd1r1/..passwordhash..");

            var users = userFaker.Generate(50);
            users = users.GroupBy(u => u.Email).Select(g => g.First()).ToList();
            context.Users.AddRange(users);
            context.SaveChanges();

            var spaceFaker = new Faker<Space>()
                .RuleFor(s => s.Name, f => f.Commerce.ProductName() + " Room")
                .RuleFor(s => s.NumberOfSeats, f => f.Random.Int(2, 20))
                .RuleFor(s => s.AvailableEquipment, f => f.PickRandom<Equipment>());

            var spaces = spaceFaker.Generate(20);
            context.Spaces.AddRange(spaces);
            context.SaveChanges();

            Console.WriteLine("Генеруюю бронювання...");
            var bookingFaker = new Faker<Booking>()
                .RuleFor(b => b.User, f => f.PickRandom(users))
                .RuleFor(b => b.Space, f => f.PickRandom(spaces))
                .RuleFor(b => b.StartTime, f => {
                    var date = f.Date.Future(60); 
                    return new DateTime(date.Year, date.Month, date.Day, f.Random.Int(9, 18), 0, 0, DateTimeKind.Utc);
                })
                .RuleFor(b => b.EndTime, (f, b) => b.StartTime.AddHours(f.Random.Int(1, 3)));

            int total = 10000;
            int batchSize = 1000;

            for (int i = 0; i < total; i += batchSize)
            {
                var batch = bookingFaker.Generate(batchSize);
                context.Bookings.AddRange(batch);
                try
                {
                    context.SaveChanges();
                }
                catch(Exception ex)
                {
                    Console.WriteLine($"Ошибка при заполнении базы: {ex.Message}");
                }
            }
            Console.WriteLine("Дані сгенеровані!");
        }
    }
}