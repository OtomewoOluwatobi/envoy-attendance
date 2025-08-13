namespace envoy_attendance.Models.Entities
{
    public class Member
    {
        public Guid Id { get; set; }
        public required string Name { get; set; }
        public required string Email { get; set; }
        public required string Phone { get; set; }
        public required string Password { get; set; }
        public DateTime CreatedAt { get; set; }
        public Member()
        {
            Id = Guid.NewGuid();
            CreatedAt = DateTime.UtcNow;
        }
    }
}
