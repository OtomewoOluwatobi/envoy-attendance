using envoy_attendance.Models.Entities;

namespace envoy_attendance.Models.Dtos.Admin
{   
    public class EditAdminDto
    {
        public required string Name { get; set; }
        public string? Phone { get; set; }
        public string? Password { get; set; }
        public string? Role { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}
