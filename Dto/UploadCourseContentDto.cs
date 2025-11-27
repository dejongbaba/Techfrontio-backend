using Microsoft.AspNetCore.Http;

namespace Course_management.Dto
{
    public class UploadCourseContentDto
    {
        public IFormFile? File { get; set; }
        public string Title { get; set; } = string.Empty;
        public string ContentType { get; set; } = string.Empty;
        public int Order { get; set; }
    }
}