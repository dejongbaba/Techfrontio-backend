using Microsoft.AspNetCore.Http;

namespace Course_management.Dto
{
    public class TaskSubmissionFormDto
    {
        public int TaskId { get; set; }
        public string? SubmissionText { get; set; }
        public IFormFile? File { get; set; }
    }
}
