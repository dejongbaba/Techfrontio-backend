using System.Threading.Tasks;

namespace Course_management.Interfaces
{
    public interface IEmailService
    {
        Task SendPasswordResetEmailAsync(string email, string resetToken, string resetUrl);
        Task SendEmailAsync(string to, string subject, string body);
    }
}