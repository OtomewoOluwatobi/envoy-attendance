using System.ComponentModel.DataAnnotations;

namespace envoy_attendance.Models.Dtos.Admin
{
    public class UpdatePasswordDto
    {
        [Required]
        public required string CurrentPassword { get; set; }
        
        [Required]
        [StringLength(100, MinimumLength = 6)]
        public required string NewPassword { get; set; }
        
        [Required]
        [Compare("NewPassword")]
        public required string ConfirmPassword { get; set; }
    }
}