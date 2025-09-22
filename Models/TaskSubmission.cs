using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Course_management.Models
{
    public class TaskSubmission
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int TaskId { get; set; }

        [ForeignKey("TaskId")]
        public CourseTask Task { get; set; }

        [Required]
        public string StudentId { get; set; }

        [ForeignKey("StudentId")]
        public User Student { get; set; }

        // Submission content
        public string? SubmissionText { get; set; }
        
        // Status tracking
        [Required]
        public string Status { get; set; } = "Draft"; // Draft, Submitted, Reviewed, Graded
        
        // Grading
        public int? PointsEarned { get; set; }
        public string? TutorFeedback { get; set; }
        public string? GradedByTutorId { get; set; }
        
        [ForeignKey("GradedByTutorId")]
        public User? GradedByTutor { get; set; }
        
        // Timestamps
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? SubmittedAt { get; set; }
        public DateTime? GradedAt { get; set; }
        
        // Navigation properties
        public ICollection<TaskAttachment> Attachments { get; set; } = new List<TaskAttachment>();
    }
}