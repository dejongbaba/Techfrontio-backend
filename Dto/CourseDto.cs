using System.Collections.Generic;

namespace Course_management.Dto
{
    public class CourseDto
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public string TutorId { get; set; }
        public string TutorName { get; set; }
        public int EnrollmentCount { get; set; }
        public double AverageRating { get; set; }
    }

    public class CourseCreateDto
    {
        public string Title { get; set; }
        public string Description { get; set; }
    }

    public class CourseUpdateDto
    {
        public string Title { get; set; }
        public string Description { get; set; }
    }

    public class CourseDetailDto
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public string TutorId { get; set; }
        public string TutorName { get; set; }
        public int EnrollmentCount { get; set; }
        public double AverageRating { get; set; }
        public List<ReviewDto> Reviews { get; set; }
    }
}