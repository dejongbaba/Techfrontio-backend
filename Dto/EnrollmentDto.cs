namespace Course_management.Dto
{
    public class EnrollmentDto
    {
        public int Id { get; set; }
        public string UserId { get; set; }
        public string UserName { get; set; }
        public int CourseId { get; set; }
        public string CourseTitle { get; set; }
    }

    public class EnrollmentCreateDto
    {
        public int CourseId { get; set; }
    }
}