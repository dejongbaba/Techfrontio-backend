namespace Course_management.Dto
{
    public class CourseContentUpdateDto
    {
        public string Title { get; set; }
        public string? PublicId { get; set; }
        public string? ContentType { get; set; }
        public double Duration { get; set; }
        public int Order { get; set; }
        public int? SectionId { get; set; }
    }
}
