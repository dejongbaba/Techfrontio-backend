using System.Collections.Generic;

namespace Course_management.Models
{
    public class Course
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public string TutorId { get; set; }
        public User Tutor { get; set; }
        public ICollection<Enrollment> Enrollments { get; set; }
        public ICollection<Review> Reviews { get; set; }
    }
} 