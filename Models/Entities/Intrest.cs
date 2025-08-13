namespace envoy_attendance.Models.Entities
{
    public class Interest
    {
        public Guid Id { get; set; }
        public required Guid MemberId { get; set; }
        public required Guid EventId { get; set; }
        public DateTime CreatedAt { get; set; }
        public Interest()
        {
            Id = Guid.NewGuid();
            CreatedAt = DateTime.UtcNow;
        }
        // Navigation properties
        public virtual Member? Member { get; set; }
        public virtual Event? Event { get; set; }
    }
}
