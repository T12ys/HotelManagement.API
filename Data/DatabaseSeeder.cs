using HotelWebApplication.Enums;
using HotelWebApplication.Models;
using Microsoft.EntityFrameworkCore;

namespace HotelWebApplication.Data;

/// <summary>
/// Seeds the database with initial data on first startup.
/// Applies pending migrations, creates admin/moderator accounts,
/// room types, rooms, tags, price rules and sample reservations.
/// Safe to call on every startup — skips entities that already exist.
/// </summary>
public static class DatabaseSeeder
{
    public static async Task SeedAsync(IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<HotelDbContext>();
        var cfg = scope.ServiceProvider.GetRequiredService<IConfiguration>();

        await db.Database.MigrateAsync();

        await SeedUsersAsync(db, cfg);
        await SeedTagsAsync(db);
        await SeedRoomTypesAndRoomsAsync(db);
        await SeedPriceRulesAsync(db);
        await SeedReservationsAsync(db);
    }

    private static async Task SeedUsersAsync(HotelDbContext db, IConfiguration cfg)
    {
        var adminEmail = cfg["Seed:AdminEmail"] ?? "admin@hotel.local";
        var adminPassword = cfg["Seed:AdminPassword"] ?? "Admin123!";

        if (!await db.Users.AnyAsync(u => u.Email == adminEmail))
            db.Users.Add(new User
            {
                Email = adminEmail,
                DisplayName = "Administrator",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(adminPassword),
                Salt = Guid.NewGuid().ToString(),
                SecurityStamp = Guid.NewGuid().ToString(),
                Role = UserRole.Admin,
                CreatedAt = DateTime.UtcNow,
                IsActive = true
            });

        const string modEmail = "moderator@hotel.local";
        if (!await db.Users.AnyAsync(u => u.Email == modEmail))
            db.Users.Add(new User
            {
                Email = modEmail,
                DisplayName = "Demo Moderator",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("Moderator123!"),
                Salt = Guid.NewGuid().ToString(),
                SecurityStamp = Guid.NewGuid().ToString(),
                Role = UserRole.Moderator,
                CreatedAt = DateTime.UtcNow,
                IsActive = true
            });

        const string customerEmail = "guest@hotel.local";
        if (!await db.Users.AnyAsync(u => u.Email == customerEmail))
            db.Users.Add(new User
            {
                Email = customerEmail,
                DisplayName = "Demo Guest",
                PhoneNumber = "+994501234567",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("Guest123!"),
                Salt = Guid.NewGuid().ToString(),
                SecurityStamp = Guid.NewGuid().ToString(),
                Role = UserRole.Customer,
                CreatedAt = DateTime.UtcNow,
                IsActive = true
            });

        await db.SaveChangesAsync();
    }

    private static async Task SeedTagsAsync(HotelDbContext db)
    {
        if (await db.Tags.AnyAsync()) return;

        db.Tags.AddRange(
            new Tag { Slug = "wifi", Translations = new() { ["en"] = "Wi-Fi", ["ru"] = "Wi-Fi", ["az"] = "Wi-Fi" } },
            new Tag { Slug = "sea-view", Translations = new() { ["en"] = "Sea View", ["ru"] = "Вид на море", ["az"] = "Dəniz mənzərəsi" } },
            new Tag { Slug = "balcony", Translations = new() { ["en"] = "Balcony", ["ru"] = "Балкон", ["az"] = "Balkon" } },
            new Tag { Slug = "jacuzzi", Translations = new() { ["en"] = "Jacuzzi", ["ru"] = "Джакузи", ["az"] = "Cakuzi" } },
            new Tag { Slug = "air-con", Translations = new() { ["en"] = "Air Conditioning", ["ru"] = "Кондиционер", ["az"] = "Kondisioner" } },
            new Tag { Slug = "breakfast", Translations = new() { ["en"] = "Breakfast Included", ["ru"] = "Завтрак включён", ["az"] = "Səhər yeməyi daxildir" } },
            new Tag { Slug = "king-bed", Translations = new() { ["en"] = "King Bed", ["ru"] = "Кинг-кровать", ["az"] = "King çarpayı" } },
            new Tag { Slug = "city-view", Translations = new() { ["en"] = "City View", ["ru"] = "Вид на город", ["az"] = "Şəhər mənzərəsi" } },
            new Tag { Slug = "pool-access", Translations = new() { ["en"] = "Pool Access", ["ru"] = "Бассейн", ["az"] = "Hovuz girişi" } },
            new Tag { Slug = "pet-friendly", Translations = new() { ["en"] = "Pet Friendly", ["ru"] = "Можно с питомцами", ["az"] = "Ev heyvanları ilə" } }
        );

        await db.SaveChangesAsync();
    }

    /// <summary>
    /// Seeds room types and physical rooms in a single method.
    /// Rooms are inserted after SaveChanges so that RoomType IDs are guaranteed to be set.
    /// </summary>
    private static async Task SeedRoomTypesAndRoomsAsync(HotelDbContext db)
    {

        if (!await db.RoomTypes.AnyAsync())
        {
            var tags = await db.Tags.ToListAsync();
            Tag T(string slug) => tags.First(t => t.Slug == slug);

            db.RoomTypes.AddRange(
                new RoomType
            {
                Code = "STANDARD",
                Name = "Standard Room",
                Description = "A comfortable standard room with all essential amenities, perfect for business travellers and short stays.",
                Capacity = 2,
                MaxOccupancyAdults = 2,
                MaxOccupancyChildren = 1,
                BasePrice = 80m,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                Tags = new List<Tag> { T("wifi"), T("air-con") },
                Photos = new List<RoomPhoto>
                {
                    new() { Url = "https://images.unsplash.com/photo-1631049307264-da0ec9d70304?w=800", SortOrder = 0, AltText = "Standard room overview" },
                    new() { Url = "https://images.unsplash.com/photo-1587985064135-0366536eab42?w=800", SortOrder = 1, AltText = "Standard room bathroom" }
                }
            },
            new RoomType
            {
                Code = "DELUXE",
                Name = "Deluxe Room",
                Description = "Spacious deluxe room featuring a private balcony with stunning city views and premium furnishings.",
                Capacity = 3,
                MaxOccupancyAdults = 2,
                MaxOccupancyChildren = 1,
                BasePrice = 140m,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                Tags = new List<Tag> { T("wifi"), T("air-con"), T("balcony"), T("city-view") },
                Photos = new List<RoomPhoto>
                {
                    new() { Url = "https://images.unsplash.com/photo-1590490360182-c33d57733427?w=800", SortOrder = 0, AltText = "Deluxe room with balcony" },
                    new() { Url = "https://images.unsplash.com/photo-1578683010236-d716f9a3f461?w=800", SortOrder = 1, AltText = "Deluxe room interior" }
                }
            },
            new RoomType
            {
                Code = "SUITE",
                Name = "Junior Suite",
                Description = "Elegant junior suite with a separate living area, king-size bed, and breathtaking sea view.",
                Capacity = 3,
                MaxOccupancyAdults = 2,
                MaxOccupancyChildren = 1,
                BasePrice = 220m,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                Tags = new List<Tag> { T("wifi"), T("air-con"), T("sea-view"), T("king-bed"), T("breakfast") },
                Photos = new List<RoomPhoto>
                {
                    new() { Url = "https://images.unsplash.com/photo-1618773928121-c32242e63f39?w=800", SortOrder = 0, AltText = "Junior suite living area" },
                    new() { Url = "https://images.unsplash.com/photo-1596394516093-501ba68a0ba6?w=800", SortOrder = 1, AltText = "Junior suite bedroom" }
                }
            },
            new RoomType
            {
                Code = "PRESIDENTIAL",
                Name = "Presidential Suite",
                Description = "Our most exclusive suite offering panoramic sea views, a private jacuzzi, dedicated concierge service, and luxury amenities throughout.",
                Capacity = 6,
                MaxOccupancyAdults = 4,
                MaxOccupancyChildren = 2,
                BasePrice = 500m,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                Tags = new List<Tag> { T("wifi"), T("air-con"), T("sea-view"), T("jacuzzi"), T("king-bed"), T("breakfast"), T("pool-access") },
                Photos = new List<RoomPhoto>
                {
                    new() { Url = "https://images.unsplash.com/photo-1582719478250-c89cae4dc85b?w=800", SortOrder = 0, AltText = "Presidential suite panoramic view" },
                    new() { Url = "https://images.unsplash.com/photo-1631049552057-403cdb8f0658?w=800", SortOrder = 1, AltText = "Presidential suite jacuzzi" }
                }
            },
            new RoomType
            {
                Code = "FAMILY",
                Name = "Family Room",
                Description = "Generous family room designed for guests travelling with children. Includes two separate sleeping areas and pet-friendly policy.",
                Capacity = 5,
                MaxOccupancyAdults = 2,
                MaxOccupancyChildren = 3,
                BasePrice = 180m,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                Tags = new List<Tag> { T("wifi"), T("air-con"), T("pet-friendly"), T("breakfast") },
                Photos = new List<RoomPhoto>
                {
                    new() { Url = "https://images.unsplash.com/photo-1594563703937-fdc640497dcd?w=800", SortOrder = 0, AltText = "Family room overview" }
                }
            }
        );

        // SaveChanges here — EF assigns IDs to all RoomType entities
        await db.SaveChangesAsync();
    }

        if (!await db.Rooms.AnyAsync())
        {
            var rt = await db.RoomTypes
            .AsNoTracking()
            .ToDictionaryAsync(x => x.Code, x => x.Id);

            db.Rooms.AddRange(
                // Standard — floors 1–3
                new Room { Number = "101", RoomTypeId = rt["STANDARD"], Floor = 1, IsAvailable = true, CreatedAt = DateTime.UtcNow },
            new Room { Number = "102", RoomTypeId = rt["STANDARD"], Floor = 1, IsAvailable = true, CreatedAt = DateTime.UtcNow },
            new Room { Number = "103", RoomTypeId = rt["STANDARD"], Floor = 1, IsAvailable = true, CreatedAt = DateTime.UtcNow },
            new Room { Number = "201", RoomTypeId = rt["STANDARD"], Floor = 2, IsAvailable = true, CreatedAt = DateTime.UtcNow },
            new Room { Number = "202", RoomTypeId = rt["STANDARD"], Floor = 2, IsAvailable = true, CreatedAt = DateTime.UtcNow },
            new Room { Number = "301", RoomTypeId = rt["STANDARD"], Floor = 3, IsAvailable = true, CreatedAt = DateTime.UtcNow },
            // Deluxe — floors 3–5
            new Room { Number = "302", RoomTypeId = rt["DELUXE"], Floor = 3, IsAvailable = true, CreatedAt = DateTime.UtcNow },
            new Room { Number = "401", RoomTypeId = rt["DELUXE"], Floor = 4, IsAvailable = true, CreatedAt = DateTime.UtcNow },
            new Room { Number = "402", RoomTypeId = rt["DELUXE"], Floor = 4, IsAvailable = true, CreatedAt = DateTime.UtcNow },
            new Room { Number = "501", RoomTypeId = rt["DELUXE"], Floor = 5, IsAvailable = true, CreatedAt = DateTime.UtcNow },
            // Junior Suites — floors 6–7
            new Room { Number = "601", RoomTypeId = rt["SUITE"], Floor = 6, IsAvailable = true, CreatedAt = DateTime.UtcNow },
            new Room { Number = "602", RoomTypeId = rt["SUITE"], Floor = 6, IsAvailable = true, CreatedAt = DateTime.UtcNow },
            new Room { Number = "701", RoomTypeId = rt["SUITE"], Floor = 7, IsAvailable = true, CreatedAt = DateTime.UtcNow },
            // Family — floors 2–3
            new Room { Number = "203", RoomTypeId = rt["FAMILY"], Floor = 2, IsAvailable = true, CreatedAt = DateTime.UtcNow },
            new Room { Number = "303", RoomTypeId = rt["FAMILY"], Floor = 3, IsAvailable = true, CreatedAt = DateTime.UtcNow },
            // Presidential — top floor
            new Room { Number = "P01", RoomTypeId = rt["PRESIDENTIAL"], Floor = 10, IsAvailable = true, CreatedAt = DateTime.UtcNow }
        );

        await db.SaveChangesAsync();
    }
    }

    private static async Task SeedPriceRulesAsync(HotelDbContext db)
    {
        if (await db.PriceRules.AnyAsync()) return;

        var today = DateTime.UtcNow.Date;
        var rt = await db.RoomTypes.AsNoTracking().ToDictionaryAsync(x => x.Code, x => x.Id);
        if (rt.Count == 0) return;

        db.PriceRules.AddRange(
            new PriceRule
            {
                Name = "Summer Season Surcharge",
                RuleType = RuleType.SeasonalRange,
                RoomTypeId = null,
                StartDate = new DateTime(today.Year, 6, 1),
                EndDate = new DateTime(today.Year, 8, 31),
                IsIncrease = true,
                IsPercent = true,
                Value = 25m,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            },
            new PriceRule
            {
                Name = "New Year Holiday Surcharge",
                RuleType = RuleType.SeasonalRange,
                RoomTypeId = null,
                StartDate = new DateTime(today.Year, 12, 28),
                EndDate = new DateTime(today.Year + 1, 1, 3),
                IsIncrease = true,
                IsPercent = true,
                Value = 40m,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            },
            new PriceRule
            {
                Name = "Presidential Early Bird Discount",
                RuleType = RuleType.SeasonalRange,
                RoomTypeId = rt["PRESIDENTIAL"],
                StartDate = today.AddDays(30),
                EndDate = today.AddDays(120),
                IsIncrease = false,
                IsPercent = true,
                Value = 15m,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            },
            new PriceRule
            {
                Name = "Standard Midweek Discount",
                RuleType = RuleType.SeasonalRange,
                RoomTypeId = rt["STANDARD"],
                StartDate = today,
                EndDate = today.AddDays(90),
                IsIncrease = false,
                IsPercent = false,
                Value = 10m,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            }
        );

        await db.SaveChangesAsync();
    }

    private static async Task SeedReservationsAsync(HotelDbContext db)
    {
        if (await db.Reservations.AnyAsync()) return;

        var rooms = await db.Rooms.AsNoTracking().ToDictionaryAsync(r => r.Number, r => r.Id);
        if (rooms.Count == 0) return;

        var customerId = await db.Users
            .Where(u => u.Role == UserRole.Customer)
            .Select(u => (Guid?)u.Id)
            .FirstOrDefaultAsync();

        var today = DateTime.UtcNow.Date;

        db.Reservations.AddRange(
            new Reservation
            {
                Id = Guid.NewGuid(),
                RoomId = rooms["101"],
                UserId = customerId,
                CustomerName = "Alice Johnson",
                CustomerEmail = "alice@example.com",
                CustomerPhone = "+994501112233",
                StartDate = today.AddDays(-2),
                EndDate = today.AddDays(3),
                TotalPrice = 400m,
                Status = ReservationStatus.Confirmed,
                PaidAt = today.AddDays(-2),
                Source = "web",
                CreatedAt = today.AddDays(-5),
                UpdatedAt = today.AddDays(-2)
            },
            new Reservation
            {
                Id = Guid.NewGuid(),
                RoomId = rooms["401"],
                CustomerName = "Bob Smith",
                CustomerEmail = "bob@example.com",
                CustomerPhone = "+994552223344",
                StartDate = today.AddDays(5),
                EndDate = today.AddDays(10),
                TotalPrice = 700m,
                Status = ReservationStatus.Confirmed,
                PaidAt = today.AddDays(-1),
                Source = "web",
                CreatedAt = today.AddDays(-3),
                UpdatedAt = today.AddDays(-1)
            },
            new Reservation
            {
                Id = Guid.NewGuid(),
                RoomId = rooms["201"],
                CustomerName = "Carol White",
                CustomerEmail = "carol@example.com",
                CustomerPhone = "+994703334455",
                StartDate = today.AddDays(7),
                EndDate = today.AddDays(9),
                TotalPrice = 160m,
                Status = ReservationStatus.Pending,
                HeldUntil = DateTime.UtcNow.AddMinutes(12),
                Source = "web",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            },
            new Reservation
            {
                Id = Guid.NewGuid(),
                RoomId = rooms["601"],
                CustomerName = "David Brown",
                CustomerEmail = "david@example.com",
                CustomerPhone = "+994504445566",
                StartDate = today.AddDays(-10),
                EndDate = today.AddDays(-7),
                TotalPrice = 660m,
                Status = ReservationStatus.Cancelled,
                Source = "web",
                CreatedAt = today.AddDays(-14),
                UpdatedAt = today.AddDays(-11)
            },
            new Reservation
            {
                Id = Guid.NewGuid(),
                RoomId = rooms["102"],
                CustomerName = "Eva Martinez",
                CustomerEmail = "eva@example.com",
                CustomerPhone = "+994555556677",
                StartDate = today.AddDays(-20),
                EndDate = today.AddDays(-15),
                TotalPrice = 400m,
                Status = ReservationStatus.Completed,
                PaidAt = today.AddDays(-21),
                Notes = "Requested extra pillows.",
                Source = "web",
                CreatedAt = today.AddDays(-25),
                UpdatedAt = today.AddDays(-15)
            },
            new Reservation
            {
                Id = Guid.NewGuid(),
                RoomId = rooms["P01"],
                CustomerName = "Frank Lee",
                CustomerEmail = "frank@example.com",
                CustomerPhone = "+994516667788",
                StartDate = today.AddDays(14),
                EndDate = today.AddDays(21),
                TotalPrice = 3500m,
                Status = ReservationStatus.Confirmed,
                PaidAt = today.AddDays(-2),
                Notes = "VIP guest — arrange welcome amenity.",
                Source = "web",
                CreatedAt = today.AddDays(-5),
                UpdatedAt = today.AddDays(-2),
                ReservationItems = new List<ReservationItem>
                {
                    new() { Name = "Airport Transfer", Price = 50m, Quantity = 1 },
                    new() { Name = "Daily Breakfast",  Price = 30m, Quantity = 7 }
                }
            }
        );

        await db.SaveChangesAsync();
    }
}