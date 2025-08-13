using envoy_attendance.Models.Entities;

namespace envoy_attendance.Models.Dtos
{
    public class EventDetailsDto
    {
        public Guid Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public string Location { get; set; } = string.Empty;
        public string InterestedQrCodeUrl { get; set; } = string.Empty;
        public string AttendingQrCodeUrl { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public AdminSummaryDto? Admin { get; set; }
        public int? InterestedCount { get; set; }
        public int? AttendanceCount { get; set; }
    }

    public class AdminSummaryDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
    }
}