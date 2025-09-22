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

        // GET: api/students/dashboard - Get enhanced student dashboard data
        [HttpGet("dashboard")]
        [Authorize(Roles = "Student")]
        public async Task<IActionResult> GetDashboard()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
                return Unauthorized(ApiResponse.Error("User not authenticated", 401));

            // Get enrolled courses with progress
            var enrollments = await _context.Enrollments
                .Include(e => e.Course)
                .ThenInclude(c => c.Tutor)
                .Where(e => e.UserId == userId)
                .ToListAsync();

            // Get course progress for enrolled courses
            var courseProgresses = await _context.CourseProgresses
                .Where(cp => cp.UserId == userId)
                .ToListAsync();

            // Get pending tasks
            var pendingTasks = await _context.TaskSubmissions
                .Include(ts => ts.Task)
                .ThenInclude(t => t.Course)
                .Where(ts => ts.StudentId == userId && ts.Status == "Draft")
                .CountAsync();

            // Get upcoming tasks (due within 7 days)
            var upcomingTasks = await _context.CourseTasks
                .Include(t => t.Course)
                .Where(t => t.Course.Enrollments.Any(e => e.UserId == userId) && 
                           t.DueDate > DateTime.UtcNow && 
                           t.DueDate <= DateTime.UtcNow.AddDays(7) &&
                           t.IsActive)
                .Take(5)
                .Select(t => new CourseTaskDto
                {
                    Id = t.Id,
                    Title = t.Title,
                    CourseTitle = t.Course.Title,
                    DueDate = t.DueDate,
                    MaxPoints = t.MaxPoints
                })
                .ToListAsync();

            // Get recent progress
            var recentProgress = courseProgresses
                .OrderByDescending(cp => cp.LastWatchedAt)
                .Take(5)
                .Select(cp => new CourseProgressDto
                {
                    Id = cp.Id,
                    CourseId = cp.CourseId,
                    CourseTitle = enrollments.FirstOrDefault(e => e.CourseId == cp.CourseId)?.Course?.Title,
                    WatchedMinutes = cp.WatchedMinutes,
                    TotalMinutes = enrollments.FirstOrDefault(e => e.CourseId == cp.CourseId)?.Course?.VideoDurationMinutes ?? 0,
                    ProgressPercentage = cp.ProgressPercentage,
                    LastWatchedAt = cp.LastWatchedAt,
                    IsCompleted = cp.IsCompleted
                })
                .ToList();

            var dashboardData = new StudentDashboardDto
            {
                TotalEnrolledCourses = enrollments.Count,
                CompletedCourses = courseProgresses.Count(cp => cp.IsCompleted),
                InProgressCourses = courseProgresses.Count(cp => !cp.IsCompleted),
                PendingTasks = pendingTasks,
                RecentProgress = recentProgress,
                UpcomingTasks = upcomingTasks
            };

            return Ok(ApiResponse<StudentDashboardDto>.Success(dashboardData, "Dashboard data retrieved successfully", 200));
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

        // GET api/students/courses/{id} - Get detailed course information with progress
        [HttpGet("courses/{id}")]
        [Authorize(Roles = "Student")]
        public async Task<IActionResult> GetCourseDetails(int id)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
                return Unauthorized(ApiResponse.Error("User not authenticated", 401));

            // Check if student is enrolled in the course
            var enrollment = await _context.Enrollments
                .FirstOrDefaultAsync(e => e.UserId == userId && e.CourseId == id);

            if (enrollment == null)
                return Forbid("You are not enrolled in this course");

            var course = await _context.Courses
                .Include(c => c.Tutor)
                .Include(c => c.Reviews)
                .FirstOrDefaultAsync(c => c.Id == id);

            if (course == null)
                return NotFound(ApiResponse.Error("Course not found", 404));

            // Get student's progress for this course
            var progress = await _context.CourseProgresses
                .FirstOrDefaultAsync(cp => cp.UserId == userId && cp.CourseId == id);

            // Get tasks for this course
            var tasks = await _context.CourseTasks
                .Include(t => t.TaskSubmissions.Where(ts => ts.StudentId == userId))
                .Where(t => t.CourseId == id && t.IsActive)
                .Select(t => new CourseTaskDto
                {
                    Id = t.Id,
                    Title = t.Title,
                    Description = t.Description,
                    DueDate = t.DueDate,
                    MaxPoints = t.MaxPoints,
                    AllowAttachments = t.AllowAttachments,
                    Instructions = t.Instructions,
                    CreatedAt = t.CreatedAt,
                    StudentSubmission = t.TaskSubmissions.FirstOrDefault() != null ? new TaskSubmissionDto
                    {
                        Id = t.TaskSubmissions.First().Id,
                        Status = t.TaskSubmissions.First().Status,
                        SubmissionText = t.TaskSubmissions.First().SubmissionText,
                        PointsEarned = t.TaskSubmissions.First().PointsEarned,
                        TutorFeedback = t.TaskSubmissions.First().TutorFeedback,
                        SubmittedAt = t.TaskSubmissions.First().SubmittedAt,
                        GradedAt = t.TaskSubmissions.First().GradedAt
                    } : null
                })
                .ToListAsync();

            var courseDetail = new CourseDetailDto
            {
                Id = course.Id,
                Title = course.Title,
                Description = course.Description,
                TutorId = course.TutorId,
                TutorName = course.Tutor?.FullName,
                VideoCoverImageUrl = course.VideoCoverImageUrl,
                VideoContentUrl = course.VideoContentUrl,
                VideoDurationMinutes = course.VideoDurationMinutes,
                Progress = progress != null ? new CourseProgressDto
                {
                    Id = progress.Id,
                    WatchedMinutes = progress.WatchedMinutes,
                    TotalMinutes = course.VideoDurationMinutes ?? 0,
                    ProgressPercentage = progress.ProgressPercentage,
                    LastWatchedPosition = progress.LastWatchedPosition,
                    LastWatchedAt = progress.LastWatchedAt,
                    IsCompleted = progress.IsCompleted,
                    CompletedAt = progress.CompletedAt
                } : null,
                Tasks = tasks
            };

            return Ok(ApiResponse<CourseDetailDto>.Success(courseDetail, "Course details retrieved successfully", 200));
        }

        // POST api/students/progress - Update course progress
        [HttpPost("progress")]
        [Authorize(Roles = "Student")]
        public async Task<IActionResult> UpdateProgress([FromBody] UpdateProgressDto updateDto)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
                return Unauthorized(ApiResponse.Error("User not authenticated", 401));

            // Check if student is enrolled in the course
            var enrollment = await _context.Enrollments
                .FirstOrDefaultAsync(e => e.UserId == userId && e.CourseId == updateDto.CourseId);

            if (enrollment == null)
                return Forbid("You are not enrolled in this course");

            var course = await _context.Courses.FindAsync(updateDto.CourseId);
            if (course == null)
                return NotFound(ApiResponse.Error("Course not found", 404));

            // Get or create progress record
            var progress = await _context.CourseProgresses
                .FirstOrDefaultAsync(cp => cp.UserId == userId && cp.CourseId == updateDto.CourseId);

            if (progress == null)
            {
                progress = new CourseProgress
                {
                    UserId = userId,
                    CourseId = updateDto.CourseId,
                    StartedAt = DateTime.UtcNow,
                    CreatedAt = DateTime.UtcNow
                };
                _context.CourseProgresses.Add(progress);
            }

            // Update progress
            progress.WatchedMinutes = updateDto.WatchedMinutes;
            progress.LastWatchedPosition = updateDto.LastWatchedPosition;
            progress.LastWatchedAt = DateTime.UtcNow;
            progress.UpdatedAt = DateTime.UtcNow;

            // Calculate progress percentage
            if (course.VideoDurationMinutes.HasValue && course.VideoDurationMinutes > 0)
            {
                progress.ProgressPercentage = Math.Min(100, (decimal)updateDto.WatchedMinutes / course.VideoDurationMinutes.Value * 100);
                
                // Mark as completed if watched 95% or more
                if (progress.ProgressPercentage >= 95 && !progress.IsCompleted)
                {
                    progress.IsCompleted = true;
                    progress.CompletedAt = DateTime.UtcNow;
                }
            }

            await _context.SaveChangesAsync();

            var progressDto = new CourseProgressDto
            {
                Id = progress.Id,
                CourseId = progress.CourseId,
                WatchedMinutes = progress.WatchedMinutes,
                TotalMinutes = course.VideoDurationMinutes ?? 0,
                ProgressPercentage = progress.ProgressPercentage,
                LastWatchedPosition = progress.LastWatchedPosition,
                LastWatchedAt = progress.LastWatchedAt,
                IsCompleted = progress.IsCompleted,
                CompletedAt = progress.CompletedAt
            };

            return Ok(ApiResponse<CourseProgressDto>.Success(progressDto, "Progress updated successfully", 200));
        }

        // GET api/students/tasks - Get all tasks for enrolled courses
        [HttpGet("tasks")]
        [Authorize(Roles = "Student")]
        public async Task<IActionResult> GetTasks([FromQuery] string? status = null)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
                return Unauthorized(ApiResponse.Error("User not authenticated", 401));

            var query = _context.CourseTasks
                .Include(t => t.Course)
                .Include(t => t.TaskSubmissions.Where(ts => ts.StudentId == userId))
                .Where(t => t.Course.Enrollments.Any(e => e.UserId == userId) && t.IsActive);

            if (!string.IsNullOrEmpty(status))
            {
                if (status.ToLower() == "pending")
                {
                    query = query.Where(t => !t.TaskSubmissions.Any() || t.TaskSubmissions.Any(ts => ts.Status == "Draft"));
                }
                else if (status.ToLower() == "submitted")
                {
                    query = query.Where(t => t.TaskSubmissions.Any(ts => ts.Status == "Submitted"));
                }
                else if (status.ToLower() == "graded")
                {
                    query = query.Where(t => t.TaskSubmissions.Any(ts => ts.Status == "Graded"));
                }
            }

            var tasks = await query
                .Select(t => new CourseTaskDto
                {
                    Id = t.Id,
                    Title = t.Title,
                    Description = t.Description,
                    CourseId = t.CourseId,
                    CourseTitle = t.Course.Title,
                    DueDate = t.DueDate,
                    MaxPoints = t.MaxPoints,
                    AllowAttachments = t.AllowAttachments,
                    Instructions = t.Instructions,
                    CreatedAt = t.CreatedAt,
                    StudentSubmission = t.TaskSubmissions.FirstOrDefault() != null ? new TaskSubmissionDto
                    {
                        Id = t.TaskSubmissions.First().Id,
                        Status = t.TaskSubmissions.First().Status,
                        SubmissionText = t.TaskSubmissions.First().SubmissionText,
                        PointsEarned = t.TaskSubmissions.First().PointsEarned,
                        TutorFeedback = t.TaskSubmissions.First().TutorFeedback,
                        SubmittedAt = t.TaskSubmissions.First().SubmittedAt,
                        GradedAt = t.TaskSubmissions.First().GradedAt
                    } : null
                })
                .OrderBy(t => t.DueDate)
                .ToListAsync();

            return Ok(ApiResponse<List<CourseTaskDto>>.Success(tasks, "Tasks retrieved successfully", 200));
        }

        // POST api/students/tasks/{taskId}/submit - Submit or update task submission
        [HttpPost("tasks/{taskId}/submit")]
        [Authorize(Roles = "Student")]
        public async Task<IActionResult> SubmitTask(int taskId, [FromBody] CreateTaskSubmissionDto submissionDto)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
                return Unauthorized(ApiResponse.Error("User not authenticated", 401));

            var task = await _context.CourseTasks
                .Include(t => t.Course)
                .FirstOrDefaultAsync(t => t.Id == taskId);

            if (task == null)
                return NotFound(ApiResponse.Error("Task not found", 404));

            // Check if student is enrolled in the course
            var enrollment = await _context.Enrollments
                .FirstOrDefaultAsync(e => e.UserId == userId && e.CourseId == task.CourseId);

            if (enrollment == null)
                return Forbid("You are not enrolled in this course");

            // Get or create submission
            var submission = await _context.TaskSubmissions
                .FirstOrDefaultAsync(ts => ts.TaskId == taskId && ts.StudentId == userId);

            if (submission == null)
            {
                submission = new TaskSubmission
                {
                    TaskId = taskId,
                    StudentId = userId,
                    Status = "Draft",
                    CreatedAt = DateTime.UtcNow
                };
                _context.TaskSubmissions.Add(submission);
            }

            // Update submission
            submission.SubmissionText = submissionDto.SubmissionText;
            submission.Status = "Submitted";
            submission.SubmittedAt = DateTime.UtcNow;
            submission.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            var submissionResult = new TaskSubmissionDto
            {
                Id = submission.Id,
                TaskId = submission.TaskId,
                TaskTitle = task.Title,
                SubmissionText = submission.SubmissionText,
                Status = submission.Status,
                SubmittedAt = submission.SubmittedAt,
                CreatedAt = submission.CreatedAt,
                UpdatedAt = submission.UpdatedAt
            };

            return Ok(ApiResponse<TaskSubmissionDto>.Success(submissionResult, "Task submitted successfully", 200));
        }

        // GET api/students/tasks/{taskId}/submission - Get task submission details
        [HttpGet("tasks/{taskId}/submission")]
        [Authorize(Roles = "Student")]
        public async Task<IActionResult> GetTaskSubmission(int taskId)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
                return Unauthorized(ApiResponse.Error("User not authenticated", 401));

            var submission = await _context.TaskSubmissions
                .Include(ts => ts.Task)
                .ThenInclude(t => t.Course)
                .Include(ts => ts.Attachments)
                .FirstOrDefaultAsync(ts => ts.TaskId == taskId && ts.StudentId == userId);

            if (submission == null)
                return NotFound(ApiResponse.Error("Submission not found", 404));

            // Check if student is enrolled in the course
            var enrollment = await _context.Enrollments
                .FirstOrDefaultAsync(e => e.UserId == userId && e.CourseId == submission.Task.CourseId);

            if (enrollment == null)
                return Forbid("You are not enrolled in this course");

            var submissionDto = new TaskSubmissionDto
            {
                Id = submission.Id,
                TaskId = submission.TaskId,
                TaskTitle = submission.Task.Title,
                SubmissionText = submission.SubmissionText,
                Status = submission.Status,
                PointsEarned = submission.PointsEarned,
                TutorFeedback = submission.TutorFeedback,
                CreatedAt = submission.CreatedAt,
                UpdatedAt = submission.UpdatedAt,
                SubmittedAt = submission.SubmittedAt,
                GradedAt = submission.GradedAt,
                Attachments = submission.Attachments.Select(a => new TaskAttachmentDto
                {
                    Id = a.Id,
                    FileName = a.FileName,
                    FilePath = a.FilePath,
                    ContentType = a.ContentType,
                    FileSizeBytes = a.FileSizeBytes,
                    UploadedAt = a.UploadedAt
                }).ToList()
            };

            return Ok(ApiResponse<TaskSubmissionDto>.Success(submissionDto, "Submission retrieved successfully", 200));
        }
    }
}
