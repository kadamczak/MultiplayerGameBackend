using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using MultiplayerGameBackend.Application.Interfaces;
using MultiplayerGameBackend.Domain.Entities;

namespace MultiplayerGameBackend.Infrastructure.BackgroundServices;

// Cleans accounts that were made over 24 hours ago
// but have not been activated through email.
public class UnactivatedAccountCleanupService(
    IServiceProvider serviceProvider,
    ILogger<UnactivatedAccountCleanupService> logger)
    : BackgroundService
{
    private readonly TimeSpan _interval = TimeSpan.FromHours(24);

    protected override async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        logger.LogInformation("Unactivated accounts cleanup service started.");
    
        using var timer = new PeriodicTimer(_interval);
        try
        {
            await PerformCleanup(cancellationToken);
        
            while (await timer.WaitForNextTickAsync(cancellationToken))
            {
                await PerformCleanup(cancellationToken);
            }
        }
        catch (OperationCanceledException)
        {
            logger.LogInformation("Unactivated accounts cleanup service is stopping.");
        }
    }
    
    private async Task PerformCleanup(CancellationToken cancellationToken)
    {
        try
        {
            await CleanupUnactivatedAccounts(cancellationToken);
            logger.LogInformation("Unactivated accounts cleanup completed.");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error occurred during unactivated accounts cleanup.");
        }
    }

    private async Task CleanupUnactivatedAccounts(CancellationToken cancellationToken)
    {
        using var scope = serviceProvider.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<User>>();
        var dbContext = scope.ServiceProvider.GetRequiredService<IMultiplayerGameDbContext>();

        var cutoffDate = DateTime.UtcNow.AddDays(-1);
        
        var deletedUsersCount = await dbContext.Users
            .Where(u => !u.EmailConfirmed && u.CreatedAt < cutoffDate)
            .ExecuteDeleteAsync(cancellationToken);
        
        logger.LogInformation("Deleted {Count} unactivated accounts.", deletedUsersCount);
    }
}
