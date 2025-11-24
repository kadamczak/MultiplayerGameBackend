using Microsoft.Extensions.Logging;
using MultiplayerGameBackend.Application.Interfaces;
using MultiplayerGameBackend.Domain.Entities;

namespace MultiplayerGameBackend.Infrastructure.Email;

public class EmailService(ILogger<EmailService> logger) : IEmailService
{
    public Task SendPasswordResetEmailAsync(User user, string email, string resetToken)
    {
        logger.LogInformation(
            "Password Reset Token for {Email}: {ResetToken}",
            email,
            resetToken);
        
        // In production, you would send an actual email with a link like:
        // https://yourdomain.com/reset-password?token={resetToken}&email={email}
        // You would use an email service like SendGrid, AWS SES, etc.
        
        logger.LogInformation(
            "To reset your password, use the following link (in production):");
        logger.LogInformation(
            "https://yourdomain.com/reset-password?token={Token}&email={Email}",
            resetToken,
            email);
        
        return Task.CompletedTask;
    }
}

