namespace envoy_attendance.Models.Entities
{
    public enum UserRole
    {
        Admin,
        SuperAdmin
    }

    public class Admin
    {
        public Guid Id { get; set; }
        public required string Name { get; set; }
        public required string Email { get; set; }
        public required string Phone { get; set; }
        public required string Password { get; set; }
        public required UserRole Role { get; set; } // Changed from Enum to UserRole
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public Admin()
        {
            Id = Guid.NewGuid();
            CreatedAt = DateTime.UtcNow;
            UpdatedAt = DateTime.UtcNow;
        }
    }
}
