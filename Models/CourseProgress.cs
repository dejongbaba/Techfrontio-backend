using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Course_management.Models
{
    public class CourseProgress
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string UserId { get; set; }

        [ForeignKey("UserId")]
        public User User { get; set; }

        [Required]
        public int CourseId { get; set; }

        [ForeignKey("CourseId")]
        public Course Course { get; set; }

        // Progress tracking
        public int WatchedMinutes { get; set; } = 0;
        public int TotalMinutes { get; set; } = 0;
        public decimal ProgressPercentage { get; set; } = 0;
        
        // Last watched position in video (in seconds)
        public int LastWatchedPosition { get; set; } = 0;
        
        // Timestamps
        public DateTime LastWatchedAt { get; set; } = DateTime.UtcNow;
        public DateTime StartedAt { get; set; } = DateTime.UtcNow;
        
        // Completion status
        public bool IsCompleted { get; set; } = false;
        public DateTime? CompletedAt { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}