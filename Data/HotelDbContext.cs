using HotelWebApplication.Data;
using HotelWebApplication.Models;
using Microsoft.EntityFrameworkCore;

namespace HotelWebApplication.Data;

public class HotelDbContext : DbContext
{
    public HotelDbContext(DbContextOptions<HotelDbContext> options)
        : base(options)
    { }

    public DbSet<User> Users => Set<User>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();

    public DbSet<RoomType> RoomTypes => Set<RoomType>();
    public DbSet<Tag> Tags => Set<Tag>();
    public DbSet<RoomPhoto> RoomPhotos => Set<RoomPhoto>();
    public DbSet<Room> Rooms => Set<Room>();

    public DbSet<PriceRule> PriceRules => Set<PriceRule>();

    public DbSet<Reservation> Reservations => Set<Reservation>();
    public DbSet<ReservationItem> ReservationItems => Set<ReservationItem>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<User>(u =>
        {
            u.HasKey(x => x.Id);
            u.HasIndex(x => x.Email).IsUnique();
            u.Property(x => x.Email).IsRequired().HasMaxLength(200);
            u.Property(x => x.DisplayName).IsRequired().HasMaxLength(100);
            u.Property(x => x.PasswordHash).IsRequired();
            u.Property(x => x.Salt).IsRequired();
            u.Property(x => x.SecurityStamp).IsRequired();
            u.Property(x => x.Role).IsRequired().HasConversion<int>();
            u.Property(x => x.CreatedAt).IsRequired();
            u.Property(x => x.IsActive).HasDefaultValue(true);
        });

        modelBuilder.Entity<RefreshToken>(rt =>
        {
            rt.HasKey(x => x.Id);
            rt.HasIndex(x => x.Token).IsUnique();
            rt.Property(x => x.Token).IsRequired();
            rt.Property(x => x.ExpiresAt).IsRequired();
            rt.Property(x => x.CreatedAt).HasDefaultValueSql("GETUTCDATE()");

            rt.HasOne(x => x.User)
              .WithMany()
              .HasForeignKey(x => x.UserId)
              .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<AuditLog>(al =>
        {
            al.HasKey(x => x.Id);
            al.HasOne(x => x.ActorUser)
              .WithMany()
              .HasForeignKey(x => x.ActorUserId)
              .OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<RoomType>(rt =>
        {
            rt.HasKey(x => x.Id);
            rt.HasIndex(x => x.IsActive);

            rt.HasMany(x => x.Photos)
              .WithOne(p => p.RoomType)
              .HasForeignKey(p => p.RoomTypeId)
              .OnDelete(DeleteBehavior.Cascade);

            rt.HasMany(x => x.Tags)
              .WithMany(t => t.RoomTypes)
              .UsingEntity<Dictionary<string, object>>(
                "RoomTypeTag",
                r => r.HasOne<Tag>().WithMany().HasForeignKey("TagId"),
                t => t.HasOne<RoomType>().WithMany().HasForeignKey("RoomTypeId"),
                j => j.HasKey("RoomTypeId", "TagId")
              );

            rt.Property(x => x.BasePrice).HasColumnType("decimal(18,2)");

            rt.HasMany(x => x.Rooms)
              .WithOne(x => x.RoomType)
              .HasForeignKey(x => x.RoomTypeId)
              .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<RoomPhoto>(rp =>
        {
            rp.HasKey(x => x.Id);
            rp.Property(x => x.Url).IsRequired();
        });

        modelBuilder.Entity<Tag>(t =>
        {
            t.HasKey(x => x.Id);
            t.Property(x => x.Name).IsRequired().HasMaxLength(100);
            t.Property(x => x.Slug).IsRequired().HasMaxLength(100);
        });

        modelBuilder.Entity<PriceRule>(pr =>
        {
            pr.HasKey(x => x.Id);

            pr.Property(x => x.Name)
              .IsRequired()
              .HasMaxLength(200);

            pr.Property(x => x.Value)
              .HasColumnType("decimal(18,2)");

            pr.Property(x => x.RuleType)
              .HasConversion<int>();

            pr.Property(x => x.StartDate).IsRequired();
            pr.Property(x => x.EndDate).IsRequired();
            pr.Property(x => x.IsActive).HasDefaultValue(true);
            pr.Property(x => x.CreatedAt).IsRequired();
            pr.Property(x => x.UpdatedAt).IsRequired();

            pr.HasIndex(x => new { x.RoomTypeId, x.StartDate, x.EndDate });

            pr.HasOne(x => x.RoomType)
              .WithMany()
              .HasForeignKey(x => x.RoomTypeId)
              .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Reservation>(r =>
        {
            r.HasKey(x => x.Id);
            r.HasIndex(x => new { x.RoomId, x.StartDate, x.EndDate, x.Status });
            r.Property(x => x.ConcurrencyToken).IsRowVersion();

            r.HasOne(x => x.Room)
             .WithMany()
             .HasForeignKey(x => x.RoomId)
             .OnDelete(DeleteBehavior.Restrict);

            r.HasMany(x => x.ReservationItems)
             .WithOne(ri => ri.Reservation)
             .HasForeignKey(ri => ri.ReservationId)
             .OnDelete(DeleteBehavior.Cascade);

            r.Property(x => x.TotalPrice).HasColumnType("decimal(18,2)");
        });

        modelBuilder.Entity<ReservationItem>(ri =>
        {
            ri.HasKey(x => x.Id);
            ri.Property(x => x.Name).IsRequired().HasMaxLength(200);
            ri.Property(x => x.Price).HasColumnType("decimal(18,2)");
        });

        modelBuilder.Entity<Room>(r =>
        {
            r.HasKey(x => x.Id);
            r.Property(x => x.Number).IsRequired().HasMaxLength(50);
            r.Property(x => x.IsAvailable).HasDefaultValue(true);
        });
    }
}