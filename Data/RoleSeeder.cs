using HotelWebApplication.Enums;
using HotelWebApplication.Models;
using Microsoft.EntityFrameworkCore;

namespace HotelWebApplication.Data;

public static class RoleSeeder
{
    public static async Task SeedAdminAsync(IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<HotelDbContext>();
        var cfg = scope.ServiceProvider.GetRequiredService<IConfiguration>();

        // apply migrations
        await db.Database.MigrateAsync();

        var adminEmail = cfg["Seed:AdminEmail"] ?? "admin@local";
        var adminPassword = cfg["Seed:AdminPassword"] ?? "Admin123!";

        var existing = await db.Users.FirstOrDefaultAsync(u => u.Email == adminEmail);
        if (existing != null) return;

        var pwdHash = BCrypt.Net.BCrypt.HashPassword(adminPassword);

        var admin = new User
        {
            Email = adminEmail,
            DisplayName = "Administrator",
            PasswordHash = pwdHash,
            Salt = Guid.NewGuid().ToString(),
            SecurityStamp = Guid.NewGuid().ToString(),
            Role = UserRole.Admin,
            CreatedAt = DateTime.UtcNow,
            IsActive = true
        };

        db.Users.Add(admin);
        await db.SaveChangesAsync();
    }
}