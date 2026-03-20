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
        
        public string? Subtitle { get; set; }
        
        public string Description { get; set; }
        
        public string? Category { get; set; }
        
        public string? Level { get; set; } // Beginner, Intermediate, Advanced
        
        public string? Language { get; set; }
        
        public string? Tags { get; set; } // Comma-separated or JSON string
        
        public string? Features { get; set; } // Comma-separated or JSON string list of features

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
        public ICollection<Review> Reviews { get; set; } = new List<Review>();
        public ICollection<CourseContent> CourseContents { get; set; } = new List<CourseContent>();
        public ICollection<CourseSection> CourseSections { get; set; } = new List<CourseSection>();
        public ICollection<CourseProgress> CourseProgresses { get; set; } = new List<CourseProgress>();
        public ICollection<CourseTask> CourseTasks { get; set; }
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }
}