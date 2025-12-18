using DotNet.Testcontainers.Builders;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using MultiplayerGameBackend.Application.Interfaces;
using MultiplayerGameBackend.Domain.Entities;
using MultiplayerGameBackend.Infrastructure.Persistence;
using Testcontainers.PostgreSql;

namespace MultiplayerGameBackend.API.Tests.TestHelpers;

public class CustomWebApplicationFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly PostgreSqlContainer _dbContainer = new PostgreSqlBuilder()
        .WithImage("postgres:18-alpine")
        .WithDatabase("testdb")
        .WithUsername("testuser")
        .WithPassword("testpass")
        .WithWaitStrategy(Wait.ForUnixContainer().UntilPortIsAvailable(5432))
        .Build();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        
        // Add test configuration for JWT
        builder.ConfigureAppConfiguration((context, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string>
            {
                {"Jwt:SecretKey", "ThisIsAVeryLongSecretKeyForTestingPurposesOnly1234567890"},
                {"Jwt:Issuer", "TestIssuer"},
                {"Jwt:Audience", "TestAudience"}
            }!);
        });
        
        builder.ConfigureTestServices(services =>
        {
            // Remove the existing DbContext registration
            var descriptor = services.SingleOrDefault(d => d.ServiceType == typeof(DbContextOptions<MultiplayerGameDbContext>));
            if (descriptor != null)
            {
                services.Remove(descriptor);
            }

            // Add DbContext with test container connection string
            services.AddDbContext<MultiplayerGameDbContext>(options =>
            {
                options.UseNpgsql(_dbContainer.GetConnectionString());
            });
            
            // Replace email service with a mock that does nothing
            services.RemoveAll<IEmailService>();
            services.AddSingleton<IEmailService>(new MockEmailService());
        });
    }
    
    private bool _databaseInitialized = false;
    
    private void EnsureDatabaseCreated()
    {
        if (_databaseInitialized) return;
        
        using var scope = Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<MultiplayerGameDbContext>();
        context.Database.EnsureCreated();
        _databaseInitialized = true;
    }

    public async Task InitializeAsync()
    {
        await _dbContainer.StartAsync();
    }

    public new async Task DisposeAsync()
    {
        await _dbContainer.StopAsync();
        await _dbContainer.DisposeAsync();
    }

    public async Task ResetDatabase()
    {
        EnsureDatabaseCreated();
        
        using var scope = Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<MultiplayerGameDbContext>();
        
        // Clear all data but keep schema
        await context.Database.ExecuteSqlRawAsync("TRUNCATE TABLE \"UserItemOffers\" CASCADE");
        await context.Database.ExecuteSqlRawAsync("TRUNCATE TABLE \"UserItems\" CASCADE");
        await context.Database.ExecuteSqlRawAsync("TRUNCATE TABLE \"MerchantItemOffers\" CASCADE");
        await context.Database.ExecuteSqlRawAsync("TRUNCATE TABLE \"Items\" RESTART IDENTITY CASCADE");
        await context.Database.ExecuteSqlRawAsync("TRUNCATE TABLE \"InGameMerchants\" RESTART IDENTITY CASCADE");
        await context.Database.ExecuteSqlRawAsync("TRUNCATE TABLE \"UserCustomizations\" RESTART IDENTITY CASCADE");
        await context.Database.ExecuteSqlRawAsync("TRUNCATE TABLE \"RefreshTokens\" CASCADE");
        await context.Database.ExecuteSqlRawAsync("TRUNCATE TABLE \"AspNetUserRoles\" CASCADE");
        await context.Database.ExecuteSqlRawAsync("TRUNCATE TABLE \"AspNetUserClaims\" CASCADE");
        await context.Database.ExecuteSqlRawAsync("TRUNCATE TABLE \"AspNetUserLogins\" CASCADE");
        await context.Database.ExecuteSqlRawAsync("TRUNCATE TABLE \"AspNetUserTokens\" CASCADE");
        await context.Database.ExecuteSqlRawAsync("TRUNCATE TABLE \"AspNetUsers\" CASCADE");
        await context.Database.ExecuteSqlRawAsync("TRUNCATE TABLE \"AspNetRoles\" CASCADE");
        await context.Database.ExecuteSqlRawAsync("TRUNCATE TABLE \"FriendRequests\" CASCADE");
    }

    public async Task<User> CreateTestUser(string username, string email, string password, string role = "User")
    {
        EnsureDatabaseCreated();
        
        using var scope = Services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<User>>();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole<Guid>>>();

        // Ensure role exists
        if (!await roleManager.RoleExistsAsync(role))
        {
            await roleManager.CreateAsync(new IdentityRole<Guid> { Name = role, NormalizedName = role.ToUpper() });
        }

        var user = new User
        {
            UserName = username,
            Email = email,
            EmailConfirmed = true
        };

        await userManager.CreateAsync(user, password);
        await userManager.AddToRoleAsync(user, role);

        return user;
    }
}

// Mock email service for testing
public class MockEmailService : IEmailService
{
    public Task SendPasswordResetEmailAsync(User user, string email, string resetToken)
    {
        return Task.CompletedTask;
    }

    public Task SendEmailConfirmationAsync(User user, string email, string confirmationToken)
    {
        return Task.CompletedTask;
    }
}
