using HotelWebApplication.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace HotelWebApplication.Data;
// Я не смог убрать предупреждение "Validation 30000 No Type Specified for the Decimal Column" при миграции
// как я понял оно требует что бы все decimal поля были явно указаны в длине до и после запятой но даже после явного указание предупреждение не пропадает
// Пробовал 2 варианта .HasPrecision(18, 2) как просилось в ошибке и .HasColumnType("decimal(18,2)") как посоветовал чат гпт оба не работают 
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


    public DbSet<IPriceRuleService> PriceRules => Set<IPriceRuleService>();


    public DbSet<Reservation> Reservations => Set<Reservation>();
    public DbSet<ReservationItem> ReservationItems => Set<ReservationItem>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);


        modelBuilder.Entity<User>(u =>
        {
            u.HasKey(x => x.Id);
            u.HasIndex(x => x.Email).IsUnique();
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
        });


        modelBuilder.Entity<RefreshToken>(rt =>
        {
            rt.HasKey(x => x.Id);
            rt.HasIndex(x => x.Token).IsUnique();
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
                r => r.HasOne<Tag>()
                .WithMany()
                .HasForeignKey("TagId"),
                t => t.HasOne<RoomType>()
                .WithMany()
                .HasForeignKey("RoomTypeId"),
                j => j.HasKey("RoomTypeId", "TagId")
              );
            rt.Property(x => x.BasePrice)
            .HasPrecision(18, 2);
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


        modelBuilder.Entity<IPriceRuleService>(pr =>
        {
            pr.HasKey(x => x.Id);
            pr.HasIndex(x => new { x.RoomTypeId, x.StartDate });
            pr.HasOne(x => x.RoomType)
              .WithMany()
              .HasForeignKey(x => x.RoomTypeId)
              .OnDelete(DeleteBehavior.SetNull);
            pr.Property(x => x.Price)
            .HasPrecision(18, 2);
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
            r.Property(x => x.TotalPrice)
            .HasPrecision(18, 2);
        });



        modelBuilder.Entity<ReservationItem>(ri =>
        {
            ri.HasKey(x => x.Id);
            ri.Property(x => x.Name).IsRequired().HasMaxLength(200);
            ri.Property(x => x.Price)
            .HasPrecision(18, 2);
        });
    }
}
