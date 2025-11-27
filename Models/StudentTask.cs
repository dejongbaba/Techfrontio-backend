using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Course_management.Models
{
    public class StudentTask
    {
        [Key]
        public int Id { get; set; }
        
        [Required]
        public string StudentId { get; set; }
        
        [Required]
        [MaxLength(200)]
        public string Title { get; set; }
        
        [MaxLength(1000)]
        public string? Description { get; set; }
        
        [Required]
        public TaskCategory Category { get; set; }
        
        [Required]
        public TaskPriority Priority { get; set; }
        
        public DateTime? DueDate { get; set; }
        
        public int? EstimatedTimeMinutes { get; set; }
        
        public bool IsCompleted { get; set; } = false;
        
        public DateTime? CompletedAt { get; set; }
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
        
        // Navigation property
        [ForeignKey("StudentId")]
        public virtual User Student { get; set; }
        
        // Computed property
        [NotMapped]
        public bool IsOverdue => DueDate.HasValue && DueDate.Value < DateTime.UtcNow && !IsCompleted;
        
        [NotMapped]
        public decimal? EstimatedTimeHours => EstimatedTimeMinutes.HasValue ? (decimal)EstimatedTimeMinutes.Value / 60 : null;
    }
}