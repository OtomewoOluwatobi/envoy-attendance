using System.ComponentModel.DataAnnotations;

namespace envoy_attendance.Models.Dtos.Admin
{
    public class ChangeRoleDto
    {
        [Required]
        public required string Role { get; set; }
    }
}