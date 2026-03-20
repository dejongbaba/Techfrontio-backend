using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Course_management.Models
{
    public class QuizQuestion
    {
        public int Id { get; set; }

        public int QuizId { get; set; }

        [JsonIgnore]
        public Quiz Quiz { get; set; }

        [Required]
        public string QuestionText { get; set; }

        // Storing options as JSON string for simplicity: ["Option A", "Option B", "Option C", "Option D"]
        [Required]
        public string OptionsJson { get; set; }

        [Required]
        public int CorrectOptionIndex { get; set; } // 0-based index

        public string Explanation { get; set; }
    }
}
