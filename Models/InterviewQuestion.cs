using System;
using System.ComponentModel.DataAnnotations;

namespace Course_management.Models
{
    public class InterviewQuestion
    {
        public int Id { get; set; }

        [Required]
        public string Category { get; set; } // e.g., "React", "JavaScript", "CSS"

        [Required]
        public string Difficulty { get; set; } // "Easy", "Medium", "Hard"

        [Required]
        public string QuestionText { get; set; }

        public string? CodeSnippet { get; set; }

        [Required]
        public string AnswerText { get; set; } // Markdown supported

        public string? Tags { get; set; } // Comma-separated tags

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
