using Microsoft.AspNetCore.Identity;
using System.Collections.Generic;

namespace Course_management.Models
{
    public class User : IdentityUser
    {
        public string FullName { get; set; }
        public string Role { get; set; } // Student, Tutor, Admin
        
        // Navigation properties
        public ICollection<Enrollment> Enrollments { get; set; }
        public ICollection<Review> Reviews { get; set; }
        public ICollection<Course> Courses { get; set; } // For tutors
        public ICollection<CourseProgress> CourseProgresses { get; set; } // For students
        public ICollection<CourseTask> CreatedTasks { get; set; } // For tutors
        public ICollection<TaskSubmission> TaskSubmissions { get; set; } // For students
        public ICollection<TaskSubmission> GradedSubmissions { get; set; } // For tutors
        public ICollection<TaskAttachment> UploadedAttachments { get; set; }
    }
}