using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Course_management.Data;
using Course_management.Models;
using Course_management.Dto;
using System.Security.Claims;

namespace Course_management.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "Student")]
    public class StudentTasksController : ControllerBase
    {
        private readonly DataContext _context;

        public StudentTasksController(DataContext context)
        {
            _context = context;
        }

        // GET: api/StudentTasks
        [HttpGet]
        public async Task<ActionResult<ApiResponse<List<StudentTaskDto>>>> GetStudentTasks(
            [FromQuery] bool? isCompleted = null,
            [FromQuery] TaskPriority? priority = null,
            [FromQuery] TaskCategory? category = null,
            [FromQuery] bool? isOverdue = null)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
                return Unauthorized(ApiResponse<List<StudentTaskDto>>.Error("User not authenticated", 401));

            var query = _context.StudentTasks
                .Where(t => t.StudentId == userId)
                .AsQueryable();

            // Apply filters
            if (isCompleted.HasValue)
                query = query.Where(t => t.IsCompleted == isCompleted.Value);

            if (priority.HasValue)
                query = query.Where(t => t.Priority == priority.Value);

            if (category.HasValue)
                query = query.Where(t => t.Category == category.Value);

            if (isOverdue.HasValue && isOverdue.Value)
                query = query.Where(t => t.DueDate.HasValue && t.DueDate.Value < DateTime.UtcNow && !t.IsCompleted);

            var tasks = await query
                .OrderByDescending(t => t.CreatedAt)
                .Select(t => new StudentTaskDto
                {
                    Id = t.Id,
                    StudentId = t.StudentId,
                    Title = t.Title,
                    Description = t.Description,
                    Category = t.Category,
                    CategoryName = t.Category.ToString(),
                    Priority = t.Priority,
                    PriorityName = t.Priority.ToString(),
                    DueDate = t.DueDate,
                    EstimatedTimeMinutes = t.EstimatedTimeMinutes,
                    EstimatedTimeHours = t.EstimatedTimeMinutes.HasValue ? Math.Round(t.EstimatedTimeMinutes.Value / 60.0m, 2) : null,
                    IsCompleted = t.IsCompleted,
                    CompletedAt = t.CompletedAt,
                    IsOverdue = t.DueDate.HasValue && t.DueDate.Value < DateTime.UtcNow && !t.IsCompleted,
                    CreatedAt = t.CreatedAt,
                    UpdatedAt = t.UpdatedAt
                })
                .ToListAsync();

            return Ok(ApiResponse<List<StudentTaskDto>>.Success(tasks, "Tasks retrieved successfully", 200));
        }

        // GET: api/StudentTasks/5
        [HttpGet("{id}")]
        public async Task<ActionResult<ApiResponse<StudentTaskDto>>> GetStudentTask(int id)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
                return Unauthorized(ApiResponse<StudentTaskDto>.Error("User not authenticated", 401));

            var task = await _context.StudentTasks
                .Where(t => t.Id == id && t.StudentId == userId)
                .Select(t => new StudentTaskDto
                {
                    Id = t.Id,
                    StudentId = t.StudentId,
                    Title = t.Title,
                    Description = t.Description,
                    Category = t.Category,
                    CategoryName = t.Category.ToString(),
                    Priority = t.Priority,
                    PriorityName = t.Priority.ToString(),
                    DueDate = t.DueDate,
                    EstimatedTimeMinutes = t.EstimatedTimeMinutes,
                    EstimatedTimeHours = t.EstimatedTimeMinutes.HasValue ? Math.Round(t.EstimatedTimeMinutes.Value / 60.0m, 2) : null,
                    IsCompleted = t.IsCompleted,
                    CompletedAt = t.CompletedAt,
                    IsOverdue = t.DueDate.HasValue && t.DueDate.Value < DateTime.UtcNow && !t.IsCompleted,
                    CreatedAt = t.CreatedAt,
                    UpdatedAt = t.UpdatedAt
                })
                .FirstOrDefaultAsync();

            if (task == null)
                return NotFound(ApiResponse<StudentTaskDto>.Error("Task not found", 404));

            return Ok(ApiResponse<StudentTaskDto>.Success(task, "Task retrieved successfully", 200));
        }

        // POST: api/StudentTasks
        [HttpPost]
        public async Task<ActionResult<ApiResponse<StudentTaskDto>>> CreateStudentTask(CreateStudentTaskDto createDto)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
                return Unauthorized(ApiResponse<StudentTaskDto>.Error("User not authenticated", 401));

            var task = new StudentTask
            {
                StudentId = userId,
                Title = createDto.Title,
                Description = createDto.Description,
                Category = createDto.Category,
                Priority = createDto.Priority,
                DueDate = createDto.DueDate,
                EstimatedTimeMinutes = createDto.EstimatedTimeMinutes,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.StudentTasks.Add(task);
            await _context.SaveChangesAsync();

            var taskDto = new StudentTaskDto
            {
                Id = task.Id,
                StudentId = task.StudentId,
                Title = task.Title,
                Description = task.Description,
                Category = task.Category,
                CategoryName = task.Category.ToString(),
                Priority = task.Priority,
                PriorityName = task.Priority.ToString(),
                DueDate = task.DueDate,
                EstimatedTimeMinutes = task.EstimatedTimeMinutes,
                EstimatedTimeHours = task.EstimatedTimeHours,
                IsCompleted = task.IsCompleted,
                CompletedAt = task.CompletedAt,
                IsOverdue = task.IsOverdue,
                CreatedAt = task.CreatedAt,
                UpdatedAt = task.UpdatedAt
            };

            return CreatedAtAction(nameof(GetStudentTask), new { id = task.Id }, 
                ApiResponse<StudentTaskDto>.Success(taskDto, "Task created successfully", 201));
        }

        // PUT: api/StudentTasks/5
        [HttpPut("{id}")]
        public async Task<ActionResult<ApiResponse<StudentTaskDto>>> UpdateStudentTask(int id, UpdateStudentTaskDto updateDto)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
                return Unauthorized(ApiResponse<StudentTaskDto>.Error("User not authenticated", 401));

            var task = await _context.StudentTasks
                .FirstOrDefaultAsync(t => t.Id == id && t.StudentId == userId);

            if (task == null)
                return NotFound(ApiResponse<StudentTaskDto>.Error("Task not found", 404));

            // Update only provided fields
            if (!string.IsNullOrEmpty(updateDto.Title))
                task.Title = updateDto.Title;
            
            if (updateDto.Description != null)
                task.Description = updateDto.Description;
            
            if (updateDto.Category.HasValue)
                task.Category = updateDto.Category.Value;
            
            if (updateDto.Priority.HasValue)
                task.Priority = updateDto.Priority.Value;
            
            if (updateDto.DueDate.HasValue)
                task.DueDate = updateDto.DueDate;
            
            if (updateDto.EstimatedTimeMinutes.HasValue)
                task.EstimatedTimeMinutes = updateDto.EstimatedTimeMinutes;
            
            if (updateDto.IsCompleted.HasValue)
            {
                task.IsCompleted = updateDto.IsCompleted.Value;
                if (updateDto.IsCompleted.Value && !task.CompletedAt.HasValue)
                    task.CompletedAt = DateTime.UtcNow;
                else if (!updateDto.IsCompleted.Value)
                    task.CompletedAt = null;
            }

            task.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            var taskDto = new StudentTaskDto
            {
                Id = task.Id,
                StudentId = task.StudentId,
                Title = task.Title,
                Description = task.Description,
                Category = task.Category,
                CategoryName = task.Category.ToString(),
                Priority = task.Priority,
                PriorityName = task.Priority.ToString(),
                DueDate = task.DueDate,
                EstimatedTimeMinutes = task.EstimatedTimeMinutes,
                EstimatedTimeHours = task.EstimatedTimeHours,
                IsCompleted = task.IsCompleted,
                CompletedAt = task.CompletedAt,
                IsOverdue = task.IsOverdue,
                CreatedAt = task.CreatedAt,
                UpdatedAt = task.UpdatedAt
            };

            return Ok(ApiResponse<StudentTaskDto>.Success(taskDto, "Task updated successfully", 200));
        }

        // DELETE: api/StudentTasks/5
        [HttpDelete("{id}")]
        public async Task<ActionResult<ApiResponse<object>>> DeleteStudentTask(int id)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
                return Unauthorized(ApiResponse<object>.Error("User not authenticated", 401));

            var task = await _context.StudentTasks
                .FirstOrDefaultAsync(t => t.Id == id && t.StudentId == userId);

            if (task == null)
                return NotFound(ApiResponse<object>.Error("Task not found", 404));

            _context.StudentTasks.Remove(task);
            await _context.SaveChangesAsync();

            return Ok(ApiResponse<object>.Success(null, "Task deleted successfully", 200));
        }

        // POST: api/StudentTasks/5/complete
        [HttpPost("{id}/complete")]
        public async Task<ActionResult<ApiResponse<StudentTaskDto>>> CompleteTask(int id)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
                return Unauthorized(ApiResponse<StudentTaskDto>.Error("User not authenticated", 401));

            var task = await _context.StudentTasks
                .FirstOrDefaultAsync(t => t.Id == id && t.StudentId == userId);

            if (task == null)
                return NotFound(ApiResponse<StudentTaskDto>.Error("Task not found", 404));

            task.IsCompleted = true;
            task.CompletedAt = DateTime.UtcNow;
            task.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            var taskDto = new StudentTaskDto
            {
                Id = task.Id,
                StudentId = task.StudentId,
                Title = task.Title,
                Description = task.Description,
                Category = task.Category,
                CategoryName = task.Category.ToString(),
                Priority = task.Priority,
                PriorityName = task.Priority.ToString(),
                DueDate = task.DueDate,
                EstimatedTimeMinutes = task.EstimatedTimeMinutes,
                EstimatedTimeHours = task.EstimatedTimeHours,
                IsCompleted = task.IsCompleted,
                CompletedAt = task.CompletedAt,
                IsOverdue = task.IsOverdue,
                CreatedAt = task.CreatedAt,
                UpdatedAt = task.UpdatedAt
            };

            return Ok(ApiResponse<StudentTaskDto>.Success(taskDto, "Task completed successfully", 200));
        }

        // GET: api/StudentTasks/summary
        [HttpGet("summary")]
        public async Task<ActionResult<ApiResponse<StudentTaskSummaryDto>>> GetTaskSummary()
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
                return Unauthorized(ApiResponse<StudentTaskSummaryDto>.Error("User not authenticated", 401));

            var tasks = await _context.StudentTasks
                .Where(t => t.StudentId == userId)
                .ToListAsync();

            var today = DateTime.UtcNow.Date;
            var endOfWeek = today.AddDays(7 - (int)today.DayOfWeek);

            var summary = new StudentTaskSummaryDto
            {
                TotalTasks = tasks.Count,
                CompletedTasks = tasks.Count(t => t.IsCompleted),
                PendingTasks = tasks.Count(t => !t.IsCompleted),
                OverdueTasks = tasks.Count(t => t.DueDate.HasValue && t.DueDate.Value < DateTime.UtcNow && !t.IsCompleted),
                TasksDueToday = tasks.Count(t => t.DueDate.HasValue && t.DueDate.Value.Date == today && !t.IsCompleted),
                TasksDueThisWeek = tasks.Count(t => t.DueDate.HasValue && t.DueDate.Value.Date <= endOfWeek && t.DueDate.Value.Date >= today && !t.IsCompleted),
                CompletionRate = tasks.Count > 0 ? Math.Round((decimal)tasks.Count(t => t.IsCompleted) / tasks.Count * 100, 2) : 0
            };

            return Ok(ApiResponse<StudentTaskSummaryDto>.Success(summary, "Task summary retrieved successfully", 200));
        }
    }
}