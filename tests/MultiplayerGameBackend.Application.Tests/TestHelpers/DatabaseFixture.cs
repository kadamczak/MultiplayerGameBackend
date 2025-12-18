using Microsoft.EntityFrameworkCore;
using MultiplayerGameBackend.Infrastructure.Persistence;
using Testcontainers.PostgreSql;

namespace MultiplayerGameBackend.Application.Tests.TestHelpers;

public class DatabaseFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgresContainer = new PostgreSqlBuilder()
        .WithImage("postgres:18")
        .WithDatabase("testdb")
        .WithUsername("testuser")
        .WithPassword("testpass")
        .Build();

    public string ConnectionString => _postgresContainer.GetConnectionString();
    
    public async Task InitializeAsync()
    {
        await _postgresContainer.StartAsync();
        await using var context = CreateDbContext();
        await context.Database.EnsureCreatedAsync();
    }

    public async Task DisposeAsync()
    {
        await _postgresContainer.DisposeAsync();
    }

    public MultiplayerGameDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<MultiplayerGameDbContext>()
            .UseNpgsql(ConnectionString)
            .Options;

        return new MultiplayerGameDbContext(options);
    }
    
    public async Task CleanDatabase()
    {
        await using var context = CreateDbContext();
        
        // Remove all data from tables (in reverse order of dependencies to avoid FK constraints)
        context.UserItemOffers.RemoveRange(context.UserItemOffers);
        context.MerchantItemOffers.RemoveRange(context.MerchantItemOffers);
        context.UserItems.RemoveRange(context.UserItems);
        context.UserCustomizations.RemoveRange(context.UserCustomizations);
        context.Items.RemoveRange(context.Items);
        context.InGameMerchants.RemoveRange(context.InGameMerchants);
        context.FriendRequests.RemoveRange(context.FriendRequests);
        
        // Clean Identity tables
        context.UserRoles.RemoveRange(context.UserRoles);
        context.Users.RemoveRange(context.Users);
        context.Roles.RemoveRange(context.Roles);
        
        await context.SaveChangesAsync();
    }
}

