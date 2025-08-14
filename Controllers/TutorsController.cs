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
    [Route("api/tutors")]
    [ApiController]
    [Authorize(Roles = "Tutor,Admin")] // Only tutors and admins can access
    public class TutorsController : ControllerBase
    {
        private readonly DataContext _context;
        private readonly UserManager<User> _userManager;

        public TutorsController(DataContext context, UserManager<User> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // GET: api/tutors - Get all tutors (public)
        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> GetTutors()
        {
            var tutors = await _userManager.GetUsersInRoleAsync("Tutor");
            
            var tutorDtos = tutors.Select(t => new UserDto
            {
                Id = t.Id,
                Email = t.Email,
                FullName = t.FullName,
                Role = "Tutor"
            }).ToList();

            return Ok(ApiResponse<List<UserDto>>.Success(tutorDtos, "Tutors retrieved successfully", 200));
        }

        // GET api/tutors/{id} - Get tutor by ID (public)
        [HttpGet("{id}")]
        [AllowAnonymous]
        public async Task<IActionResult> GetTutor(string id)
        {
            var tutor = await _userManager.FindByIdAsync(id);
            if (tutor == null)
                return NotFound(ApiResponse.Error("Tutor not found", 404));

            var roles = await _userManager.GetRolesAsync(tutor);
            if (!roles.Contains("Tutor"))
                return NotFound(ApiResponse.Error("User is not a tutor", 404));

            // Get courses taught by this tutor
            var courses = await _context.Courses
                .Include(c => c.Enrollments)
                .Include(c => c.Reviews)
                .Where(c => c.TutorId == id)
                .ToListAsync();

            var courseDtos = courses.Select(c => new CourseDto
            {
                Id = c.Id,
                Title = c.Title,
                Description = c.Description,
                TutorId = c.TutorId,
                TutorName = tutor.FullName,
                EnrollmentCount = c.Enrollments?.Count ?? 0,
                AverageRating = c.Reviews != null && c.Reviews.Any() 
                    ? c.Reviews.Average(r => r.Rating) 
                    : 0
            }).ToList();

            var tutorWithCourses = new
            {
                Id = tutor.Id,
                Email = tutor.Email,
                FullName = tutor.FullName,
                Role = "Tutor",
                Courses = courseDtos
            };

            return Ok(ApiResponse<object>.Success(tutorWithCourses, "Tutor retrieved successfully", 200));
        }

        // GET api/tutors/dashboard - Get tutor dashboard data
        [HttpGet("dashboard")]
        [Authorize(Roles = "Tutor")]
        public async Task<IActionResult> GetDashboard()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
                return Unauthorized(ApiResponse.Error("User not authenticated", 401));

            // Get courses taught by this tutor
            var courses = await _context.Courses
                .Include(c => c.Enrollments)
                .Include(c => c.Reviews)
                .Where(c => c.TutorId == userId)
                .ToListAsync();

            var courseDtos = courses.Select(c => new CourseDto
            {
                Id = c.Id,
                Title = c.Title,
                Description = c.Description,
                TutorId = c.TutorId,
                EnrollmentCount = c.Enrollments?.Count ?? 0,
                AverageRating = c.Reviews != null && c.Reviews.Any() 
                    ? c.Reviews.Average(r => r.Rating) 
                    : 0
            }).ToList();

            // Get total number of students enrolled in tutor's courses
            var totalStudents = await _context.Enrollments
                .Where(e => courses.Select(c => c.Id).Contains(e.CourseId))
                .Select(e => e.UserId)
                .Distinct()
                .CountAsync();

            // Get average rating across all courses
            double averageRating = 0;
            if (courses.Any() && courses.SelectMany(c => c.Reviews).Any())
            {
                averageRating = courses.SelectMany(c => c.Reviews).Average(r => r.Rating);
            }

            // Create dashboard response
            var dashboardData = new
            {
                Courses = courseDtos,
                CourseCount = courses.Count,
                TotalStudents = totalStudents,
                AverageRating = averageRating
            };

            return Ok(ApiResponse<object>.Success(dashboardData, "Dashboard data retrieved successfully", 200));
        }

        // GET api/tutors/courses - Get all courses for the current tutor
        [HttpGet("courses")]
        [Authorize(Roles = "Tutor")]
        public async Task<IActionResult> GetTutorCourses()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
                return Unauthorized(ApiResponse.Error("User not authenticated", 401));

            var courses = await _context.Courses
                .Include(c => c.Enrollments)
                .Include(c => c.Reviews)
                .Where(c => c.TutorId == userId)
                .ToListAsync();

            var courseDtos = courses.Select(c => new CourseDto
            {
                Id = c.Id,
                Title = c.Title,
                Description = c.Description,
                TutorId = c.TutorId,
                EnrollmentCount = c.Enrollments?.Count ?? 0,
                AverageRating = c.Reviews != null && c.Reviews.Any() 
                    ? c.Reviews.Average(r => r.Rating) 
                    : 0
            }).ToList();

            return Ok(ApiResponse<List<CourseDto>>.Success(courseDtos, "Tutor courses retrieved successfully", 200));
        }

        // GET api/tutors/courses/{courseId}/enrollments - Get all enrollments for a specific course
        [HttpGet("courses/{courseId}/enrollments")]
        [Authorize(Roles = "Tutor")]
        public async Task<IActionResult> GetCourseEnrollments(int courseId)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
                return Unauthorized(ApiResponse.Error("User not authenticated", 401));

            // Check if course exists and belongs to this tutor
            var course = await _context.Courses
                .FirstOrDefaultAsync(c => c.Id == courseId && c.TutorId == userId);

            if (course == null)
                return NotFound(ApiResponse.Error("Course not found or you don't have permission to view it", 404));

            var enrollments = await _context.Enrollments
                .Include(e => e.User)
                .Where(e => e.CourseId == courseId)
                .ToListAsync();

            var enrollmentDtos = enrollments.Select(e => new EnrollmentDto
            {
                Id = e.Id,
                UserId = e.UserId,
                UserName = e.User?.FullName,
                CourseId = e.CourseId,
                CourseTitle = course.Title
            }).ToList();

            return Ok(ApiResponse<List<EnrollmentDto>>.Success(enrollmentDtos, "Course enrollments retrieved successfully", 200));
        }

        // GET api/tutors/courses/{courseId}/reviews - Get all reviews for a specific course
        [HttpGet("courses/{courseId}/reviews")]
        [Authorize(Roles = "Tutor")]
        public async Task<IActionResult> GetCourseReviews(int courseId)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
                return Unauthorized(ApiResponse.Error("User not authenticated", 401));

            // Check if course exists and belongs to this tutor
            var course = await _context.Courses
                .FirstOrDefaultAsync(c => c.Id == courseId && c.TutorId == userId);

            if (course == null)
                return NotFound(ApiResponse.Error("Course not found or you don't have permission to view it", 404));

            var reviews = await _context.Reviews
                .Include(r => r.User)
                .Where(r => r.CourseId == courseId)
                .ToListAsync();

            var reviewDtos = reviews.Select(r => new ReviewDto
            {
                Id = r.Id,
                Content = r.Content,
                Rating = r.Rating,
                UserId = r.UserId,
                UserName = r.User?.FullName,
                CourseId = r.CourseId,
                CourseTitle = course.Title
            }).ToList();

            return Ok(ApiResponse<List<ReviewDto>>.Success(reviewDtos, "Course reviews retrieved successfully", 200));
        }
    }
}
