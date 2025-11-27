using System;

namespace Course_management.Dto
{
    public class LearningStreakDto
    {
        public int Id { get; set; }
        public string StudentId { get; set; }
        public int CurrentStreakDays { get; set; }
        public DateTime? CurrentStreakStartDate { get; set; }
        public DateTime? LastActivityDate { get; set; }
        public int LongestStreakDays { get; set; }
        public DateTime? LongestStreakStartDate { get; set; }
        public DateTime? LongestStreakEndDate { get; set; }
        public int TotalActiveDays { get; set; }
        public int TotalLearningHours { get; set; }
        public int TotalLearningMinutes { get; set; }
        public decimal TotalLearningTimeHours { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }

    public class UpdateLearningStreakDto
    {
        public int? AdditionalMinutes { get; set; }
        public DateTime? ActivityDate { get; set; }
    }
}