using System.ComponentModel.DataAnnotations;

namespace Course_management.Models
{
    public class CourseContent
    {
        public int Id { get; set; }
        public int CourseId { get; set; }
        public Course Course { get; set; }

        [Required]
        public string Title { get; set; }

        public string PublicId { get; set; } // Cloudinary Public ID

        public string SecureUrl { get; set; } // Cloudinary Secure URL

        public string ContentType { get; set; } // e.g., "video", "document"

        public double Duration { get; set; } // Duration in seconds for videos

        public int Order { get; set; }
    }
}