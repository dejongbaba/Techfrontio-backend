using System;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Course_management.Models
{
    public class QuizSubmission
    {
        public int Id { get; set; }

        public int QuizId { get; set; }
        [JsonIgnore]
        public Quiz Quiz { get; set; }

        public string StudentId { get; set; }
        [JsonIgnore]
        public User Student { get; set; }

        public int Score { get; set; } // Percentage

        public bool Passed { get; set; }

        // Storing answers as JSON: { "questionId": selectedOptionIndex, ... }
        public string AnswersJson { get; set; }

        public DateTime SubmittedAt { get; set; } = DateTime.UtcNow;
    }
}
