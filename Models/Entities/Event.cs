using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace envoy_attendance.Models.Entities
    {
        public class Event
        {
            [Key]
            public Guid Id { get; set; }

            [Required]
            public required Guid AdminId { get; set; }

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

            [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
            public DateTime CreatedAt { get; set; }

            [DatabaseGenerated(DatabaseGeneratedOption.Computed)]
            public DateTime UpdatedAt { get; set; }

            [ForeignKey("AdminId")]
            public virtual Admin? Admin { get; set; }

            public Event()
            {
                Id = Guid.NewGuid();
                CreatedAt = DateTime.UtcNow;
                UpdatedAt = DateTime.UtcNow;
            }

            // Validate that end date is after start date
            public bool IsValidDateRange() => EndDate > StartDate;
        }
    }
