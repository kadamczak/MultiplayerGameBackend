using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using MultiplayerGameBackend.Application.Interfaces;

namespace MultiplayerGameBackend.Application.Identity;

public class RefreshTokenCleanupService(
    IServiceProvider serviceProvider,
    ILogger<RefreshTokenCleanupService> logger)
    : BackgroundService
{
    private readonly TimeSpan _interval = TimeSpan.FromHours(24);

    protected override async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        logger.LogInformation("Refresh token cleanup service started.");
    
        using var timer = new PeriodicTimer(_interval);
        try
        {
            // Perform initial cleanup immediately on startup
            await PerformCleanup(cancellationToken);
        
            while (await timer.WaitForNextTickAsync(cancellationToken))
            {
                await PerformCleanup(cancellationToken);
            }
        }
        catch (OperationCanceledException)
        {
            logger.LogInformation("Refresh token cleanup service is stopping.");
        }
    }
    
    private async Task PerformCleanup(CancellationToken cancellationToken)
    {
        try
        {
            await CleanupExpiredTokens(cancellationToken);
            logger.LogInformation("Refresh token cleanup completed.");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error occurred during refresh token cleanup.");
        }
    }

    private async Task CleanupExpiredTokens(CancellationToken cancellationToken)
    {
        using var scope = serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<IMultiplayerGameDbContext>();

        var cutoffDate = DateTime.UtcNow.AddDays(-30); // Keep revoked tokens for 30 days

        var deletedCount = await dbContext.RefreshTokens
            .Where(rt => 
                // Remove all expired tokens
                rt.IsExpired ||
                // Remove revoked tokens older than retention (theft detection window)
                (rt.IsRevoked && rt.RevokedAt < cutoffDate))
            .ExecuteDeleteAsync(cancellationToken);
        
        logger.LogInformation("Deleted {Count} expired/old refresh tokens.", deletedCount);
    }
}
