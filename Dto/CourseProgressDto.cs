using System;
using System.Collections.Generic;

namespace Course_management.Dto
{
    public class CourseProgressDto
    {
        public int Id { get; set; }
        public string UserId { get; set; }
        public int CourseId { get; set; }
        public string CourseTitle { get; set; }
        public int WatchedMinutes { get; set; }
        public int TotalMinutes { get; set; }
        public decimal ProgressPercentage { get; set; }
        public int LastWatchedPosition { get; set; }
        public DateTime LastWatchedAt { get; set; }
        public DateTime StartedAt { get; set; }
        public bool IsCompleted { get; set; }
        public DateTime? CompletedAt { get; set; }
    }

    public class UpdateProgressDto
    {
        public int CourseId { get; set; }
        public int WatchedMinutes { get; set; }
        public int LastWatchedPosition { get; set; }
    }

    public class StudentDashboardDto
    {
        public int TotalEnrolledCourses { get; set; }
        public int CompletedCourses { get; set; }
        public int InProgressCourses { get; set; }
        public int PendingTasks { get; set; }
        public List<CourseProgressDto> RecentProgress { get; set; }
        public List<CourseTaskDto> UpcomingTasks { get; set; }
    }
}