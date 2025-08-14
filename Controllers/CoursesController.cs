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
    [Route("api/courses")]
    [ApiController]
    public class CoursesController : ControllerBase
    {
        private readonly DataContext _context;
        private readonly UserManager<User> _userManager;

        public CoursesController(DataContext context, UserManager<User> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // GET: api/courses - List all courses (public)
        [HttpGet]
        public async Task<IActionResult> GetCourses()
        {
            var courses = await _context.Courses
                .Include(c => c.Tutor)
                .Include(c => c.Enrollments)
                .Include(c => c.Reviews)
                .ToListAsync();

            var courseDtos = courses.Select(c => new CourseDto
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

            return Ok(ApiResponse<List<CourseDto>>.Success(courseDtos, "Courses retrieved successfully", 200));
        }

        // GET api/courses/5 - Get course details by ID (public)
        [HttpGet("{id}")]
        public async Task<IActionResult> GetCourse(int id)
        {
            var course = await _context.Courses
                .Include(c => c.Tutor)
                .Include(c => c.Enrollments)
                .Include(c => c.Reviews)
                .ThenInclude(r => r.User)
                .FirstOrDefaultAsync(c => c.Id == id);

            if (course == null)
                return NotFound(ApiResponse.Error("Course not found", 404));

            var reviewDtos = course.Reviews?.Select(r => new ReviewDto
            {
                Id = r.Id,
                Content = r.Content,
                Rating = r.Rating,
                UserId = r.UserId,
                UserName = r.User?.FullName,
                CourseId = r.CourseId,
                CourseTitle = course.Title
            }).ToList() ?? new List<ReviewDto>();

            var courseDetailDto = new CourseDetailDto
            {
                Id = course.Id,
                Title = course.Title,
                Description = course.Description,
                TutorId = course.TutorId,
                TutorName = course.Tutor?.FullName,
                EnrollmentCount = course.Enrollments?.Count ?? 0,
                AverageRating = course.Reviews != null && course.Reviews.Any() 
                    ? course.Reviews.Average(r => r.Rating) 
                    : 0,
                Reviews = reviewDtos
            };

            return Ok(ApiResponse<CourseDetailDto>.Success(courseDetailDto, "Course retrieved successfully", 200));
        }

        // POST api/courses - Create a new course (Tutor/Admin only)
        [HttpPost]
        [Authorize(Roles = "Tutor,Admin")]
        public async Task<IActionResult> CreateCourse([FromBody] CourseCreateDto courseDto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ApiResponse.Error("Invalid course data", 400));

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
                return Unauthorized(ApiResponse.Error("User not authenticated", 401));

            var course = new Course
            {
                Title = courseDto.Title,
                Description = courseDto.Description,
                TutorId = userId
            };

            _context.Courses.Add(course);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetCourse), new { id = course.Id }, 
                ApiResponse<Course>.Success(course, "Course created successfully", 201));
        }

        // PUT api/courses/5 - Update a course (Tutor who owns the course or Admin)
        [HttpPut("{id}")]
        [Authorize(Roles = "Tutor,Admin")]
        public async Task<IActionResult> UpdateCourse(int id, [FromBody] CourseUpdateDto courseDto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ApiResponse.Error("Invalid course data", 400));

            var course = await _context.Courses.FindAsync(id);
            if (course == null)
                return NotFound(ApiResponse.Error("Course not found", 404));

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var userRole = User.FindFirstValue(ClaimTypes.Role);

            // Check if user is the tutor who owns the course or an admin
            if (course.TutorId != userId && userRole != "Admin")
                return Forbid(ApiResponse.Error("You don't have permission to update this course", 403).ToString());

            course.Title = courseDto.Title;
            course.Description = courseDto.Description;

            await _context.SaveChangesAsync();

            return Ok(ApiResponse.Success("Course updated successfully", 200));
        }

        // DELETE api/courses/5 - Delete a course (Tutor who owns the course or Admin)
        [HttpDelete("{id}")]
        [Authorize(Roles = "Tutor,Admin")]
        public async Task<IActionResult> DeleteCourse(int id)
        {
            var course = await _context.Courses.FindAsync(id);
            if (course == null)
                return NotFound(ApiResponse.Error("Course not found", 404));

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var userRole = User.FindFirstValue(ClaimTypes.Role);

            // Check if user is the tutor who owns the course or an admin
            if (course.TutorId != userId && userRole != "Admin")
                return Forbid(ApiResponse.Error("You don't have permission to delete this course", 403).ToString());

            _context.Courses.Remove(course);
            await _context.SaveChangesAsync();

            return Ok(ApiResponse.Success("Course deleted successfully", 200));
        }
    }
}
