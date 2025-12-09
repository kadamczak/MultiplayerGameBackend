using Microsoft.EntityFrameworkCore;
using MultiplayerGameBackend.Infrastructure.Persistence;
using Testcontainers.PostgreSql;

namespace MultiplayerGameBackend.Application.Tests.Common;

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
    
    public async Task ResetDatabaseAsync()
    {
        await using var context = CreateDbContext();
    
        // Disable FK checks, truncate, re-enable
        await context.Database.ExecuteSqlRawAsync("SET session_replication_role = 'replica';");
    
        var tableNames = context.Model.GetEntityTypes()
            .Select(t => t.GetTableName())
            .Where(t => t != null)
            .Distinct();
    
        foreach (var tableName in tableNames)
        {
            await context.Database.ExecuteSqlRawAsync(
                $"TRUNCATE TABLE {tableName} RESTART IDENTITY CASCADE");
        }
    
        await context.Database.ExecuteSqlRawAsync("SET session_replication_role = 'origin';");
    }
}

