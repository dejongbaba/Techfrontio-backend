using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Course_management.Models
{
    public class TaskAttachment
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int TaskSubmissionId { get; set; }

        [ForeignKey("TaskSubmissionId")]
        public TaskSubmission TaskSubmission { get; set; }

        [Required]
        public string FileName { get; set; }

        [Required]
        public string FilePath { get; set; }

        public string? ContentType { get; set; }
        
        public long FileSizeBytes { get; set; }
        
        // Timestamps
        public DateTime UploadedAt { get; set; } = DateTime.UtcNow;
        
        [Required]
        public string UploadedByUserId { get; set; }
        
        [ForeignKey("UploadedByUserId")]
        public User UploadedByUser { get; set; }
    }
}