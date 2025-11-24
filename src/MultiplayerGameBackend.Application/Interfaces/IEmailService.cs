using MultiplayerGameBackend.Domain.Entities;

namespace MultiplayerGameBackend.Application.Interfaces;

public interface IEmailService
{
    Task SendPasswordResetEmailAsync(User user, string email, string resetToken);
    Task SendEmailConfirmationAsync(User user, string email, string confirmationToken);
}

