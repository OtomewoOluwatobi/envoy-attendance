using System.ComponentModel.DataAnnotations;

namespace envoy_attendance.Models.Dtos.Admin
{
    public class UpdateAdminDto
    {
        [Required]
        [StringLength(100, MinimumLength = 3)]
        public required string Name { get; set; }
        
        [Required]
        [EmailAddress]
        public required string Email { get; set; }
        
        [Phone]
        public string? Phone { get; set; }
    }
}