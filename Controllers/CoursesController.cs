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
                Subtitle = c.Subtitle,
                Description = c.Description,
                Category = c.Category,
                Level = c.Level,
                Language = c.Language,
                Tags = c.Tags,
                Features = c.Features,
                TutorId = c.TutorId,
                TutorName = c.Tutor?.FullName,
                EnrollmentCount = c.Enrollments?.Count ?? 0,
                AverageRating = c.Reviews != null && c.Reviews.Any() 
                    ? c.Reviews.Average(r => r.Rating) 
                    : 0,
                Price = c.Price,
                Status = c.Status,
                CreatedAt = c.CreatedAt,
                UpdatedAt = c.UpdatedAt,
                VideoCoverImageUrl = c.VideoCoverImageUrl,
                VideoContentUrl = c.VideoContentUrl,
                VideoDurationMinutes = c.VideoDurationMinutes
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
                .Include(c => c.CourseSections)
                .ThenInclude(s => s.Contents)
                .Include(c => c.CourseContents.Where(cc => cc.SectionId == null))
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
                CourseTitle = course.Title,
                CreatedAt = r.CreatedAt
            }).ToList() ?? new List<ReviewDto>();

            var sectionDtos = course.CourseSections?.OrderBy(s => s.Order).Select(s => new CourseSectionDto
            {
                Id = s.Id,
                Title = s.Title,
                Order = s.Order,
                Contents = s.Contents.OrderBy(cc => cc.Order).Select(cc => new CourseContentDto
                {
                    Id = cc.Id,
                    Title = cc.Title,
                    ContentType = cc.ContentType,
                    Duration = cc.Duration,
                    Order = cc.Order,
                    IsCompleted = false,
                    PublicId = cc.PublicId,
                    SecureUrl = cc.SecureUrl
                }).ToList()
            }).ToList() ?? new List<CourseSectionDto>();

            var unassignedContentDtos = course.CourseContents?
                .Where(cc => cc.SectionId == null)
                .OrderBy(cc => cc.Order)
                .Select(cc => new CourseContentDto
                {
                    Id = cc.Id,
                    Title = cc.Title,
                    ContentType = cc.ContentType,
                    Duration = cc.Duration,
                    Order = cc.Order,
                    IsCompleted = false,
                    PublicId = cc.PublicId,
                    SecureUrl = cc.SecureUrl
                }).ToList() ?? new List<CourseContentDto>();

            var courseDetailDto = new CourseDetailDto
            {
                Id = course.Id,
                Title = course.Title,
                Subtitle = course.Subtitle,
                Description = course.Description,
                Category = course.Category,
                Level = course.Level,
                Language = course.Language,
                Tags = course.Tags,
                Features = course.Features,
                TutorId = course.TutorId,
                TutorName = course.Tutor?.FullName,
                EnrollmentCount = course.Enrollments?.Count ?? 0,
                AverageRating = course.Reviews != null && course.Reviews.Any() 
                    ? course.Reviews.Average(r => r.Rating) 
                    : 0,
                Price = course.Price,
                Status = course.Status,
                CreatedAt = course.CreatedAt,
                UpdatedAt = course.UpdatedAt,
                VideoCoverImageUrl = course.VideoCoverImageUrl,
                VideoContentUrl = course.VideoContentUrl,
                VideoDurationMinutes = course.VideoDurationMinutes,
                Reviews = reviewDtos,
                Sections = sectionDtos,
                Contents = unassignedContentDtos
            };

            return Ok(ApiResponse<CourseDetailDto>.Success(courseDetailDto, "Course retrieved successfully", 200));
        }

        // POST api/courses/{id}/sections - Create a new section
        [HttpPost("{id}/sections")]
        [Authorize(Roles = "Tutor,Admin")]
        public async Task<IActionResult> CreateSection(int id, [FromBody] CourseSectionCreateDto dto)
        {
            var course = await _context.Courses.FindAsync(id);
            if (course == null) return NotFound(ApiResponse.Error("Course not found", 404));

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var userRole = User.FindFirstValue(ClaimTypes.Role);
            if (course.TutorId != userId && userRole != "Admin") return Forbid();

            var section = new CourseSection
            {
                CourseId = id,
                Title = dto.Title,
                Order = dto.Order
            };

            _context.CourseSections.Add(section);
            await _context.SaveChangesAsync();

            return Ok(ApiResponse<CourseSection>.Success(section, "Section created successfully", 201));
        }

        [HttpPut("{courseId}/sections/{sectionId}")]
        [Authorize(Roles = "Tutor,Admin")]
        public async Task<IActionResult> UpdateSection(int courseId, int sectionId, [FromBody] CourseSectionCreateDto dto)
        {
            var section = await _context.CourseSections.FirstOrDefaultAsync(s => s.Id == sectionId && s.CourseId == courseId);
            if (section == null) return NotFound(ApiResponse.Error("Section not found", 404));

            var course = await _context.Courses.FindAsync(courseId);
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var userRole = User.FindFirstValue(ClaimTypes.Role);
            if (course.TutorId != userId && userRole != "Admin") return Forbid();

            section.Title = dto.Title;
            section.Order = dto.Order;

            await _context.SaveChangesAsync();
            return Ok(ApiResponse<CourseSection>.Success(section, "Section updated successfully", 200));
        }

        [HttpDelete("{courseId}/sections/{sectionId}")]
        [Authorize(Roles = "Tutor,Admin")]
        public async Task<IActionResult> DeleteSection(int courseId, int sectionId)
        {
            var section = await _context.CourseSections.FirstOrDefaultAsync(s => s.Id == sectionId && s.CourseId == courseId);
            if (section == null) return NotFound(ApiResponse.Error("Section not found", 404));

            var course = await _context.Courses.FindAsync(courseId);
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var userRole = User.FindFirstValue(ClaimTypes.Role);
            if (course.TutorId != userId && userRole != "Admin") return Forbid();

            _context.CourseSections.Remove(section);
            await _context.SaveChangesAsync();
            return Ok(ApiResponse.Success("Section deleted successfully", 200));
        }

        // PATCH api/courses/{id}/status - Update a course's status (Admin only)
        [HttpPatch("{id}/status")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> UpdateCourseStatus(int id, [FromBody] UpdateCourseStatusDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ApiResponse.Error("Invalid status payload", 400));

            if (dto.Status != CourseStatus.Approved && dto.Status != CourseStatus.Rejected)
                return BadRequest(ApiResponse.Error("Status must be Approved or Rejected", 400));

            var course = await _context.Courses
                .Include(c => c.Tutor)
                .Include(c => c.Enrollments)
                .Include(c => c.Reviews)
                .FirstOrDefaultAsync(c => c.Id == id);

            if (course == null)
                return NotFound(ApiResponse.Error("Course not found", 404));

            course.Status = dto.Status;
            course.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            var courseDto = new CourseDto
            {
                Id = course.Id,
                Title = course.Title,
                Subtitle = course.Subtitle,
                Description = course.Description,
                Category = course.Category,
                Level = course.Level,
                Language = course.Language,
                Tags = course.Tags,
                Features = course.Features,
                TutorId = course.TutorId,
                TutorName = course.Tutor?.FullName,
                EnrollmentCount = course.Enrollments?.Count ?? 0,
                AverageRating = course.Reviews != null && course.Reviews.Any() ? course.Reviews.Average(r => r.Rating) : 0,
                Price = course.Price,
                Status = course.Status,
                CreatedAt = course.CreatedAt,
                UpdatedAt = course.UpdatedAt,
                VideoCoverImageUrl = course.VideoCoverImageUrl,
                VideoContentUrl = course.VideoContentUrl,
                VideoDurationMinutes = course.VideoDurationMinutes
            };

            return Ok(ApiResponse<CourseDto>.Success(courseDto, "Course status updated successfully", 200));
        }

        // POST api/courses/{id}/contents - Add content to a course (Tutor/Admin)
        [HttpPost("{id}/contents")]
        [Authorize(Roles = "Tutor,Admin")]
        public async Task<IActionResult> AddContent(int id, [FromBody] CourseContentCreateDto contentDto)
        {
            try
            {
                var course = await _context.Courses.FindAsync(id);
                if (course == null) return NotFound(ApiResponse.Error("Course not found", 404));

                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                var userRole = User.FindFirstValue(ClaimTypes.Role);
                if (course.TutorId != userId && userRole != "Admin") return Forbid();

                string resourceType = "video";
                if (contentDto.ContentType == "pdf" || contentDto.ContentType == "document")
                {
                    resourceType = "image";
                }
                
                var secureUrl = _cloudinaryService.GetSecureUrl(contentDto.PublicId, resourceType);

                var content = new CourseContent
                {
                    CourseId = id,
                    SectionId = contentDto.SectionId,
                    Title = contentDto.Title,
                    PublicId = contentDto.PublicId,
                    ContentType = contentDto.ContentType,
                    Duration = contentDto.Duration,
                    Order = contentDto.Order,
                    SecureUrl = secureUrl
                };

                _context.CourseContents.Add(content);
                await _context.SaveChangesAsync();

                var responseDto = new CourseContentResponseDto
                {
                    Id = content.Id,
                    Title = content.Title,
                    PublicId = content.PublicId,
                    SecureUrl = content.SecureUrl,
                    ContentType = content.ContentType,
                    Duration = content.Duration,
                    Order = content.Order
                };

                return Ok(ApiResponse<CourseContentResponseDto>.Success(responseDto, "Content added successfully", 201));
            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResponse.Error($"Internal Server Error: {ex.Message}", 500));
            }
        }

        // PUT api/courses/{courseId}/contents/{contentId} - Update content (Tutor/Admin)
        [HttpPut("{courseId}/contents/{contentId}")]
        [Authorize(Roles = "Tutor,Admin")]
        public async Task<IActionResult> UpdateContent(int courseId, int contentId, [FromBody] CourseContentUpdateDto contentDto)
        {
            var content = await _context.CourseContents.FirstOrDefaultAsync(c => c.Id == contentId && c.CourseId == courseId);
            if (content == null) return NotFound(ApiResponse.Error("Content not found", 404));

            var course = await _context.Courses.FindAsync(courseId);
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var userRole = User.FindFirstValue(ClaimTypes.Role);
            if (course.TutorId != userId && userRole != "Admin") return Forbid();

            content.Title = contentDto.Title;
            content.Order = contentDto.Order;
            content.SectionId = contentDto.SectionId;
            
            if (!string.IsNullOrEmpty(contentDto.PublicId))
            {
                content.PublicId = contentDto.PublicId;
                content.ContentType = contentDto.ContentType;
                content.Duration = contentDto.Duration;
                
                string resourceType = "video";
                if (content.ContentType == "pdf" || content.ContentType == "document")
                {
                    resourceType = "image";
                }
                content.SecureUrl = _cloudinaryService.GetSecureUrl(content.PublicId, resourceType);
            }

            await _context.SaveChangesAsync();

            var responseDto = new CourseContentResponseDto
            {
                Id = content.Id,
                Title = content.Title,
                PublicId = content.PublicId,
                SecureUrl = content.SecureUrl,
                ContentType = content.ContentType,
                Duration = content.Duration,
                Order = content.Order
            };

            return Ok(ApiResponse<CourseContentResponseDto>.Success(responseDto, "Content updated successfully", 200));
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
                Subtitle = courseDto.Subtitle,
                Description = courseDto.Description,
                Category = courseDto.Category,
                Level = courseDto.Level,
                Language = courseDto.Language,
                Tags = courseDto.Tags,
                Features = courseDto.Features,
                TutorId = userId,
                Price = courseDto.Price,
                VideoCoverImageUrl = courseDto.VideoCoverImageUrl,
                VideoContentUrl = courseDto.VideoContentUrl,
                VideoDurationMinutes = courseDto.videoDurationMinutes,
                Status = CourseStatus.Pending 
            };

            _context.Courses.Add(course);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetCourse), new { id = course.Id }, 
                ApiResponse<Course>.Success(course, "Course created successfully", 201));
        }

        // PUT api/courses/5 - Update a course
        [HttpPut("{id}")]
        [Authorize(Roles = "Tutor,Admin")]
        public async Task<IActionResult> UpdateCourse(int id, [FromBody] CourseUpdateDto courseDto)
        {
            var course = await _context.Courses.FindAsync(id);
            if (course == null) return NotFound(ApiResponse.Error("Course not found", 404));

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var userRole = User.FindFirstValue(ClaimTypes.Role);
            if (course.TutorId != userId && userRole != "Admin") return Forbid();

            course.Title = courseDto.Title;
            course.Subtitle = courseDto.Subtitle;
            course.Description = courseDto.Description;
            course.Category = courseDto.Category;
            course.Level = courseDto.Level;
            course.Language = courseDto.Language;
            course.Tags = courseDto.Tags;
            course.Features = courseDto.Features;
            course.Price = courseDto.Price;
            course.VideoCoverImageUrl = courseDto.VideoCoverImageUrl;
            course.VideoContentUrl = courseDto.VideoContentUrl;
            course.VideoDurationMinutes = courseDto.videoDurationMinutes;

            await _context.SaveChangesAsync();
            return Ok(ApiResponse.Success("Course updated successfully", 200));
        }

        // DELETE api/courses/5
        [HttpDelete("{id}")]
        [Authorize(Roles = "Tutor,Admin")]
        public async Task<IActionResult> DeleteCourse(int id)
        {
            var course = await _context.Courses.FindAsync(id);
            if (course == null) return NotFound(ApiResponse.Error("Course not found", 404));

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var userRole = User.FindFirstValue(ClaimTypes.Role);
            if (course.TutorId != userId && userRole != "Admin") return Forbid();

            _context.Courses.Remove(course);
            await _context.SaveChangesAsync();
            return Ok(ApiResponse.Success("Course deleted successfully", 200));
        }
    }
}
