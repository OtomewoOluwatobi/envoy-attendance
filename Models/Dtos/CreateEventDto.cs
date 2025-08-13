using System.ComponentModel.DataAnnotations;

namespace envoy_attendance.Models.Dtos
{
    public class CreateEventDto
    {
        [Required]
        [StringLength(100, MinimumLength = 3)]
        public required string Title { get; set; }

        [Required]
        [StringLength(500)]
        public required string Description { get; set; }

        [Required]
        public required DateTime StartDate { get; set; }

        [Required]
        public required DateTime EndDate { get; set; }

        [Required]
        [StringLength(200)]
        public required string Location { get; set; }

        [Required]
        [Url]
        [StringLength(255)]
        public required string InterestedQrCodeUrl { get; set; }

        [Required]
        [Url]
        [StringLength(255)]
        public required string AttendingQrCodeUrl { get; set; }
    }
}