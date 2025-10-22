using Microsoft.AspNetCore.Identity;
using MultiplayerGameBackend.Domain.Entities;


namespace MultiplayerGameBackend.Infrastructure.Email;

public class NoOpEmailSender : IEmailSender<User>
{
    public Task SendConfirmationLinkAsync(User user, string email, string confirmationLink)
        => Task.CompletedTask;

    public Task SendPasswordResetLinkAsync(User user, string email, string resetLink)
        => Task.CompletedTask;

    public Task SendPasswordResetCodeAsync(User user, string email, string resetCode)
        => Task.CompletedTask;
}