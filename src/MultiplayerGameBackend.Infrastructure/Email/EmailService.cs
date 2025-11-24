using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MimeKit;
using MultiplayerGameBackend.Application.Interfaces;
using MultiplayerGameBackend.Domain.Entities;

namespace MultiplayerGameBackend.Infrastructure.Email;

public class EmailService(
    ILogger<EmailService> logger,
    IConfiguration configuration) : IEmailService
{
    public async Task SendPasswordResetEmailAsync(User user, string email, string resetToken)
    {
        var smtpHost = configuration["Email:SmtpHost"];
        var smtpPort = int.Parse(configuration["Email:SmtpPort"] ?? "587");
        var smtpUsername = configuration["Email:SmtpUsername"];
        var smtpPassword = configuration["Email:SmtpPassword"];
        var fromEmail = configuration["Email:FromEmail"];
        var fromName = configuration["Email:FromName"] ?? "Barvon";
        var frontendUrl = configuration["Email:FrontendUrl"] ?? "http://localhost:5173";

        // If SMTP is not configured, fall back to logging
        if (string.IsNullOrWhiteSpace(smtpHost) || string.IsNullOrWhiteSpace(smtpUsername))
        {
            logger.LogWarning("SMTP not configured. Password reset token for {Email}: {ResetToken}", email, resetToken);
            logger.LogInformation("Reset link: {FrontendUrl}/reset-password?token={Token}&email={Email}", 
                frontendUrl, resetToken, email);
            return;
        }

        try
        {
            var message = new MimeMessage();
            message.From.Add(new MailboxAddress(fromName, fromEmail));
            message.To.Add(new MailboxAddress(user.UserName, email));
            message.Subject = "Password Reset Request";
            
            var resetUrl = $"{frontendUrl}/reset-password?token={Uri.EscapeDataString(resetToken)}&email={Uri.EscapeDataString(email)}";
            
            var bodyBuilder = new BodyBuilder
            {
                HtmlBody = $@"
                    <html>
                    <body style='font-family: Arial, sans-serif;'>
                        <h2>Password Reset Request</h2>
                        <p>Hello {user.UserName},</p>
                        <p>You recently requested to reset your password. Click the button below to reset it:</p>
                        <p style='margin: 30px 0;'>
                            <a href='{resetUrl}' 
                               style='background-color: #007bff; color: white; padding: 12px 24px; text-decoration: none; border-radius: 4px; display: inline-block;'>
                                Reset Password
                            </a>
                        </p>
                        <p>Or copy and paste this link into your browser:</p>
                        <p style='color: #666; word-break: break-all;'>{resetUrl}</p>
                        <p style='margin-top: 30px; color: #666;'>
                            If you didn't request a password reset, you can safely ignore this email.
                        </p>
                        <p style='color: #666;'>
                            This link will expire in 24 hours.
                        </p>
                    </body>
                    </html>",
                TextBody = $@"
Password Reset Request

Hello {user.UserName},

You recently requested to reset your password. Click the link below to reset it:

{resetUrl}

If you didn't request a password reset, you can safely ignore this email.

This link will expire in 24 hours."
            };

            message.Body = bodyBuilder.ToMessageBody();

            // Send the email
            using var client = new SmtpClient();
            
            await client.ConnectAsync(smtpHost, smtpPort, SecureSocketOptions.StartTls);
            await client.AuthenticateAsync(smtpUsername, smtpPassword);
            await client.SendAsync(message);
            await client.DisconnectAsync(true);

            logger.LogInformation("Password reset email sent successfully to {Email}", email);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to send password reset email to {Email}", email);
            throw new ApplicationException("Failed to send password reset email. Please try again later.");
        }
    }
    
    public async Task SendEmailConfirmationAsync(User user, string email, string confirmationToken)
    {
        var smtpHost = configuration["Email:SmtpHost"];
        var smtpPort = int.Parse(configuration["Email:SmtpPort"] ?? "587");
        var smtpUsername = configuration["Email:SmtpUsername"];
        var smtpPassword = configuration["Email:SmtpPassword"];
        var fromEmail = configuration["Email:FromEmail"];
        var fromName = configuration["Email:FromName"] ?? "Barvon";
        var frontendUrl = configuration["Email:FrontendUrl"] ?? "http://localhost:5173";
        
        if (string.IsNullOrWhiteSpace(smtpHost) || string.IsNullOrWhiteSpace(smtpUsername))
        {
            logger.LogWarning("SMTP not configured. Email confirmation token for {Email}: {ConfirmationToken}", email, confirmationToken);
            logger.LogInformation("Confirmation link: {FrontendUrl}/confirm-email?token={Token}&email={Email}", 
                frontendUrl, confirmationToken, email);
            return;
        }

        try
        {
            var message = new MimeMessage();
            message.From.Add(new MailboxAddress(fromName, fromEmail));
            message.To.Add(new MailboxAddress(user.UserName, email));
            message.Subject = "Confirm Your Email Address";
            
            var confirmationUrl = $"{frontendUrl}/confirm-email?token={Uri.EscapeDataString(confirmationToken)}&email={Uri.EscapeDataString(email)}";
            
            var bodyBuilder = new BodyBuilder
            {
                HtmlBody = $@"
                    <html>
                    <body style='font-family: Arial, sans-serif;'>
                        <h2>Welcome to Multiplayer Game!</h2>
                        <p>Hello {user.UserName},</p>
                        <p>Thank you for registering! Please confirm your email address by clicking the button below:</p>
                        <p style='margin: 30px 0;'>
                            <a href='{confirmationUrl}' 
                               style='background-color: #28a745; color: white; padding: 12px 24px; text-decoration: none; border-radius: 4px; display: inline-block;'>
                                Confirm Email
                            </a>
                        </p>
                        <p>Or copy and paste this link into your browser:</p>
                        <p style='color: #666; word-break: break-all;'>{confirmationUrl}</p>
                        <p style='margin-top: 30px; color: #666;'>
                            If you didn't create this account, you can safely ignore this email.
                        </p>
                    </body>
                    </html>",
                TextBody = $@"
Welcome to Multiplayer Game!

Hello {user.UserName},

Thank you for registering! Please confirm your email address by clicking the link below:

{confirmationUrl}

If you didn't create this account, you can safely ignore this email."
            };

            message.Body = bodyBuilder.ToMessageBody();

            using var client = new SmtpClient();
            
            await client.ConnectAsync(smtpHost, smtpPort, SecureSocketOptions.StartTls);
            await client.AuthenticateAsync(smtpUsername, smtpPassword);
            await client.SendAsync(message);
            await client.DisconnectAsync(true);

            logger.LogInformation("Email confirmation sent successfully to {Email}", email);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to send confirmation email to {Email}", email);
            throw new ApplicationException("Failed to send confirmation email. Please try again later.");
        }
    }
}

