namespace Course_management.Models
{
    public class Enrollment
    {
        public int Id { get; set; }
        public string UserId { get; set; }
        public User User { get; set; }
        public int CourseId { get; set; }
        public Course Course { get; set; }
        // Add this property
        public DateTime EnrollmentDate { get; set; } = DateTime.UtcNow;
    }
}