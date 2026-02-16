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

    //fixed
    public DbSet<PriceRule> PriceRules => Set<PriceRule>();


    public DbSet<Reservation> Reservations => Set<Reservation>();
    public DbSet<ReservationItem> ReservationItems => Set<ReservationItem>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);


        modelBuilder.Entity<User>(u =>
        {
            u.HasKey(x => x.Id);
            u.HasIndex(x => x.Email)
             .IsUnique(); // уникальный email
            u.Property(x => x.Email)
             .IsRequired()
             .HasMaxLength(200);
            u.Property(x => x.DisplayName)
             .IsRequired()
             .HasMaxLength(100);
            u.Property(x => x.PasswordHash)
             .IsRequired();
            u.Property(x => x.Salt)
             .IsRequired();
            u.Property(x => x.SecurityStamp)
             .IsRequired();
            u.Property(x => x.Role)
             .IsRequired()
             .HasConversion<int>(); // храним enum как int в БД
            u.Property(x => x.CreatedAt)
             .IsRequired();
            u.Property(x => x.IsActive)
             .HasDefaultValue(true);
        });


        modelBuilder.Entity<RefreshToken>(rt =>
        {
            rt.HasKey(x => x.Id);
            rt.HasIndex(x => x.Token)
              .IsUnique(); // уникальный токен
            rt.Property(x => x.Token)
              .IsRequired();
            rt.Property(x => x.ExpiresAt)
              .IsRequired();
            rt.Property(x => x.CreatedAt)
              .HasDefaultValueSql("GETUTCDATE()"); // время создания

            // Связь с User
            rt.HasOne(x => x.User)
              .WithMany() // если не создаем коллекцию токенов в User
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
                r => r.HasOne<Tag>()
                .WithMany()
                .HasForeignKey("TagId"),
                t => t.HasOne<RoomType>()
                .WithMany()
                .HasForeignKey("RoomTypeId"),
                j => j.HasKey("RoomTypeId", "TagId")
              );
            rt.Property(x => x.BasePrice)
                .HasColumnType("decimal(18,2)");  //fixed
        });

      
        modelBuilder.Entity<RoomPhoto>(rp =>
        {
            rp.HasKey(x => x.Id);
            rp.Property(x => x.Url).IsRequired();
        });


        modelBuilder.Entity<Tag>(t =>
        {
            t.HasKey(x => x.Id);
            t.Property(x => x.Name)
            .IsRequired()
            .HasMaxLength(100);
            t.Property(x => x.Slug)
            .IsRequired()
            .HasMaxLength(100);
        });


        // ИСПРАВЛЕНО: используем класс вместо интерфейса
        modelBuilder.Entity<PriceRule>(pr =>
        {
            pr.HasKey(x => x.Id);

            // ✅ ИСПРАВЛЕНО: используйте x вместо pr в анонимном объекте
            pr.HasIndex(x => new { x.RoomTypeId, x.StartDate });

            pr.HasOne(x => x.RoomType)
              .WithMany()
              .HasForeignKey(x => x.RoomTypeId)
              .OnDelete(DeleteBehavior.SetNull);

            pr.Property(x => x.Price)
                .HasColumnType("decimal(18,2)");

            // Дополнительно (рекомендуется):
            pr.Property(x => x.StartDate).IsRequired();
            pr.Property(x => x.EndDate).IsRequired();
            pr.Property(x => x.Priority).HasDefaultValue(0);
        });

        modelBuilder.Entity<Reservation>(r =>
        {
            r.HasKey(x => x.Id);
            r.HasIndex(x => new { x.RoomTypeId, x.StartDate, x.EndDate, x.Status });

            r.Property(x => x.ConcurrencyToken)
             .IsRowVersion();

            r.HasOne(x => x.RoomType)
             .WithMany()
             .HasForeignKey(x => x.RoomTypeId)
             .OnDelete(DeleteBehavior.Restrict);

            r.HasMany(x => x.ReservationItems)
             .WithOne(ri => ri.Reservation)
             .HasForeignKey(ri => ri.ReservationId)
             .OnDelete(DeleteBehavior.Cascade);
            // ИСПРАВЛЕНО: правильная настройка decimal
            r.Property(x => x.TotalPrice)
                .HasColumnType("decimal(18,2)");
        });



        modelBuilder.Entity<ReservationItem>(ri =>
        {
            ri.HasKey(x => x.Id);
            ri.Property(x => x.Name).IsRequired().HasMaxLength(200);
            // ИСПРАВЛЕНО: правильная настройка decimal
            ri.Property(x => x.Price)
                .HasColumnType("decimal(18,2)");
        });
    }
}
