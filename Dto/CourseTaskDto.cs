using System;
using System.Collections.Generic;

namespace Course_management.Dto
{
    public class CourseTaskDto
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public int CourseId { get; set; }
        public string CourseTitle { get; set; }
        public string CreatedByTutorId { get; set; }
        public string TutorName { get; set; }
        public DateTime DueDate { get; set; }
        public int MaxPoints { get; set; }
        public bool AllowAttachments { get; set; }
        public string? Instructions { get; set; }
        public DateTime CreatedAt { get; set; }
        public bool IsActive { get; set; }
        
        // Student-specific information
        public TaskSubmissionDto? StudentSubmission { get; set; }
        public bool IsOverdue => DateTime.UtcNow > DueDate;
        public int DaysUntilDue => (DueDate - DateTime.UtcNow).Days;
    }

    public class CreateCourseTaskDto
    {
        public string Title { get; set; }
        public string Description { get; set; }
        public int CourseId { get; set; }
        public DateTime DueDate { get; set; }
        public int MaxPoints { get; set; } = 100;
        public bool AllowAttachments { get; set; } = true;
        public string? Instructions { get; set; }
    }

    public class UpdateCourseTaskDto
    {
        public string Title { get; set; }
        public string Description { get; set; }
        public DateTime DueDate { get; set; }
        public int MaxPoints { get; set; }
        public bool AllowAttachments { get; set; }
        public string? Instructions { get; set; }
        public bool IsActive { get; set; }
    }
}