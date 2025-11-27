using System;
using Course_management.Models;

namespace Course_management.Dto
{
    public class CertificateDto
    {
        public int Id { get; set; }
        public string StudentId { get; set; }
        public int CourseId { get; set; }
        public string CourseTitle { get; set; }
        public string CertificateNumber { get; set; }
        public string Title { get; set; }
        public string? Description { get; set; }
        public string? CertificateUrl { get; set; }
        public decimal FinalScore { get; set; }
        public string Grade { get; set; }
        public DateTime IssuedAt { get; set; }
        public bool IsActive { get; set; }
    }

    public class CreateCertificateDto
    {
        public User Student { get; set; }
        public int CourseId { get; set; }
        public string Title { get; set; }
        public string? Description { get; set; }
        public decimal FinalScore { get; set; }
        public string Grade { get; set; } = "Pass";
        public string? CertificateUrl { get; set; }
    }
}