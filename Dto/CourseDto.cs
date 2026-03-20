namespace Course_management.Dto
{
    using Course_management.Models;

    public class CourseDto
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public string? Subtitle { get; set; }
        public string Description { get; set; }
        public string? Category { get; set; }
        public string? Level { get; set; }
        public string? Language { get; set; }
        public string? Tags { get; set; }
        public string? Features { get; set; }
        public string TutorId { get; set; }
        public string? TutorName { get; set; }
        public int EnrollmentCount { get; set; }
        public double AverageRating { get; set; }
        public decimal Price { get; set; }
        public CourseStatus Status { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public string? VideoCoverImageUrl { get; set; }
        public string? VideoContentUrl { get; set; }
        public int? VideoDurationMinutes { get; set; }
    }

    public class CourseCreateDto
    {
        public string Title { get; set; }
        public string? Subtitle { get; set; }
        public string Description { get; set; }
        public string? Category { get; set; }
        public string? Level { get; set; }
        public string? Language { get; set; }
        public string? Tags { get; set; }
        public string? Features { get; set; }
        public string? VideoCoverImageUrl { get; set; }
        public string? VideoContentUrl { get; set; }
        public int videoDurationMinutes { get; set; }
        public decimal Price { get; set; }
    }

    public class CourseUpdateDto : CourseCreateDto { }

    public class UpdateCourseStatusDto
    {
        public CourseStatus Status { get; set; }
    }

    public class CourseContentDto
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public string ContentType { get; set; }
        public double Duration { get; set; }
        public int Order { get; set; }
        public bool IsCompleted { get; set; }
        public string? PublicId { get; set; }
        public string? SecureUrl { get; set; }
    }

    public class CourseContentCreateDto
    {
        public string Title { get; set; }
        public string PublicId { get; set; }
        public string ContentType { get; set; }
        public double Duration { get; set; }
        public int Order { get; set; }
        public int? SectionId { get; set; }
    }

    public class CourseContentResponseDto : CourseContentDto { }

    public class CourseDetailDto : CourseDto
    {
        public List<ReviewDto> Reviews { get; set; } = new List<ReviewDto>();
        public List<CourseSectionDto> Sections { get; set; } = new List<CourseSectionDto>();
        public List<CourseContentDto>? Contents { get; set; }
        public CourseProgressDto? Progress { get; set; }
        public List<CourseTaskDto>? Tasks { get; set; }
    }
    
    public class CourseSectionDto
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public int Order { get; set; }
        public List<CourseContentDto> Contents { get; set; } = new List<CourseContentDto>();
    }

    public class CourseSectionCreateDto
    {
        public string Title { get; set; }
        public int Order { get; set; }
    }
}