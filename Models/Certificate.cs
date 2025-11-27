using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Course_management.Models
{
    public class Certificate
    {
        [Key]
        public int Id { get; set; }
        
        [Required]
        public string StudentId { get; set; }  // Changed from UserId to StudentId to match migration
        
        [Required]
        public int CourseId { get; set; }
        
        [Required]
        [MaxLength(200)]
        public string CertificateName { get; set; }
        
        [Required]
        public string CertificateNumber { get; set; }
        
        public DateTime IssuedDate { get; set; } = DateTime.UtcNow;
        
        public DateTime? ExpiryDate { get; set; }
        
        [MaxLength(500)]
        public string? Description { get; set; }
        
        public string? CertificateUrl { get; set; }
        
        public bool IsActive { get; set; } = true;
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        
        // Navigation properties
        [ForeignKey("StudentId")]  // Changed from UserId to StudentId
        public virtual User User { get; set; }
        
        [ForeignKey("CourseId")]
        public virtual Course Course { get; set; }
    }
}