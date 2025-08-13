using envoy_attendance.Models.Dtos;
using envoy_attendance.Models.Entities;
using EnvoyAttendance.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace envoy_attendance.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class EventController : ControllerBase
    {
        private readonly ApplicationDbContext dbContext;

        public EventController(ApplicationDbContext dbContext)
        {
            this.dbContext = dbContext;
        }

        // GET: api/Event
        [HttpGet]
        public async Task<IActionResult> GetEvents(int pageNumber = 1, int pageSize = 10, DateTime? startDate = null, string? searchTerm = null)
        {
            // Ensure page parameters are valid
            if (pageNumber < 1) pageNumber = 1;
            if (pageSize < 1) pageSize = 10;
            if (pageSize > 50) pageSize = 50; // Max page size limit

            // Build query with filters
            var query = dbContext.Events.AsQueryable();

            // Filter by start date if provided
            if (startDate.HasValue)
            {
                var dateOnly = startDate.Value.Date;
                query = query.Where(e => e.StartDate.Date >= dateOnly);
            }

            // Filter by search term if provided
            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                query = query.Where(e => 
                    e.Title.Contains(searchTerm) || 
                    e.Description.Contains(searchTerm) || 
                    e.Location.Contains(searchTerm));
            }

            // Get total count for pagination metadata
            var totalCount = await query.CountAsync();
            
            // Calculate total pages
            var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);
            
            // Apply pagination and ordering
            var events = await query
                .OrderByDescending(e => e.StartDate) // Show upcoming events first
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .Include(e => e.Admin)
                .Select(e => new EventDetailsDto
                {
                    Id = e.Id,
                    Title = e.Title,
                    Description = e.Description,
                    StartDate = e.StartDate,
                    EndDate = e.EndDate,
                    Location = e.Location,
                    InterestedQrCodeUrl = e.InterestedQrCodeUrl,
                    AttendingQrCodeUrl = e.AttendingQrCodeUrl,
                    CreatedAt = e.CreatedAt,
                    UpdatedAt = e.UpdatedAt,
                    Admin = e.Admin != null ? new AdminSummaryDto
                    {
                        Id = e.Admin.Id,
                        Name = e.Admin.Name,
                        Email = e.Admin.Email
                    } : null,
                    // Count of interested people
                    InterestedCount = dbContext.Interests.Count(i => i.EventId == e.Id),
                    // Count of attending people
                    AttendanceCount = dbContext.Attendances.Count(a => a.EventId == e.Id)
                })
                .ToListAsync();

            // Return paginated result with metadata
            return Ok(new
            {
                success = true,
                message = "Events retrieved successfully",
                data = new
                {
                    events,
                    pagination = new
                    {
                        totalCount,
                        pageSize,
                        currentPage = pageNumber,
                        totalPages,
                        hasNext = pageNumber < totalPages,
                        hasPrevious = pageNumber > 1
                    }
                }
            });
        }

        // GET: api/Event/{id}
        [HttpGet("{id:guid}")]
        public async Task<IActionResult> GetEvent(Guid id)
        {
            var eventEntity = await dbContext.Events
                .Include(e => e.Admin)
                .FirstOrDefaultAsync(e => e.Id == id);

            if (eventEntity == null)
            {
                return NotFound(new
                {
                    success = false,
                    message = "Event not found",
                    data = (object?)null
                });
            }

            // Get counts of interested and attending people
            var interestedCount = await dbContext.Interests.CountAsync(i => i.EventId == id);
            var attendanceCount = await dbContext.Attendances.CountAsync(a => a.EventId == id);

            var eventDetails = new EventDetailsDto
            {
                Id = eventEntity.Id,
                Title = eventEntity.Title,
                Description = eventEntity.Description,
                StartDate = eventEntity.StartDate,
                EndDate = eventEntity.EndDate,
                Location = eventEntity.Location,
                InterestedQrCodeUrl = eventEntity.InterestedQrCodeUrl,
                AttendingQrCodeUrl = eventEntity.AttendingQrCodeUrl,
                CreatedAt = eventEntity.CreatedAt,
                UpdatedAt = eventEntity.UpdatedAt,
                Admin = eventEntity.Admin != null ? new AdminSummaryDto
                {
                    Id = eventEntity.Admin.Id,
                    Name = eventEntity.Admin.Name,
                    Email = eventEntity.Admin.Email
                } : null,
                InterestedCount = interestedCount,
                AttendanceCount = attendanceCount
            };

            return Ok(new
            {
                success = true,
                message = "Event retrieved successfully",
                data = eventDetails
            });
        }

        // POST: api/Event
        [HttpPost]
        [Authorize] // Only authenticated admins can create events
        public async Task<IActionResult> CreateEvent(CreateEventDto createEventDto)
        {
            // Get the current admin ID from claims
            if (!Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out Guid adminId))
            {
                return Unauthorized(new
                {
                    success = false,
                    message = "Invalid authentication token",
                    data = (object?)null
                });
            }

            // Validate the admin exists
            var admin = await dbContext.Admins.FindAsync(adminId);
            if (admin == null)
            {
                return Unauthorized(new
                {
                    success = false,
                    message = "Admin not found",
                    data = (object?)null
                });
            }

            // Create new event
            var newEvent = new Event
            {
                AdminId = adminId,
                Title = createEventDto.Title,
                Description = createEventDto.Description,
                StartDate = createEventDto.StartDate,
                EndDate = createEventDto.EndDate,
                Location = createEventDto.Location,
                InterestedQrCodeUrl = createEventDto.InterestedQrCodeUrl,
                AttendingQrCodeUrl = createEventDto.AttendingQrCodeUrl
            };

            // Validate the date range
            if (!newEvent.IsValidDateRange())
            {
                return BadRequest(new
                {
                    success = false,
                    message = "End date must be after start date",
                    data = (object?)null
                });
            }

            dbContext.Events.Add(newEvent);
            await dbContext.SaveChangesAsync();

            return CreatedAtAction(nameof(GetEvent), new { id = newEvent.Id }, new
            {
                success = true,
                message = "Event created successfully",
                data = new
                {
                    id = newEvent.Id,
                    title = newEvent.Title,
                    startDate = newEvent.StartDate,
                    endDate = newEvent.EndDate,
                    location = newEvent.Location
                }
            });
        }

        // PUT: api/Event/{id}
        [HttpPut("{id:guid}")]
        [Authorize]
        public async Task<IActionResult> UpdateEvent(Guid id, UpdateEventDto updateEventDto)
        {
            var eventEntity = await dbContext.Events.FindAsync(id);
            if (eventEntity == null)
            {
                return NotFound(new
                {
                    success = false,
                    message = "Event not found",
                    data = (object?)null
                });
            }

            // Get the current admin ID from claims
            if (!Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out Guid adminId))
            {
                return Unauthorized(new
                {
                    success = false,
                    message = "Invalid authentication token",
                    data = (object?)null
                });
            }

            // Check if the admin has permission to update this event
            // (Either they created it or they are a SuperAdmin)
            var currentUserRole = User.FindFirstValue(ClaimTypes.Role);
            if (eventEntity.AdminId != adminId && currentUserRole != "SuperAdmin")
            {
                return Forbid();
            }

            // Update event properties
            eventEntity.Title = updateEventDto.Title;
            eventEntity.Description = updateEventDto.Description;
            eventEntity.StartDate = updateEventDto.StartDate;
            eventEntity.EndDate = updateEventDto.EndDate;
            eventEntity.Location = updateEventDto.Location;
            eventEntity.InterestedQrCodeUrl = updateEventDto.InterestedQrCodeUrl;
            eventEntity.AttendingQrCodeUrl = updateEventDto.AttendingQrCodeUrl;

            // Validate the date range
            if (!eventEntity.IsValidDateRange())
            {
                return BadRequest(new
                {
                    success = false,
                    message = "End date must be after start date",
                    data = (object?)null
                });
            }

            await dbContext.SaveChangesAsync();

            return Ok(new
            {
                success = true,
                message = "Event updated successfully",
                data = new
                {
                    id = eventEntity.Id,
                    title = eventEntity.Title,
                    startDate = eventEntity.StartDate,
                    endDate = eventEntity.EndDate,
                    location = eventEntity.Location
                }
            });
        }

        // DELETE: api/Event/{id}
        [HttpDelete("{id:guid}")]
        [Authorize]
        public async Task<IActionResult> DeleteEvent(Guid id)
        {
            var eventEntity = await dbContext.Events.FindAsync(id);
            if (eventEntity == null)
            {
                return NotFound(new
                {
                    success = false,
                    message = "Event not found",
                    data = (object?)null
                });
            }

            // Get the current admin ID from claims
            if (!Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out Guid adminId))
            {
                return Unauthorized(new
                {
                    success = false,
                    message = "Invalid authentication token",
                    data = (object?)null
                });
            }

            // Check if the admin has permission to delete this event
            // (Either they created it or they are a SuperAdmin)
            var currentUserRole = User.FindFirstValue(ClaimTypes.Role);
            if (eventEntity.AdminId != adminId && currentUserRole != "SuperAdmin")
            {
                return Forbid();
            }

            // Check if there are any attendances or interests for this event
            var hasAttendances = await dbContext.Attendances.AnyAsync(a => a.EventId == id);
            var hasInterests = await dbContext.Interests.AnyAsync(i => i.EventId == id);

            if (hasAttendances || hasInterests)
            {
                return BadRequest(new
                {
                    success = false,
                    message = "Cannot delete event with existing attendances or interests",
                    data = new
                    {
                        hasAttendances,
                        hasInterests
                    }
                });
            }

            dbContext.Events.Remove(eventEntity);
            await dbContext.SaveChangesAsync();

            return Ok(new
            {
                success = true,
                message = "Event deleted successfully",
                data = (object?)null
            });
        }

        // GET: api/Event/admin
        [HttpGet("admin")]
        [Authorize]
        public async Task<IActionResult> GetAdminEvents(int pageNumber = 1, int pageSize = 10)
        {
            // Get the current admin ID from claims
            if (!Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out Guid adminId))
            {
                return Unauthorized(new
                {
                    success = false,
                    message = "Invalid authentication token",
                    data = (object?)null
                });
            }

            // Ensure page parameters are valid
            if (pageNumber < 1) pageNumber = 1;
            if (pageSize < 1) pageSize = 10;
            if (pageSize > 50) pageSize = 50; // Max page size limit

            // Check if user is a SuperAdmin, they can see all events
            var currentUserRole = User.FindFirstValue(ClaimTypes.Role);
            var query = dbContext.Events.AsQueryable();

            if (currentUserRole != "SuperAdmin")
            {
                // If not SuperAdmin, filter to only show their own events
                query = query.Where(e => e.AdminId == adminId);
            }

            // Get total count for pagination metadata
            var totalCount = await query.CountAsync();
            
            // Calculate total pages
            var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);
            
            // Apply pagination and ordering
            var events = await query
                .OrderByDescending(e => e.CreatedAt) // Show newest first
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .Include(e => e.Admin)
                .Select(e => new EventDetailsDto
                {
                    Id = e.Id,
                    Title = e.Title,
                    Description = e.Description,
                    StartDate = e.StartDate,
                    EndDate = e.EndDate,
                    Location = e.Location,
                    InterestedQrCodeUrl = e.InterestedQrCodeUrl,
                    AttendingQrCodeUrl = e.AttendingQrCodeUrl,
                    CreatedAt = e.CreatedAt,
                    UpdatedAt = e.UpdatedAt,
                    Admin = e.Admin != null ? new AdminSummaryDto
                    {
                        Id = e.Admin.Id,
                        Name = e.Admin.Name,
                        Email = e.Admin.Email
                    } : null,
                    // Count of interested people
                    InterestedCount = dbContext.Interests.Count(i => i.EventId == e.Id),
                    // Count of attending people
                    AttendanceCount = dbContext.Attendances.Count(a => a.EventId == e.Id)
                })
                .ToListAsync();

            // Return paginated result with metadata
            return Ok(new
            {
                success = true,
                message = "Admin events retrieved successfully",
                data = new
                {
                    events,
                    pagination = new
                    {
                        totalCount,
                        pageSize,
                        currentPage = pageNumber,
                        totalPages,
                        hasNext = pageNumber < totalPages,
                        hasPrevious = pageNumber > 1
                    }
                }
            });
        }

        // GET: api/Event/upcoming
        [HttpGet("upcoming")]
        public async Task<IActionResult> GetUpcomingEvents(int count = 5)
        {
            if (count < 1) count = 5;
            if (count > 20) count = 20; // Limit maximum number of events

            var today = DateTime.UtcNow.Date;
            var upcomingEvents = await dbContext.Events
                .Where(e => e.StartDate.Date >= today)
                .OrderBy(e => e.StartDate) // Order by closest upcoming date
                .Take(count)
                .Include(e => e.Admin)
                .Select(e => new EventDetailsDto
                {
                    Id = e.Id,
                    Title = e.Title,
                    Description = e.Description,
                    StartDate = e.StartDate,
                    EndDate = e.EndDate,
                    Location = e.Location,
                    InterestedQrCodeUrl = e.InterestedQrCodeUrl,
                    AttendingQrCodeUrl = e.AttendingQrCodeUrl,
                    CreatedAt = e.CreatedAt,
                    UpdatedAt = e.UpdatedAt,
                    Admin = e.Admin != null ? new AdminSummaryDto
                    {
                        Id = e.Admin.Id,
                        Name = e.Admin.Name,
                        Email = e.Admin.Email
                    } : null,
                    // Count of interested people
                    InterestedCount = dbContext.Interests.Count(i => i.EventId == e.Id),
                    // Count of attending people
                    AttendanceCount = dbContext.Attendances.Count(a => a.EventId == e.Id)
                })
                .ToListAsync();

            return Ok(new
            {
                success = true,
                message = "Upcoming events retrieved successfully",
                data = upcomingEvents
            });
        }

        // GET: api/Event/statistics
        [HttpGet("statistics")]
        [Authorize]
        public async Task<IActionResult> GetEventStatistics(Guid eventId)
        {
            // Verify the event exists
            var eventEntity = await dbContext.Events.FindAsync(eventId);
            if (eventEntity == null)
            {
                return NotFound(new
                {
                    success = false,
                    message = "Event not found",
                    data = (object?)null
                });
            }

            // Get the current admin ID from claims
            if (!Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out Guid adminId))
            {
                return Unauthorized(new
                {
                    success = false,
                    message = "Invalid authentication token",
                    data = (object?)null
                });
            }

            // Check if the admin has permission to view event statistics
            // (Either they created it or they are a SuperAdmin)
            var currentUserRole = User.FindFirstValue(ClaimTypes.Role);
            if (eventEntity.AdminId != adminId && currentUserRole != "SuperAdmin")
            {
                return Forbid();
            }

            // Get counts
            var interestedCount = await dbContext.Interests.CountAsync(i => i.EventId == eventId);
            var attendanceCount = await dbContext.Attendances.CountAsync(a => a.EventId == eventId);

            // Get member details who are interested
            var interestedMembers = await dbContext.Interests
                .Where(i => i.EventId == eventId)
                .Include(i => i.Member)
                .Select(i => new
                {
                    i.Member!.Id,
                    i.Member.Name,
                    i.Member.Email,
                    InterestedAt = i.CreatedAt
                })
                .ToListAsync();

            // Get member details who are attending
            var attendingMembers = await dbContext.Attendances
                .Where(a => a.EventId == eventId)
                .Include(a => a.Member)
                .Select(a => new
                {
                    a.Member!.Id,
                    a.Member.Name,
                    a.Member.Email,
                    AttendedAt = a.CreatedAt
                })
                .ToListAsync();

            return Ok(new
            {
                success = true,
                message = "Event statistics retrieved successfully",
                data = new
                {
                    eventId,
                    title = eventEntity.Title,
                    statistics = new
                    {
                        interestedCount,
                        attendanceCount,
                        interestedMembers,
                        attendingMembers
                    }
                }
            });
        }
    }
}