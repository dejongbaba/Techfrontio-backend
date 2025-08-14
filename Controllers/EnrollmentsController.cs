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
    [Route("api/enrollments")]
    [ApiController]
    [Authorize] // All enrollment endpoints require authentication
    public class EnrollmentsController : ControllerBase
    {
        private readonly DataContext _context;
        private readonly UserManager<User> _userManager;

        public EnrollmentsController(DataContext context, UserManager<User> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // GET: api/enrollments - Get all enrollments for the current user
        [HttpGet]
        public async Task<IActionResult> GetUserEnrollments()
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

        // GET: api/enrollments/course/{courseId} - Get all enrollments for a specific course (Admin/Tutor only)
        [HttpGet("course/{courseId}")]
        [Authorize(Roles = "Admin,Tutor")]
        public async Task<IActionResult> GetCourseEnrollments(int courseId)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var userRole = User.FindFirstValue(ClaimTypes.Role);

            // Check if course exists
            var course = await _context.Courses.FindAsync(courseId);
            if (course == null)
                return NotFound(ApiResponse.Error("Course not found", 404));

            // If user is a tutor, check if they own the course
            if (userRole == "Tutor" && course.TutorId != userId)
                return Forbid(ApiResponse.Error("You don't have permission to view enrollments for this course", 403).ToString());

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

        // POST api/enrollments - Enroll in a course (Student only)
        [HttpPost]
        [Authorize(Roles = "Student")]
        public async Task<IActionResult> EnrollInCourse([FromBody] EnrollmentCreateDto enrollmentDto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ApiResponse.Error("Invalid enrollment data", 400));

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
                return Unauthorized(ApiResponse.Error("User not authenticated", 401));

            // Check if course exists
            var course = await _context.Courses.FindAsync(enrollmentDto.CourseId);
            if (course == null)
                return NotFound(ApiResponse.Error("Course not found", 404));

            // Check if user is already enrolled
            var existingEnrollment = await _context.Enrollments
                .FirstOrDefaultAsync(e => e.UserId == userId && e.CourseId == enrollmentDto.CourseId);

            if (existingEnrollment != null)
                return BadRequest(ApiResponse.Error("You are already enrolled in this course", 400));

            var enrollment = new Enrollment
            {
                UserId = userId,
                CourseId = enrollmentDto.CourseId
            };

            _context.Enrollments.Add(enrollment);
            await _context.SaveChangesAsync();

            var enrollmentResponse = new EnrollmentDto
            {
                Id = enrollment.Id,
                UserId = enrollment.UserId,
                CourseId = enrollment.CourseId,
                CourseTitle = course.Title
            };

            return CreatedAtAction(nameof(GetUserEnrollments), 
                ApiResponse<EnrollmentDto>.Success(enrollmentResponse, "Successfully enrolled in course", 201));
        }

        // DELETE api/enrollments/{id} - Unenroll from a course
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteEnrollment(int id)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var userRole = User.FindFirstValue(ClaimTypes.Role);

            var enrollment = await _context.Enrollments.FindAsync(id);
            if (enrollment == null)
                return NotFound(ApiResponse.Error("Enrollment not found", 404));

            // Students can only delete their own enrollments, Admins can delete any
            if (enrollment.UserId != userId && userRole != "Admin")
                return Forbid(ApiResponse.Error("You don't have permission to delete this enrollment", 403).ToString());

            _context.Enrollments.Remove(enrollment);
            await _context.SaveChangesAsync();

            return Ok(ApiResponse.Success("Successfully unenrolled from course", 200));
        }
    }
}