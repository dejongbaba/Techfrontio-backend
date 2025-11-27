using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Course_management.Models
{
    public enum CourseStatus
    {
        Pending, 
        Approved,
        Rejected
    }

    public class Course
    {
        public int Id { get; set; }
        
        [Required]
        public string Title { get; set; }
        
        public string Description { get; set; }
        
        [Required]
        public string TutorId { get; set; }
        
        public User Tutor { get; set; }

        public decimal Price { get; set; }

        public CourseStatus Status { get; set; }
        
        // Video content properties
        public string? VideoCoverImageUrl { get; set; }
        public string? VideoContentUrl { get; set; }
        public int? VideoDurationMinutes { get; set; }
        
        // Navigation properties
        public ICollection<Enrollment> Enrollments { get; set; }
        public ICollection<Review> Reviews { get; set; }
        public ICollection<CourseProgress> CourseProgresses { get; set; }
        public ICollection<CourseTask> CourseTasks { get; set; }
        public ICollection<CourseContent> CourseContents { get; set; }
        
        // Add these properties
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }
}