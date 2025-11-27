using Course_management.Interfaces;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;

namespace Course_management.Services
{
    public class EmailService : IEmailService
    {
        private readonly ILogger<EmailService> _logger;

        public EmailService(ILogger<EmailService> logger)
        {
            _logger = logger;
        }

        public async Task SendPasswordResetEmailAsync(string email, string resetToken, string resetUrl)
        {
            var subject = "Password Reset Request";
            var body = $@"
                <h2>Password Reset Request</h2>
                <p>You have requested to reset your password. Please click the link below to reset your password:</p>
                <p><a href=""{resetUrl}?token={resetToken}&email={email}"">Reset Password</a></p>
                <p>This link will expire in 1 hour.</p>
                <p>If you did not request this password reset, please ignore this email.</p>
            ";

            await SendEmailAsync(email, subject, body);
        }

        public async Task SendEmailAsync(string to, string subject, string body)
        {
            // For demo purposes, we'll just log the email content
            // In a real application, you would integrate with an email service like SendGrid, SMTP, etc.
            _logger.LogInformation($"Email would be sent to: {to}");
            _logger.LogInformation($"Subject: {subject}");
            _logger.LogInformation($"Body: {body}");
            
            // Simulate async email sending
            await Task.Delay(100);
            
            _logger.LogInformation("Email sent successfully (simulated)");
        }
    }
}