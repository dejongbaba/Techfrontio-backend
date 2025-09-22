using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Course_management.Models
{
    public class CourseTask
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string Title { get; set; }

        [Required]
        public string Description { get; set; }

        [Required]
        public int CourseId { get; set; }

        [ForeignKey("CourseId")]
        public Course Course { get; set; }

        [Required]
        public string CreatedByTutorId { get; set; }

        [ForeignKey("CreatedByTutorId")]
        public User CreatedByTutor { get; set; }

        // Task properties
        public DateTime DueDate { get; set; }
        public int MaxPoints { get; set; } = 100;
        public bool AllowAttachments { get; set; } = true;
        public string? Instructions { get; set; }
        
        // Timestamps
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
        
        // Status
        public bool IsActive { get; set; } = true;
        
        // Navigation properties
        public ICollection<TaskSubmission> TaskSubmissions { get; set; }
    }
}