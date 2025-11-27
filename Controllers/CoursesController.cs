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
using Course_management.Interfaces;
using Microsoft.AspNetCore.Http;

namespace Course_management.Controllers
{
    [Route("api/courses")]
    [ApiController]
    public class CoursesController : ControllerBase
    {
        private readonly DataContext _context;
        private readonly UserManager<User> _userManager;
        private readonly ICloudinaryService _cloudinaryService;

        public CoursesController(DataContext context, UserManager<User> userManager, ICloudinaryService cloudinaryService)
        {
            _context = context;
            _userManager = userManager;
            _cloudinaryService = cloudinaryService;
        }

        // GET: api/courses - List all courses (public)
        [HttpGet]
        public async Task<IActionResult> GetCourses()
        {
            var courses = await _context.Courses
                .Include(c => c.Tutor)
                .Include(c => c.Enrollments)
                .Include(c => c.Reviews)
                .Where(c => c.Status == CourseStatus.Approved) // Only show approved courses
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

        [HttpPost("{id}/content")]
        [Authorize(Roles = "Tutor")]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> UploadCourseContent(int id, [FromForm] UploadCourseContentDto dto)
        {
            var course = await _context.Courses.FindAsync(id);
            if (course == null) return NotFound(ApiResponse.Error("Course not found", 404));

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (course.TutorId != userId)
            {
                return Forbid();
            }

            if (dto.File == null || dto.File.Length == 0)
            {
                return BadRequest(ApiResponse.Error("No file uploaded or file is empty", 400));
            }

            var uploadResult = await _cloudinaryService.UploadVideoAsync(dto.File!);

            var courseContent = new CourseContent
            {
                CourseId = id,
                Title = dto.Title,
                PublicId = uploadResult.PublicId,
                SecureUrl = uploadResult.SecureUrl.ToString(),
                ContentType = dto.ContentType,
                Duration = uploadResult.Duration,
                Order = dto.Order
            };

            _context.CourseContents.Add(courseContent);
            await _context.SaveChangesAsync();

            return Ok(ApiResponse<CourseContent>.Success(courseContent, "Content uploaded successfully", 200));
        }

        [HttpGet("{id}/stream")]
        [Authorize]
        public async Task<IActionResult> GetStreamUrl(int id)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var isEnrolled = await _context.Enrollments.AnyAsync(e => e.CourseId == id && e.UserId == userId);

            if (!isEnrolled)
            {
                return Forbid();
            }

            var courseContent = await _context.CourseContents.FirstOrDefaultAsync(cc => cc.CourseId == id && cc.ContentType == "video");
            if (courseContent == null)
            {
                return NotFound(ApiResponse.Error("No video content found for this course", 404));
            }

            var secureUrl = _cloudinaryService.GetSecureVideoUrl(courseContent.PublicId);

            return Ok(ApiResponse<string>.Success(secureUrl, "Secure stream URL generated", 200));
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
                TutorId = userId,
                Price = courseDto.Price,
                VideoCoverImageUrl = courseDto.VideoCoverImageUrl,
                VideoContentUrl = courseDto.VideoContentUrl,
                VideoDurationMinutes = courseDto.VideoDurationMinutes,
                Status = CourseStatus.Pending // Default status
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
            course.Price = courseDto.Price;
            course.VideoCoverImageUrl = courseDto.VideoCoverImageUrl;
            course.VideoContentUrl = courseDto.VideoContentUrl;
            course.VideoDurationMinutes = courseDto.VideoDurationMinutes;

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
