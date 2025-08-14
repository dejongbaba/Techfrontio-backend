using Microsoft.AspNetCore.Identity;
using System.Collections.Generic;

namespace Course_management.Models
{
    public class User : IdentityUser
    {
        public string FullName { get; set; }
        public string Role { get; set; } // Student, Tutor, Admin
        public ICollection<Enrollment> Enrollments { get; set; }
        public ICollection<Review> Reviews { get; set; }
        public ICollection<Course> Courses { get; set; } // For tutors
    }
} 