namespace envoy_attendance.Models.Dtos.Admin
{
    public class AddAdminDto
    {
        public required string Name { get; set; }
        public required string Email { get; set; }
        public string? Phone { get; set; }
        public required string Password { get; set; }
        public required string Role { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
