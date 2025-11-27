using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Course_management.Models
{
    public class LearningStreak
    {
        [Key]
        public int Id { get; set; }
        
        [Required]
        public string StudentId { get; set; }  // Changed from UserId to StudentId to match controller and migrations
        
        public int CurrentStreak { get; set; } = 0;
        
        public int LongestStreak { get; set; } = 0;
        
        public DateTime? LastActivityDate { get; set; }
        
        public DateTime? StreakStartDate { get; set; }
        
        public int TotalActiveDays { get; set; } = 0;
        
        public int TotalLearningMinutes { get; set; } = 0;
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
        
        // Navigation property
        [ForeignKey("StudentId")]  // Changed from UserId to StudentId
        public virtual User Student { get; set; }  // Changed from User to Student to match relationship
        
        // Computed properties
        [NotMapped]
        public bool IsStreakActive => LastActivityDate.HasValue && 
            LastActivityDate.Value.Date >= DateTime.UtcNow.Date.AddDays(-1);
        
        [NotMapped]
        public int DaysSinceLastActivity => LastActivityDate.HasValue ? 
            (DateTime.UtcNow.Date - LastActivityDate.Value.Date).Days : 0;
        
        [NotMapped]
        public int CurrentStreakDays => CurrentStreak;
        
        [NotMapped]
        public DateTime? CurrentStreakStartDate => StreakStartDate;
        
        [NotMapped]
        public int LongestStreakDays => LongestStreak;
        
        [NotMapped]
        public DateTime? LongestStreakStartDate => StreakStartDate; // Fallback to current streak start
        
        [NotMapped]
        public DateTime? LongestStreakEndDate => null; // Not tracked, return null
        
        [NotMapped]
        public int TotalLearningHours => TotalLearningMinutes / 60;
        
        [NotMapped]
        public decimal TotalLearningTimeHours => (decimal)TotalLearningMinutes / 60;
    }
}