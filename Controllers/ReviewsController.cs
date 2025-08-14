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
    [Route("api/reviews")]
    [ApiController]
    public class ReviewsController : ControllerBase
    {
        private readonly DataContext _context;
        private readonly UserManager<User> _userManager;

        public ReviewsController(DataContext context, UserManager<User> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // GET: api/reviews - Get all reviews (public)
        [HttpGet]
        public async Task<IActionResult> GetReviews()
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

        // GET api/reviews/course/{courseId} - Get reviews for a specific course
        [HttpGet("course/{courseId}")]
        public async Task<IActionResult> GetCourseReviews(int courseId)
        {
            var course = await _context.Courses.FindAsync(courseId);
            if (course == null)
                return NotFound(ApiResponse.Error("Course not found", 404));

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

        // GET api/reviews/user - Get reviews by the current user
        [HttpGet("user")]
        [Authorize]
        public async Task<IActionResult> GetUserReviews()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
                return Unauthorized(ApiResponse.Error("User not authenticated", 401));

            var reviews = await _context.Reviews
                .Include(r => r.Course)
                .Where(r => r.UserId == userId)
                .ToListAsync();

            var reviewDtos = reviews.Select(r => new ReviewDto
            {
                Id = r.Id,
                Content = r.Content,
                Rating = r.Rating,
                UserId = r.UserId,
                CourseId = r.CourseId,
                CourseTitle = r.Course?.Title
            }).ToList();

            return Ok(ApiResponse<List<ReviewDto>>.Success(reviewDtos, "User reviews retrieved successfully", 200));
        }

        // POST api/reviews - Create a review (authenticated users only)
        [HttpPost]
        [Authorize]
        public async Task<IActionResult> CreateReview([FromBody] ReviewCreateDto reviewDto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ApiResponse.Error("Invalid review data", 400));

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
                return Unauthorized(ApiResponse.Error("User not authenticated", 401));

            // Check if course exists
            var course = await _context.Courses.FindAsync(reviewDto.CourseId);
            if (course == null)
                return NotFound(ApiResponse.Error("Course not found", 404));

            // Check if user is enrolled in the course
            var isEnrolled = await _context.Enrollments
                .AnyAsync(e => e.UserId == userId && e.CourseId == reviewDto.CourseId);

            if (!isEnrolled)
                return BadRequest(ApiResponse.Error("You must be enrolled in the course to leave a review", 400));

            // Check if user has already reviewed this course
            var existingReview = await _context.Reviews
                .FirstOrDefaultAsync(r => r.UserId == userId && r.CourseId == reviewDto.CourseId);

            if (existingReview != null)
                return BadRequest(ApiResponse.Error("You have already reviewed this course", 400));

            // Validate rating
            if (reviewDto.Rating < 1 || reviewDto.Rating > 5)
                return BadRequest(ApiResponse.Error("Rating must be between 1 and 5", 400));

            var review = new Review
            {
                Content = reviewDto.Content,
                Rating = reviewDto.Rating,
                UserId = userId,
                CourseId = reviewDto.CourseId
            };

            _context.Reviews.Add(review);
            await _context.SaveChangesAsync();

            var reviewResponse = new ReviewDto
            {
                Id = review.Id,
                Content = review.Content,
                Rating = review.Rating,
                UserId = review.UserId,
                CourseId = review.CourseId,
                CourseTitle = course.Title
            };

            return CreatedAtAction(nameof(GetCourseReviews), new { courseId = review.CourseId },
                ApiResponse<ReviewDto>.Success(reviewResponse, "Review created successfully", 201));
        }

        // PUT api/reviews/{id} - Update a review (owner or admin only)
        [HttpPut("{id}")]
        [Authorize]
        public async Task<IActionResult> UpdateReview(int id, [FromBody] ReviewUpdateDto reviewDto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ApiResponse.Error("Invalid review data", 400));

            var review = await _context.Reviews.FindAsync(id);
            if (review == null)
                return NotFound(ApiResponse.Error("Review not found", 404));

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var userRole = User.FindFirstValue(ClaimTypes.Role);

            // Check if user is the owner of the review or an admin
            if (review.UserId != userId && userRole != "Admin")
                return Forbid(ApiResponse.Error("You don't have permission to update this review", 403).ToString());

            // Validate rating
            if (reviewDto.Rating < 1 || reviewDto.Rating > 5)
                return BadRequest(ApiResponse.Error("Rating must be between 1 and 5", 400));

            review.Content = reviewDto.Content;
            review.Rating = reviewDto.Rating;

            await _context.SaveChangesAsync();

            return Ok(ApiResponse.Success("Review updated successfully", 200));
        }

        // DELETE api/reviews/{id} - Delete a review (owner or admin only)
        [HttpDelete("{id}")]
        [Authorize]
        public async Task<IActionResult> DeleteReview(int id)
        {
            var review = await _context.Reviews.FindAsync(id);
            if (review == null)
                return NotFound(ApiResponse.Error("Review not found", 404));

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var userRole = User.FindFirstValue(ClaimTypes.Role);

            // Check if user is the owner of the review or an admin
            if (review.UserId != userId && userRole != "Admin")
                return Forbid(ApiResponse.Error("You don't have permission to delete this review", 403).ToString());

            _context.Reviews.Remove(review);
            await _context.SaveChangesAsync();

            return Ok(ApiResponse.Success("Review deleted successfully", 200));
        }
    }
}
