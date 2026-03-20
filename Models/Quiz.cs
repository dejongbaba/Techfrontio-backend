using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Course_management.Models
{
    public class Quiz
    {
        public int Id { get; set; }

        [Required]
        public string Title { get; set; }

        public string Description { get; set; }

        public int CourseId { get; set; }

        [JsonIgnore]
        public Course Course { get; set; }

        public int PassingScore { get; set; } = 70; // Percentage

        public ICollection<QuizQuestion> Questions { get; set; }
        public ICollection<QuizSubmission> Submissions { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
