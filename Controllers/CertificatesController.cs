using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Course_management.Data;
using Course_management.Models;
using Course_management.Dto;
using System.Security.Claims;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace Course_management.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CertificatesController : ControllerBase
    {
        private readonly DataContext _context;

        public CertificatesController(DataContext context)
        {
            _context = context;
        }

        [HttpGet("my")]
        [Authorize(Roles = "Student")]
        public async Task<IActionResult> GetMyCertificates()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var certificates = await _context.Certificates
                .Include(c => c.Course)
                .Where(c => c.StudentId == userId && c.IsActive)
                .Select(c => new
                {
                    c.Id,
                    c.CertificateName,
                    c.CertificateNumber,
                    c.IssuedDate,
                    CourseTitle = c.Course.Title,
                    c.CertificateUrl
                })
                .ToListAsync();

            return Ok(ApiResponse<object>.Success(certificates, "Certificates retrieved", 200));
        }

        [HttpGet("download/{certificateNumber}")]
        [AllowAnonymous]
        public async Task<IActionResult> DownloadCertificate(string certificateNumber)
        {
            var certificate = await _context.Certificates
                .Include(c => c.Course)
                .Include(c => c.User)
                .FirstOrDefaultAsync(c => c.CertificateNumber == certificateNumber);

            if (certificate == null)
                return NotFound("Certificate not found");

            var document = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4.Landscape());
                    page.Margin(2, Unit.Centimetre);
                    page.PageColor(Colors.White);
                    page.DefaultTextStyle(x => x.FontSize(20));

                    page.Header()
                        .Text("TECHFRONT.IO")
                        .SemiBold().FontSize(30).FontColor(Colors.Blue.Medium);

                    page.Content()
                        .PaddingVertical(1, Unit.Centimetre)
                        .Column(x =>
                        {
                            x.Spacing(20);

                            x.Item().AlignCenter().Text("CERTIFICATE OF COMPLETION").FontSize(40).Bold().FontColor(Colors.Grey.Darken3);
                            
                            x.Item().AlignCenter().Text("This is to certify that").FontSize(18).Italic();
                            
                            x.Item().PaddingVertical(10).AlignCenter().Text(certificate.User.FullName ?? "Student")
                                .FontSize(36).Bold().Underline();

                            x.Item().AlignCenter().Text("has successfully completed the course").FontSize(18).Italic();

                            x.Item().PaddingVertical(10).AlignCenter().Text(certificate.Course.Title)
                                .FontSize(30).Bold().FontColor(Colors.Blue.Darken2);

                            x.Item().AlignCenter().Text($"Date Issued: {certificate.IssuedDate:MMMM dd, yyyy}").FontSize(16);
                            x.Item().AlignCenter().Text($"Certificate ID: {certificate.CertificateNumber}").FontSize(12).FontColor(Colors.Grey.Medium);
                        });

                    page.Footer()
                        .AlignCenter()
                        .Text(x =>
                        {
                            x.Span("Page ");
                            x.CurrentPageNumber();
                        });
                });
            });

            var pdfBytes = document.GeneratePdf();
            return File(pdfBytes, "application/pdf", $"Certificate-{certificateNumber}.pdf");
        }

        [HttpGet("{certificateNumber}")]
        [AllowAnonymous]
        public async Task<IActionResult> VerifyCertificate(string certificateNumber)
        {
            var certificate = await _context.Certificates
                .Include(c => c.Course)
                .Include(c => c.User)
                .FirstOrDefaultAsync(c => c.CertificateNumber == certificateNumber);

            if (certificate == null)
                return NotFound(ApiResponse<object>.Error("Certificate not found", 404));

            return Ok(ApiResponse<object>.Success(new
            {
                certificate.Id,
                certificate.CertificateName,
                certificate.CertificateNumber,
                certificate.IssuedDate,
                CourseTitle = certificate.Course.Title,
                CertificateUrl = certificate.CertificateUrl,
                StudentName = certificate.User.FullName ?? "Student",
                VideoDurationMinutes = certificate.Course.VideoDurationMinutes
            }, "Certificate verified successfully", 200));
        }

        [HttpPost("generate/{courseId}")]
        [Authorize(Roles = "Student")]
        public async Task<IActionResult> GenerateCertificate(int courseId)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            // Check if certificate already exists
            var existingCert = await _context.Certificates
                .FirstOrDefaultAsync(c => c.CourseId == courseId && c.StudentId == userId);
            
            if (existingCert != null)
            {
                return Ok(ApiResponse<object>.Success(new { 
                    CertificateId = existingCert.Id,
                    CertificateUrl = existingCert.CertificateUrl,
                    CertificateNumber = existingCert.CertificateNumber
                }, "Certificate already exists", 200));
            }

            // Verify course completion
            // Note: In a real app, you'd check progress more strictly.
            // For now, we assume if they hit this endpoint, they might be eligible, 
            // but we should check the CourseProgress.
            
            var progress = await _context.CourseProgresses
                .FirstOrDefaultAsync(cp => cp.CourseId == courseId && cp.UserId == userId);

            // Strict check: must be marked as completed
            if (progress == null || !progress.IsCompleted)
            {
                 // Allow for testing if progress is 100% but IsCompleted flag not set
                 if (progress != null && progress.ProgressPercentage >= 100)
                 {
                     // Auto-complete if 100%
                     progress.IsCompleted = true;
                     progress.CompletedAt = DateTime.UtcNow;
                     _context.CourseProgresses.Update(progress);
                     await _context.SaveChangesAsync();
                 }
                 else 
                 {
                    return BadRequest(ApiResponse<object>.Error("Course not completed yet. Cannot generate certificate.", 400));
                 }
            }

            var course = await _context.Courses.FindAsync(courseId);
            if (course == null) return NotFound(ApiResponse<object>.Error("Course not found", 404));

            // Generate Certificate Number
            var certNumber = $"CERT-{DateTime.UtcNow.Year}-{Guid.NewGuid().ToString().Substring(0, 8).ToUpper()}";
            
            var certificate = new Certificate
            {
                StudentId = userId,
                CourseId = courseId,
                CertificateName = $"Certificate of Completion - {course.Title}",
                CertificateNumber = certNumber,
                IssuedDate = DateTime.UtcNow,
                IsActive = true,
                CertificateUrl = $"/certificates/view/{certNumber}" 
            };

            _context.Certificates.Add(certificate);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(VerifyCertificate), new { certificateNumber = certNumber }, 
                ApiResponse<object>.Success(new { 
                    CertificateId = certificate.Id, 
                    CertificateNumber = certificate.CertificateNumber,
                    CertificateUrl = certificate.CertificateUrl
                }, "Certificate generated successfully", 201));
        }
    }
}
