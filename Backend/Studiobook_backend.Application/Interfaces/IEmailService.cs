namespace Studiobook_backend.Application.Interfaces
{
    public interface IEmailService
    {
        Task SendSignupVerificationEmailAsync(string toEmail, string verifyUrl);
        Task SendPasswordResetEmailAsync(string toEmail, string resetUrl);
    }
}