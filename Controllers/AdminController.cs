using envoy_attendance.Models.Dtos.Admin;
using envoy_attendance.Models.Entities;
using envoy_attendance.Services;
using EnvoyAttendance.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace envoy_attendance.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AdminController : ControllerBase
    {
        private readonly ApplicationDbContext dbcontext;
        private readonly JwtService jwtService;
        private readonly IPasswordService passwordService;

        public AdminController(ApplicationDbContext dbcontext, JwtService jwtService, IPasswordService passwordService)
        {
            this.dbcontext = dbcontext;
            this.jwtService = jwtService;
            this.passwordService = passwordService;
        }

        [HttpPost("register")]
        [Authorize] // Require authentication
        public IActionResult CreateAdmin(AddAdminDto addAdminDto)
        {
            // Check if the current user is a SuperAdmin
            var currentUserRole = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Role)?.Value;
            
            if (currentUserRole != UserRole.SuperAdmin.ToString())
            {
                return StatusCode(403, new
                {
                    success = false,
                    message = "Only SuperAdmin users can create new admins.",
                    data = (object?)null
                });
            }

            if (dbcontext.Admins.Any(a => a.Email == addAdminDto.Email))
            {
                return Conflict(new
                {
                    success = false,
                    message = "An admin with this email already exists.",
                    data = (object?)null
                });
            }

            // Hash the password using the password service
            string passwordHash = passwordService.HashPassword(addAdminDto.Password);

            var newAdmin = new Admin()
            {
                Name = addAdminDto.Name,
                Email = addAdminDto.Email,
                Phone = addAdminDto.Phone!,
                Password = passwordHash,
                Role = Enum.Parse<UserRole>(addAdminDto.Role, ignoreCase: true),
            };
            dbcontext.Admins.Add(newAdmin);
            dbcontext.SaveChanges();

            return CreatedAtAction(nameof(CreateAdmin), new { id = newAdmin.Id }, new
            {
                success = true,
                message = "Admin created successfully",
                data = newAdmin
            });
        }

        [HttpPost("login")]
        public IActionResult Login(LoginDto loginDto)
        {
            // Find the admin by email
            var admin = dbcontext.Admins.FirstOrDefault(a => a.Email == loginDto.Email);
            if (admin == null)
            {
                return Unauthorized(new
                {
                    success = false,
                    message = "Invalid email or password",
                    data = (object?)null
                });
            }

            // Verify the password
            if (!passwordService.VerifyPassword(loginDto.Password, admin.Password))
            {
                return Unauthorized(new
                {
                    success = false,
                    message = "Invalid email or password",
                    data = (object?)null
                });
            }

            // Generate JWT token
            var token = jwtService.GenerateToken(admin);

            // Return the token
            return Ok(new
            {
                success = true,
                message = "Login successful",
                data = new
                {
                    Token = token,
                    Admin = new
                    {
                        Id = admin.Id,
                        Name = admin.Name,
                        Email = admin.Email,
                        Role = admin.Role
                    }
                }
            });
        }

        [HttpGet("index")]
        [Authorize] // This requires authentication
        public IActionResult GetAdmins(int pageNumber = 1, int pageSize = 10)
        {
            // Ensure page parameters are valid
            if (pageNumber < 1) pageNumber = 1;
            if (pageSize < 1) pageSize = 10;
            if (pageSize > 50) pageSize = 50; // Max page size limit

            // Get total count for pagination metadata
            var totalCount = dbcontext.Admins.Count();
            
            // Calculate total pages
            var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);
            
            // Apply pagination to query
            var admins = dbcontext.Admins
                .OrderBy(a => a.Id) // Ensure consistent ordering
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .Select(a => new
                {
                    a.Id,
                    a.Name,
                    a.Email,
                    a.Role
                })
                .ToList();

            // Return paginated result with metadata
            return Ok(new
            {
                success = true,
                message = "Admins retrieved successfully",
                data = new
                {
                    admins,
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

        [HttpGet("show/{id:guid}")]
        [Authorize] // Adding authorization to be consistent with other admin-related endpoints
        public IActionResult GetAdmin(Guid id)
        {
            var admin = dbcontext.Admins.Find(id);
            if (admin == null)
            {
                return NotFound(new
                {
                    success = false,
                    message = "Admin not found",
                    data = (object?)null
                });
            }
            
            return Ok(new
            {
                success = true,
                message = "Admin retrieved successfully",
                data = new
                {
                    admin.Id,
                    admin.Name,
                    admin.Email,
                    admin.Role
                    // Note: Password is intentionally excluded for security
                }
            });
        }

        [HttpPut("edit/{id:guid}")]
        [Authorize]
        public IActionResult UpdateAdmin(Guid id, UpdateAdminDto updateAdminDto)
        {
            // Verify the admin exists
            var admin = dbcontext.Admins.Find(id);
            if (admin == null)
            {
                return NotFound(new
                {
                    success = false,
                    message = "Admin not found",
                    data = (object?)null
                });
            }

            // Check if the current user is authorized to update this admin
            var currentUserId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty);
            var currentUserRole = User.FindFirstValue(ClaimTypes.Role);

            // Only allow SuperAdmin to edit other admins, or the admin to edit their own profile
            if (currentUserRole != UserRole.SuperAdmin.ToString() && currentUserId != admin.Id)
            {
                return StatusCode(403, new
                {
                    success = false,
                    message = "You are not authorized to edit this admin",
                    data = (object?)null
                });
            }

            // Check if the email is being changed and if it already exists
            if (updateAdminDto.Email != admin.Email && 
                dbcontext.Admins.Any(a => a.Email == updateAdminDto.Email))
            {
                return Conflict(new
                {
                    success = false,
                    message = "An admin with this email already exists",
                    data = (object?)null
                });
            }

            // Update admin properties
            admin.Name = updateAdminDto.Name;
            admin.Email = updateAdminDto.Email;
            admin.Phone = updateAdminDto.Phone!;

            dbcontext.SaveChanges();

            return Ok(new
            {
                success = true,
                message = "Admin updated successfully",
                data = new
                {
                    admin.Id,
                    admin.Name,
                    admin.Email,
                    admin.Role
                }
            });
        }

        [HttpPut("profile")]
        [Authorize]
        public IActionResult UpdateProfile(UpdateAdminDto updateAdminDto)
        {
            // Get the current user ID
            var currentUserId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty);
            var admin = dbcontext.Admins.Find(currentUserId);

            if (admin == null)
            {
                return NotFound(new
                {
                    success = false,
                    message = "Admin not found",
                    data = (object?)null
                });
            }

            // Check if the email is being changed and if it already exists
            if (updateAdminDto.Email != admin.Email && 
                dbcontext.Admins.Any(a => a.Email == updateAdminDto.Email))
            {
                return Conflict(new
                {
                    success = false,
                    message = "An admin with this email already exists",
                    data = (object?)null
                });
            }

            // Update admin properties
            admin.Name = updateAdminDto.Name;
            admin.Email = updateAdminDto.Email;
            admin.Phone = updateAdminDto.Phone!;

            dbcontext.SaveChanges();

            return Ok(new
            {
                success = true,
                message = "Profile updated successfully",
                data = new
                {
                    admin.Id,
                    admin.Name,
                    admin.Email,
                    admin.Role
                }
            });
        }

        [HttpPut("change-password")]
        [Authorize]
        public IActionResult ChangePassword(ChangePasswordDto changePasswordDto)
        {
            // Get the current user ID
            var currentUserId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty);
            var admin = dbcontext.Admins.Find(currentUserId);

            if (admin == null)
            {
                return NotFound(new
                {
                    success = false,
                    message = "Admin not found",
                    data = (object?)null
                });
            }

            // Verify current password
            if (!passwordService.VerifyPassword(changePasswordDto.CurrentPassword, admin.Password))
            {
                return BadRequest(new
                {
                    success = false,
                    message = "Current password is incorrect",
                    data = (object?)null
                });
            }

            // Hash and save the new password
            admin.Password = passwordService.HashPassword(changePasswordDto.NewPassword);
            dbcontext.SaveChanges();

            return Ok(new
            {
                success = true,
                message = "Password changed successfully",
                data = (object?)null
            });
        }

        [HttpPut("admin/{id:guid}/change-password")]
        [Authorize]
        public IActionResult ChangeAdminPassword(Guid id, AdminPasswordChangeDto passwordChangeDto)
        {
            // Check if the current user is a SuperAdmin
            var currentUserRole = User.FindFirstValue(ClaimTypes.Role);
            
            if (currentUserRole != UserRole.SuperAdmin.ToString())
            {
                return StatusCode(403, new
                {
                    success = false,
                    message = "Only SuperAdmin users can change other admin passwords",
                    data = (object?)null
                });
            }

            // Find the admin
            var admin = dbcontext.Admins.Find(id);
            if (admin == null)
            {
                return NotFound(new
                {
                    success = false,
                    message = "Admin not found",
                    data = (object?)null
                });
            }

            // Hash and update the password
            admin.Password = passwordService.HashPassword(passwordChangeDto.NewPassword);
            dbcontext.SaveChanges();

            return Ok(new
            {
                success = true,
                message = "Admin password updated successfully",
                data = (object?)null
            });
        }

        [HttpPut("admin/{id:guid}/change-role")]
        [Authorize]
        public IActionResult ChangeAdminRole(Guid id, ChangeRoleDto changeRoleDto)
        {
            // Check if the current user is a SuperAdmin
            var currentUserRole = User.FindFirstValue(ClaimTypes.Role);
            var currentUserId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty);
            
            if (currentUserRole != UserRole.SuperAdmin.ToString())
            {
                return StatusCode(403, new
                {
                    success = false,
                    message = "Only SuperAdmin users can change admin roles",
                    data = (object?)null
                });
            }

            // Find the admin
            var admin = dbcontext.Admins.Find(id);
            if (admin == null)
            {
                return NotFound(new
                {
                    success = false,
                    message = "Admin not found",
                    data = (object?)null
                });
            }

            // Prevent SuperAdmin from changing their own role (to avoid having no SuperAdmins)
            if (admin.Id == currentUserId && admin.Role == UserRole.SuperAdmin)
            {
                return BadRequest(new
                {
                    success = false,
                    message = "SuperAdmin cannot change their own role",
                    data = (object?)null
                });
            }

            // Update the role
            try
            {
                admin.Role = Enum.Parse<UserRole>(changeRoleDto.Role, ignoreCase: true);
                dbcontext.SaveChanges();

                return Ok(new
                {
                    success = true,
                    message = "Admin role updated successfully",
                    data = new
                    {
                        admin.Id,
                        admin.Name,
                        admin.Email,
                        admin.Role
                    }
                });
            }
            catch (ArgumentException)
            {
                return BadRequest(new
                {
                    success = false,
                    message = "Invalid role specified",
                    data = (object?)null
                });
            }
        }
    }
}
