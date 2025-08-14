namespace Course_management.Dto
{
    public class ReviewDto
    {
        public int Id { get; set; }
        public string Content { get; set; }
        public int Rating { get; set; }
        public string UserId { get; set; }
        public string UserName { get; set; }
        public int CourseId { get; set; }
        public string CourseTitle { get; set; }
    }

    public class ReviewCreateDto
    {
        public string Content { get; set; }
        public int Rating { get; set; }
        public int CourseId { get; set; }
    }

    public class ReviewUpdateDto
    {
        public string Content { get; set; }
        public int Rating { get; set; }
    }
}