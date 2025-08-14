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
    [Route("api/students")]
    [ApiController]
    [Authorize(Roles = "Student,Admin")] // Only students and admins can access
    public class StudentsController : ControllerBase
    {
        private readonly DataContext _context;
        private readonly UserManager<User> _userManager;

        public StudentsController(DataContext context, UserManager<User> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // GET: api/students/dashboard - Get student dashboard data
        [HttpGet("dashboard")]
        [Authorize(Roles = "Student")]
        public async Task<IActionResult> GetDashboard()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
                return Unauthorized(ApiResponse.Error("User not authenticated", 401));

            // Get enrolled courses
            var enrollments = await _context.Enrollments
                .Include(e => e.Course)
                .ThenInclude(c => c.Tutor)
                .Where(e => e.UserId == userId)
                .ToListAsync();

            var enrolledCourses = enrollments.Select(e => new CourseDto
            {
                Id = e.Course.Id,
                Title = e.Course.Title,
                Description = e.Course.Description,
                TutorId = e.Course.TutorId,
                TutorName = e.Course.Tutor?.FullName
            }).ToList();

            // Get reviews written by the student
            var reviews = await _context.Reviews
                .Include(r => r.Course)
                .Where(r => r.UserId == userId)
                .ToListAsync();

            var studentReviews = reviews.Select(r => new ReviewDto
            {
                Id = r.Id,
                Content = r.Content,
                Rating = r.Rating,
                CourseId = r.CourseId,
                CourseTitle = r.Course?.Title
            }).ToList();

            // Create dashboard response
            var dashboardData = new
            {
                EnrolledCourses = enrolledCourses,
                Reviews = studentReviews,
                EnrollmentCount = enrollments.Count
            };

            return Ok(ApiResponse<object>.Success(dashboardData, "Dashboard data retrieved successfully", 200));
        }

        // GET api/students/courses - Get all courses available for enrollment
        [HttpGet("courses")]
        [Authorize(Roles = "Student")]
        public async Task<IActionResult> GetAvailableCourses()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
                return Unauthorized(ApiResponse.Error("User not authenticated", 401));

            // Get all courses
            var courses = await _context.Courses
                .Include(c => c.Tutor)
                .Include(c => c.Reviews)
                .ToListAsync();

            // Get courses the student is already enrolled in
            var enrolledCourseIds = await _context.Enrollments
                .Where(e => e.UserId == userId)
                .Select(e => e.CourseId)
                .ToListAsync();

            // Filter out courses the student is already enrolled in
            var availableCourses = courses
                .Where(c => !enrolledCourseIds.Contains(c.Id))
                .Select(c => new CourseDto
                {
                    Id = c.Id,
                    Title = c.Title,
                    Description = c.Description,
                    TutorId = c.TutorId,
                    TutorName = c.Tutor?.FullName,
                    EnrollmentCount = c.Enrollments?.Count ?? 0,
                    AverageRating = c.Reviews != null && c.Reviews.Any() 
                        ? c.Reviews.Average(r => r.Rating) 
                        : 0
                }).ToList();

            return Ok(ApiResponse<List<CourseDto>>.Success(availableCourses, "Available courses retrieved successfully", 200));
        }

        // GET api/students/enrollments - Get all enrollments for the current student
        [HttpGet("enrollments")]
        [Authorize(Roles = "Student")]
        public async Task<IActionResult> GetEnrollments()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
                return Unauthorized(ApiResponse.Error("User not authenticated", 401));

            var enrollments = await _context.Enrollments
                .Include(e => e.Course)
                .ThenInclude(c => c.Tutor)
                .Where(e => e.UserId == userId)
                .ToListAsync();

            var enrollmentDtos = enrollments.Select(e => new EnrollmentDto
            {
                Id = e.Id,
                UserId = e.UserId,
                CourseId = e.CourseId,
                CourseTitle = e.Course?.Title,
                UserName = e.Course?.Tutor?.FullName
            }).ToList();

            return Ok(ApiResponse<List<EnrollmentDto>>.Success(enrollmentDtos, "Enrollments retrieved successfully", 200));
        }
    }
}
