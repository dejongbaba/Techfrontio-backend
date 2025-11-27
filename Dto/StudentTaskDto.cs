using System;
using Course_management.Models;

namespace Course_management.Dto
{
    public class StudentTaskDto
    {
        public int Id { get; set; }
        public string StudentId { get; set; }
        public string Title { get; set; }
        public string? Description { get; set; }
        public TaskCategory Category { get; set; }
        public string CategoryName { get; set; }
        public TaskPriority Priority { get; set; }
        public string PriorityName { get; set; }
        public DateTime? DueDate { get; set; }
        public int? EstimatedTimeMinutes { get; set; }
        public decimal? EstimatedTimeHours { get; set; }
        public bool IsCompleted { get; set; }
        public DateTime? CompletedAt { get; set; }
        public bool IsOverdue { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }

    public class CreateStudentTaskDto
    {
        public string Title { get; set; }
        public string? Description { get; set; }
        public TaskCategory Category { get; set; }
        public TaskPriority Priority { get; set; }
        public DateTime? DueDate { get; set; }
        public int? EstimatedTimeMinutes { get; set; }
    }

    public class UpdateStudentTaskDto
    {
        public string? Title { get; set; }
        public string? Description { get; set; }
        public TaskCategory? Category { get; set; }
        public TaskPriority? Priority { get; set; }
        public DateTime? DueDate { get; set; }
        public int? EstimatedTimeMinutes { get; set; }
        public bool? IsCompleted { get; set; }
    }

    public class StudentTaskSummaryDto
    {
        public int TotalTasks { get; set; }
        public int CompletedTasks { get; set; }
        public int PendingTasks { get; set; }
        public int OverdueTasks { get; set; }
        public int TasksDueToday { get; set; }
        public int TasksDueThisWeek { get; set; }
        public decimal CompletionRate { get; set; }
    }
}