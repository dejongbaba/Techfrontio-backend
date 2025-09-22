using System;
using System.Collections.Generic;

namespace Course_management.Dto
{
    public class TaskSubmissionDto
    {
        public int Id { get; set; }
        public int TaskId { get; set; }
        public string TaskTitle { get; set; }
        public string StudentId { get; set; }
        public string StudentName { get; set; }
        public string? SubmissionText { get; set; }
        public string Status { get; set; }
        public int? PointsEarned { get; set; }
        public string? TutorFeedback { get; set; }
        public string? GradedByTutorId { get; set; }
        public string? GradedByTutorName { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public DateTime? SubmittedAt { get; set; }
        public DateTime? GradedAt { get; set; }
        public List<TaskAttachmentDto> Attachments { get; set; } = new List<TaskAttachmentDto>();
    }

    public class CreateTaskSubmissionDto
    {
        public int TaskId { get; set; }
        public string? SubmissionText { get; set; }
    }

    public class UpdateTaskSubmissionDto
    {
        public string? SubmissionText { get; set; }
        public string Status { get; set; }
    }

    public class GradeTaskSubmissionDto
    {
        public int PointsEarned { get; set; }
        public string? TutorFeedback { get; set; }
    }

    public class TaskAttachmentDto
    {
        public int Id { get; set; }
        public int TaskSubmissionId { get; set; }
        public string FileName { get; set; }
        public string FilePath { get; set; }
        public string? ContentType { get; set; }
        public long FileSizeBytes { get; set; }
        public DateTime UploadedAt { get; set; }
        public string UploadedByUserId { get; set; }
        public string UploadedByUserName { get; set; }
    }

    public class UploadAttachmentDto
    {
        public int TaskSubmissionId { get; set; }
        public string FileName { get; set; }
        public string ContentType { get; set; }
        public long FileSizeBytes { get; set; }
        public byte[] FileContent { get; set; }
    }
}