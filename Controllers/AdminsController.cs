using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Course_management.Data;
using Course_management.Models;
using Course_management.Dto;
using System.Threading.Tasks;
using System.Linq;
using System.Collections.Generic;
using System.Security.Claims;

namespace Course_management.Controllers
{
    [Route("api/admins")]
    [ApiController]
    [Authorize(Roles = "Admin")] // Only admins can access
    public class AdminsController : ControllerBase
    {
        private readonly DataContext _context;
        private readonly UserManager<User> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;

        public AdminsController(DataContext context, UserManager<User> userManager, RoleManager<IdentityRole> roleManager)
        {
            _context = context;
            _userManager = userManager;
            _roleManager = roleManager;
        }

        // GET: api/admins/dashboard - Get admin dashboard data
        [HttpGet("dashboard")]
        public async Task<IActionResult> GetDashboard()
        {
            // Get counts for various entities
            var userCount = await _userManager.Users.CountAsync();
            var courseCount = await _context.Courses.CountAsync();
            var enrollmentCount = await _context.Enrollments.CountAsync();
            var reviewCount = await _context.Reviews.CountAsync();

            // Get role counts
            var adminCount = (await _userManager.GetUsersInRoleAsync("Admin")).Count;
            var tutorCount = (await _userManager.GetUsersInRoleAsync("Tutor")).Count;
            var studentCount = (await _userManager.GetUsersInRoleAsync("Student")).Count;

            // Get average rating across all courses
            double averageRating = 0;
            if (await _context.Reviews.AnyAsync())
            {
                averageRating = await _context.Reviews.AverageAsync(r => r.Rating);
            }

            // Get recent enrollments
            var recentEnrollments = await _context.Enrollments
                .Include(e => e.User)
                .Include(e => e.Course)
                .OrderByDescending(e => e.Id) // Assuming Id increments with newer enrollments
                .Take(5)
                .Select(e => new EnrollmentDto
                {
                    Id = e.Id,
                    UserId = e.UserId,
                    UserName = e.User.FullName,
                    CourseId = e.CourseId,
                    CourseTitle = e.Course.Title
                })
                .ToListAsync();

            // Create dashboard response
            var dashboardData = new
            {
                UserCount = userCount,
                CourseCount = courseCount,
                EnrollmentCount = enrollmentCount,
                ReviewCount = reviewCount,
                AdminCount = adminCount,
                TutorCount = tutorCount,
                StudentCount = studentCount,
                AverageRating = averageRating,
                RecentEnrollments = recentEnrollments
            };

            return Ok(ApiResponse<object>.Success(dashboardData, "Dashboard data retrieved successfully", 200));
        }

        // GET api/admins/users - Get all users with role information
        [HttpGet("users")]
        public async Task<IActionResult> GetAllUsers()
        {
            var users = await _userManager.Users.ToListAsync();
            var userDtos = new List<UserDto>();

            foreach (var user in users)
            {
                var roles = await _userManager.GetRolesAsync(user);
                var role = roles.FirstOrDefault() ?? "No Role";

                userDtos.Add(new UserDto
                {
                    Id = user.Id,
                    Email = user.Email,
                    FullName = user.FullName,
                    Role = role
                });
            }

            return Ok(ApiResponse<List<UserDto>>.Success(userDtos, "Users retrieved successfully", 200));
        }

        // GET api/admins/users/{id} - Get user by ID
        [HttpGet("users/{id}")]
        public async Task<IActionResult> GetUser(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
                return NotFound(ApiResponse.Error("User not found", 404));

            var roles = await _userManager.GetRolesAsync(user);
            var role = roles.FirstOrDefault() ?? "No Role";

            var userDto = new UserDto
            {
                Id = user.Id,
                Email = user.Email,
                FullName = user.FullName,
                Role = role
            };

            return Ok(ApiResponse<UserDto>.Success(userDto, "User retrieved successfully", 200));
        }

        // PUT api/admins/users/{id}/role - Update user role
        [HttpPut("users/{id}/role")]
        public async Task<IActionResult> UpdateUserRole(string id, [FromBody] string newRole)
        {
            if (string.IsNullOrEmpty(newRole))
                return BadRequest(ApiResponse.Error("Role cannot be empty", 400));

            // Check if role exists
            if (!await _roleManager.RoleExistsAsync(newRole))
                return BadRequest(ApiResponse.Error($"Role '{newRole}' does not exist", 400));

            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
                return NotFound(ApiResponse.Error("User not found", 404));

            // Get current roles
            var currentRoles = await _userManager.GetRolesAsync(user);

            // Remove current roles
            if (currentRoles.Any())
            {
                await _userManager.RemoveFromRolesAsync(user, currentRoles);
            }

            // Add new role
            var result = await _userManager.AddToRoleAsync(user, newRole);

            if (!result.Succeeded)
            {
                return BadRequest(ApiResponse.Error($"Failed to update role: {string.Join(", ", result.Errors.Select(e => e.Description))}", 400));
            }

            return Ok(ApiResponse.Success("User role updated successfully", 200));
        }

        // GET api/admins/courses - Get all courses with details
        [HttpGet("courses")]
        public async Task<IActionResult> GetAllCourses()
        {
            var courses = await _context.Courses
                .Include(c => c.Tutor)
                .Include(c => c.Enrollments)
                .Include(c => c.Reviews)
                .ToListAsync();

            var courseDtos = courses.Select(c => new CourseDetailDto
            {
                Id = c.Id,
                Title = c.Title,
                Description = c.Description,
                TutorId = c.TutorId,
                TutorName = c.Tutor?.FullName,
                EnrollmentCount = c.Enrollments?.Count ?? 0,
                AverageRating = c.Reviews != null && c.Reviews.Any() 
                    ? c.Reviews.Average(r => r.Rating) 
                    : 0,
                Reviews = c.Reviews?.Select(r => new ReviewDto
                {
                    Id = r.Id,
                    Content = r.Content,
                    Rating = r.Rating,
                    UserId = r.UserId,
                    UserName = r.User?.FullName,
                    CourseId = r.CourseId
                }).ToList() ?? new List<ReviewDto>()
            }).ToList();

            return Ok(ApiResponse<List<CourseDetailDto>>.Success(courseDtos, "Courses retrieved successfully", 200));
        }

        // DELETE api/admins/courses/{id} - Delete a course
        [HttpDelete("courses/{id}")]
        public async Task<IActionResult> DeleteCourse(int id)
        {
            var course = await _context.Courses.FindAsync(id);
            if (course == null)
                return NotFound(ApiResponse.Error("Course not found", 404));

            // Delete related enrollments and reviews
            var enrollments = await _context.Enrollments.Where(e => e.CourseId == id).ToListAsync();
            var reviews = await _context.Reviews.Where(r => r.CourseId == id).ToListAsync();

            _context.Enrollments.RemoveRange(enrollments);
            _context.Reviews.RemoveRange(reviews);
            _context.Courses.Remove(course);

            await _context.SaveChangesAsync();

            return Ok(ApiResponse.Success("Course and related data deleted successfully", 200));
        }

        // GET api/admins/enrollments - Get all enrollments
        [HttpGet("enrollments")]
        public async Task<IActionResult> GetAllEnrollments()
        {
            var enrollments = await _context.Enrollments
                .Include(e => e.User)
                .Include(e => e.Course)
                .ToListAsync();

            var enrollmentDtos = enrollments.Select(e => new EnrollmentDto
            {
                Id = e.Id,
                UserId = e.UserId,
                UserName = e.User?.FullName,
                CourseId = e.CourseId,
                CourseTitle = e.Course?.Title
            }).ToList();

            return Ok(ApiResponse<List<EnrollmentDto>>.Success(enrollmentDtos, "Enrollments retrieved successfully", 200));
        }

        // DELETE api/admins/enrollments/{id} - Delete an enrollment
        [HttpDelete("enrollments/{id}")]
        public async Task<IActionResult> DeleteEnrollment(int id)
        {
            var enrollment = await _context.Enrollments.FindAsync(id);
            if (enrollment == null)
                return NotFound(ApiResponse.Error("Enrollment not found", 404));

            // Also delete any reviews by this user for this course
            var reviews = await _context.Reviews
                .Where(r => r.UserId == enrollment.UserId && r.CourseId == enrollment.CourseId)
                .ToListAsync();

            _context.Reviews.RemoveRange(reviews);
            _context.Enrollments.Remove(enrollment);

            await _context.SaveChangesAsync();

            return Ok(ApiResponse.Success("Enrollment and related reviews deleted successfully", 200));
        }

        // GET api/admins/reviews - Get all reviews
        [HttpGet("reviews")]
        public async Task<IActionResult> GetAllReviews()
        {
            var reviews = await _context.Reviews
                .Include(r => r.User)
                .Include(r => r.Course)
                .ToListAsync();

            var reviewDtos = reviews.Select(r => new ReviewDto
            {
                Id = r.Id,
                Content = r.Content,
                Rating = r.Rating,
                UserId = r.UserId,
                UserName = r.User?.FullName,
                CourseId = r.CourseId,
                CourseTitle = r.Course?.Title
            }).ToList();

            return Ok(ApiResponse<List<ReviewDto>>.Success(reviewDtos, "Reviews retrieved successfully", 200));
        }

        // DELETE api/admins/reviews/{id} - Delete a review
        [HttpDelete("reviews/{id}")]
        public async Task<IActionResult> DeleteReview(int id)
        {
            var review = await _context.Reviews.FindAsync(id);
            if (review == null)
                return NotFound(ApiResponse.Error("Review not found", 404));

            _context.Reviews.Remove(review);
            await _context.SaveChangesAsync();

            return Ok(ApiResponse.Success("Review deleted successfully", 200));
        }
    }
}
