using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Course_management.Data;
using Course_management.Models;
using Course_management.Dto;
using Course_management.Services;
using System.Security.Claims;

namespace Course_management.Controllers
{
    [ApiController]
    [Route("api/tasks")]
    [Authorize]
    public class TasksController : ControllerBase
    {
        private readonly DataContext _context;
        private readonly CloudinaryService _cloudinaryService;

        public TasksController(DataContext context, CloudinaryService cloudinaryService)
        {
            _context = context;
            _cloudinaryService = cloudinaryService;
        }

        // GET: api/tasks/course/{courseId}
        [HttpGet("course/{courseId}")]
        public async Task<ActionResult<ApiResponse<List<CourseTaskDto>>>> GetTasksForCourse(int courseId)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
                return Unauthorized(ApiResponse<List<CourseTaskDto>>.Error("User not authenticated", 401));

            var tasks = await _context.CourseTasks
                .Include(t => t.Course)
                .Include(t => t.CreatedByTutor)
                .Include(t => t.TaskSubmissions.Where(s => s.StudentId == userId))
                .Where(t => t.CourseId == courseId && t.IsActive)
                .OrderBy(t => t.DueDate)
                .Select(t => new CourseTaskDto
                {
                    Id = t.Id,
                    Title = t.Title,
                    Description = t.Description,
                    CourseId = t.CourseId,
                    CourseTitle = t.Course.Title,
                    CreatedByTutorId = t.CreatedByTutorId,
                    TutorName = t.CreatedByTutor.FullName,
                    DueDate = t.DueDate,
                    MaxPoints = t.MaxPoints,
                    AllowAttachments = t.AllowAttachments,
                    Instructions = t.Instructions,
                    CreatedAt = t.CreatedAt,
                    IsActive = t.IsActive,
                    StudentSubmission = t.TaskSubmissions.Where(s => s.StudentId == userId).Select(s => new TaskSubmissionDto
                    {
                        Id = s.Id,
                        TaskId = s.TaskId,
                        TaskTitle = t.Title,
                        StudentId = s.StudentId,
                        StudentName = userId, // Placeholder, will be filled if needed
                        SubmissionText = s.SubmissionText,
                        Status = s.Status,
                        PointsEarned = s.PointsEarned,
                        TutorFeedback = s.TutorFeedback,
                        GradedByTutorId = s.GradedByTutorId,
                        CreatedAt = s.CreatedAt,
                        SubmittedAt = s.SubmittedAt,
                        GradedAt = s.GradedAt
                    }).FirstOrDefault()
                })
                .ToListAsync();

            return Ok(ApiResponse<List<CourseTaskDto>>.Success(tasks, "Tasks retrieved successfully", 200));
        }

        // GET: api/tasks/{id}
        [HttpGet("{id}")]
        public async Task<ActionResult<ApiResponse<CourseTaskDto>>> GetTask(int id)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            
            var task = await _context.CourseTasks
                .Include(t => t.Course)
                .Include(t => t.CreatedByTutor)
                .Include(t => t.TaskSubmissions.Where(s => s.StudentId == userId))
                .FirstOrDefaultAsync(t => t.Id == id);

            if (task == null)
                return NotFound(ApiResponse<CourseTaskDto>.Error("Task not found", 404));

            var taskDto = new CourseTaskDto
            {
                Id = task.Id,
                Title = task.Title,
                Description = task.Description,
                CourseId = task.CourseId,
                CourseTitle = task.Course.Title,
                CreatedByTutorId = task.CreatedByTutorId,
                TutorName = task.CreatedByTutor.FullName,
                DueDate = task.DueDate,
                MaxPoints = task.MaxPoints,
                AllowAttachments = task.AllowAttachments,
                Instructions = task.Instructions,
                CreatedAt = task.CreatedAt,
                IsActive = task.IsActive,
                StudentSubmission = task.TaskSubmissions.Where(s => s.StudentId == userId).Select(s => new TaskSubmissionDto
                {
                    Id = s.Id,
                    TaskId = s.TaskId,
                    TaskTitle = task.Title,
                    StudentId = s.StudentId,
                    SubmissionText = s.SubmissionText,
                    Status = s.Status,
                    PointsEarned = s.PointsEarned,
                    TutorFeedback = s.TutorFeedback,
                    GradedByTutorId = s.GradedByTutorId,
                    CreatedAt = s.CreatedAt,
                    SubmittedAt = s.SubmittedAt,
                    GradedAt = s.GradedAt,
                    Attachments = _context.TaskAttachments.Where(a => a.TaskSubmissionId == s.Id).Select(a => new TaskAttachmentDto
                    {
                        Id = a.Id,
                        TaskSubmissionId = a.TaskSubmissionId,
                        FileName = a.FileName,
                        FilePath = a.FilePath,
                        ContentType = a.ContentType,
                        FileSizeBytes = a.FileSizeBytes,
                        UploadedAt = a.UploadedAt,
                        UploadedByUserId = a.UploadedByUserId
                    }).ToList()
                }).FirstOrDefault()
            };

            return Ok(ApiResponse<CourseTaskDto>.Success(taskDto, "Task retrieved successfully", 200));
        }

        // POST: api/tasks
        [HttpPost]
        [Authorize(Roles = "Tutor,Admin")]
        public async Task<ActionResult<ApiResponse<CourseTaskDto>>> CreateTask([FromBody] CreateCourseTaskDto createDto)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            
            // Verify tutor owns the course or is admin
            var course = await _context.Courses.FindAsync(createDto.CourseId);
            if (course == null)
                return NotFound(ApiResponse<CourseTaskDto>.Error("Course not found", 404));

            if (course.TutorId != userId && !User.IsInRole("Admin"))
                return Forbid();

            var task = new CourseTask
            {
                Title = createDto.Title,
                Description = createDto.Description,
                CourseId = createDto.CourseId,
                CreatedByTutorId = userId,
                DueDate = createDto.DueDate,
                MaxPoints = createDto.MaxPoints,
                AllowAttachments = createDto.AllowAttachments,
                Instructions = createDto.Instructions,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                IsActive = true
            };

            _context.CourseTasks.Add(task);
            await _context.SaveChangesAsync();

            var taskDto = new CourseTaskDto
            {
                Id = task.Id,
                Title = task.Title,
                Description = task.Description,
                CourseId = task.CourseId,
                CreatedByTutorId = task.CreatedByTutorId,
                DueDate = task.DueDate,
                MaxPoints = task.MaxPoints,
                AllowAttachments = task.AllowAttachments,
                Instructions = task.Instructions,
                CreatedAt = task.CreatedAt,
                IsActive = task.IsActive
            };

            return CreatedAtAction(nameof(GetTask), new { id = task.Id }, ApiResponse<CourseTaskDto>.Success(taskDto, "Task created successfully", 201));
        }

        // POST: api/tasks/submit
        [HttpPost("submit")]
        [Consumes("multipart/form-data")]
        [Authorize(Roles = "Student")]
        [ApiExplorerSettings(IgnoreApi = true)]
        public async Task<ActionResult<ApiResponse<TaskSubmissionDto>>> SubmitTask([FromForm] TaskSubmissionFormDto dto)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            
            var task = await _context.CourseTasks.FindAsync(dto.TaskId);
            if (task == null)
                return NotFound(ApiResponse<TaskSubmissionDto>.Error("Task not found", 404));

            // Check if submission already exists
            var existingSubmission = await _context.TaskSubmissions
                .FirstOrDefaultAsync(s => s.TaskId == dto.TaskId && s.StudentId == userId);

            if (existingSubmission != null)
            {
                // Update existing submission
                existingSubmission.SubmissionText = dto.SubmissionText ?? existingSubmission.SubmissionText;
                existingSubmission.SubmittedAt = DateTime.UtcNow;
                existingSubmission.Status = "Submitted";
                existingSubmission.UpdatedAt = DateTime.UtcNow;
                
                _context.TaskSubmissions.Update(existingSubmission);
            }
            else
            {
                // Create new submission
                existingSubmission = new TaskSubmission
                {
                    TaskId = dto.TaskId,
                    StudentId = userId,
                    SubmissionText = dto.SubmissionText,
                    Status = "Submitted",
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow,
                    SubmittedAt = DateTime.UtcNow
                };
                
                _context.TaskSubmissions.Add(existingSubmission);
            }

            await _context.SaveChangesAsync();

            // Handle file upload if present
            if (dto.File != null && dto.File.Length > 0)
            {
                if (!task.AllowAttachments)
                    return BadRequest(ApiResponse<TaskSubmissionDto>.Error("This task does not allow attachments", 400));

                try 
                {
                    // Upload to Cloudinary
                    // Determine type based on extension or mime type if needed, but for now treating as raw/auto or image/video
                    // Using "raw" for generic files or checking type
                    string resourceType = "raw";
                    if (dto.File.ContentType.StartsWith("image/")) resourceType = "image";
                    else if (dto.File.ContentType.StartsWith("video/")) resourceType = "video";
                    else if (dto.File.ContentType == "application/pdf") resourceType = "auto"; 

                    // For CloudinaryService.UploadDocumentAsync, it likely expects IFormFile and handles logic
                    // But checking signature: public async Task<UploadResult> UploadDocumentAsync(IFormFile file)
                    
                    var uploadResult = await _cloudinaryService.UploadDocumentAsync(dto.File);
                    
                    if (uploadResult != null && uploadResult.StatusCode == System.Net.HttpStatusCode.OK)
                    {
                        var attachment = new TaskAttachment
                        {
                            TaskSubmissionId = existingSubmission.Id,
                            FileName = dto.File.FileName,
                            FilePath = uploadResult.SecureUrl.AbsoluteUri,
                            ContentType = dto.File.ContentType,
                            FileSizeBytes = dto.File.Length,
                            UploadedAt = DateTime.UtcNow,
                            UploadedByUserId = userId
                        };
                        
                        _context.TaskAttachments.Add(attachment);
                        await _context.SaveChangesAsync();
                    }
                }
                catch (Exception ex)
                {
                    // Log error but don't fail the whole submission if text was saved
                    // Ideally should return warning
                }
            }

            // Return updated submission
            var submissionDto = new TaskSubmissionDto
            {
                Id = existingSubmission.Id,
                TaskId = existingSubmission.TaskId,
                TaskTitle = task.Title,
                StudentId = existingSubmission.StudentId,
                SubmissionText = existingSubmission.SubmissionText,
                Status = existingSubmission.Status,
                CreatedAt = existingSubmission.CreatedAt,
                SubmittedAt = existingSubmission.SubmittedAt,
                Attachments = await _context.TaskAttachments
                    .Where(a => a.TaskSubmissionId == existingSubmission.Id)
                    .Select(a => new TaskAttachmentDto
                    {
                        Id = a.Id,
                        FileName = a.FileName,
                        FilePath = a.FilePath,
                        ContentType = a.ContentType,
                        FileSizeBytes = a.FileSizeBytes,
                        UploadedAt = a.UploadedAt
                    }).ToListAsync()
            };

            return Ok(ApiResponse<TaskSubmissionDto>.Success(submissionDto, "Task submitted successfully", 200));
        }

        // GET: api/tasks/{taskId}/submissions
        [HttpGet("{taskId}/submissions")]
        [Authorize(Roles = "Tutor,Admin")]
        public async Task<ActionResult<ApiResponse<List<TaskSubmissionDto>>>> GetTaskSubmissions(int taskId)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            var task = await _context.CourseTasks
                .Include(t => t.Course)
                .FirstOrDefaultAsync(t => t.Id == taskId);

            if (task == null)
                return NotFound(ApiResponse<List<TaskSubmissionDto>>.Error("Task not found", 404));

            // Verify tutor owns the course
            if (task.Course.TutorId != userId && !User.IsInRole("Admin"))
                return Forbid();

            var submissions = await _context.TaskSubmissions
                .Include(s => s.Student)
                .Where(s => s.TaskId == taskId)
                .Select(s => new TaskSubmissionDto
                {
                    Id = s.Id,
                    TaskId = s.TaskId,
                    TaskTitle = task.Title,
                    StudentId = s.StudentId,
                    StudentName = s.Student.FullName, // Assuming User has FullName
                    SubmissionText = s.SubmissionText,
                    Status = s.Status,
                    PointsEarned = s.PointsEarned,
                    TutorFeedback = s.TutorFeedback,
                    GradedByTutorId = s.GradedByTutorId,
                    CreatedAt = s.CreatedAt,
                    SubmittedAt = s.SubmittedAt,
                    GradedAt = s.GradedAt,
                    Attachments = _context.TaskAttachments.Where(a => a.TaskSubmissionId == s.Id).Select(a => new TaskAttachmentDto
                    {
                        Id = a.Id,
                        TaskSubmissionId = a.TaskSubmissionId,
                        FileName = a.FileName,
                        FilePath = a.FilePath,
                        ContentType = a.ContentType,
                        FileSizeBytes = a.FileSizeBytes,
                        UploadedAt = a.UploadedAt,
                        UploadedByUserId = a.UploadedByUserId
                    }).ToList()
                })
                .ToListAsync();

            return Ok(ApiResponse<List<TaskSubmissionDto>>.Success(submissions, "Submissions retrieved successfully", 200));
        }

        // POST: api/tasks/submissions/{submissionId}/grade
        [HttpPost("submissions/{submissionId}/grade")]
        [Authorize(Roles = "Tutor,Admin")]
        public async Task<ActionResult<ApiResponse<TaskSubmissionDto>>> GradeSubmission(int submissionId, [FromBody] GradeTaskSubmissionDto gradeDto)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            var submission = await _context.TaskSubmissions
                .Include(s => s.Task)
                .ThenInclude(t => t.Course)
                .FirstOrDefaultAsync(s => s.Id == submissionId);

            if (submission == null)
                return NotFound(ApiResponse<TaskSubmissionDto>.Error("Submission not found", 404));

            // Verify tutor owns the course
            if (submission.Task.Course.TutorId != userId && !User.IsInRole("Admin"))
                return Forbid();

            submission.PointsEarned = gradeDto.PointsEarned;
            submission.TutorFeedback = gradeDto.TutorFeedback;
            submission.GradedByTutorId = userId;
            submission.GradedAt = DateTime.UtcNow;
            submission.Status = "Graded";

            _context.TaskSubmissions.Update(submission);
            await _context.SaveChangesAsync();

            var submissionDto = new TaskSubmissionDto
            {
                Id = submission.Id,
                TaskId = submission.TaskId,
                TaskTitle = submission.Task.Title,
                StudentId = submission.StudentId,
                SubmissionText = submission.SubmissionText,
                Status = submission.Status,
                PointsEarned = submission.PointsEarned,
                TutorFeedback = submission.TutorFeedback,
                GradedByTutorId = submission.GradedByTutorId,
                CreatedAt = submission.CreatedAt,
                SubmittedAt = submission.SubmittedAt,
                GradedAt = submission.GradedAt
            };

            return Ok(ApiResponse<TaskSubmissionDto>.Success(submissionDto, "Submission graded successfully", 200));
        }

        // PUT: api/tasks/{id}
        [HttpPut("{id}")]
        [Authorize(Roles = "Tutor,Admin")]
        public async Task<ActionResult<ApiResponse<CourseTaskDto>>> UpdateTask(int id, [FromBody] UpdateCourseTaskDto updateDto)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            var task = await _context.CourseTasks
                .Include(t => t.Course)
                .FirstOrDefaultAsync(t => t.Id == id);

            if (task == null)
                return NotFound(ApiResponse<CourseTaskDto>.Error("Task not found", 404));

            if (task.Course.TutorId != userId && !User.IsInRole("Admin"))
                return Forbid();

            task.Title = updateDto.Title;
            task.Description = updateDto.Description;
            task.DueDate = updateDto.DueDate;
            task.MaxPoints = updateDto.MaxPoints;
            task.AllowAttachments = updateDto.AllowAttachments;
            task.Instructions = updateDto.Instructions;
            task.IsActive = updateDto.IsActive;
            task.UpdatedAt = DateTime.UtcNow;

            _context.CourseTasks.Update(task);
            await _context.SaveChangesAsync();

            var taskDto = new CourseTaskDto
            {
                Id = task.Id,
                Title = task.Title,
                Description = task.Description,
                CourseId = task.CourseId,
                CreatedByTutorId = task.CreatedByTutorId,
                DueDate = task.DueDate,
                MaxPoints = task.MaxPoints,
                AllowAttachments = task.AllowAttachments,
                Instructions = task.Instructions,
                CreatedAt = task.CreatedAt,
                IsActive = task.IsActive
            };

            return Ok(ApiResponse<CourseTaskDto>.Success(taskDto, "Task updated successfully", 200));
        }

        // DELETE: api/tasks/{id}
        [HttpDelete("{id}")]
        [Authorize(Roles = "Tutor,Admin")]
        public async Task<ActionResult<ApiResponse<bool>>> DeleteTask(int id)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            var task = await _context.CourseTasks
                .Include(t => t.Course)
                .FirstOrDefaultAsync(t => t.Id == id);

            if (task == null)
                return NotFound(ApiResponse<bool>.Error("Task not found", 404));

            if (task.Course.TutorId != userId && !User.IsInRole("Admin"))
                return Forbid();

            _context.CourseTasks.Remove(task);
            await _context.SaveChangesAsync();

            return Ok(ApiResponse<bool>.Success(true, "Task deleted successfully", 200));
        }
    }
}
