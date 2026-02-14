using HotelWebApplication.Enums;

namespace HotelWebApplication.Models
{
    public class User
    {
        public Guid Id { get; set; } = Guid.NewGuid();

        public string Email { get; set; } = null!;

        public string DisplayName { get; set; } = null!;

        public string PasswordHash { get; set; } = null!;

        public string Salt { get; set; } = null!;

        public string SecurityStamp { get; set; } = Guid.NewGuid().ToString();

        public UserRole Role { get; set; } = UserRole.Customer;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? LastLoginAt { get; set; }

        public bool IsActive { get; set; } = true;
    }
}
