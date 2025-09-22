using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Course_management.Models
{
    public class Course
    {
        public int Id { get; set; }
        
        [Required]
        public string Title { get; set; }
        
        public string Description { get; set; }
        
        [Required]
        public string TutorId { get; set; }
        
        public User Tutor { get; set; }
        
        // Video content properties
        public string? VideoCoverImageUrl { get; set; }
        public string? VideoContentUrl { get; set; }
        public int? VideoDurationMinutes { get; set; }
        
        // Navigation properties
        public ICollection<Enrollment> Enrollments { get; set; }
        public ICollection<Review> Reviews { get; set; }
        public ICollection<CourseProgress> CourseProgresses { get; set; }
        public ICollection<CourseTask> CourseTasks { get; set; }
    }
}