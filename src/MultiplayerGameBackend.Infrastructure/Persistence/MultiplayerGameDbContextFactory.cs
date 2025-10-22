using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace MultiplayerGameBackend.Infrastructure.Persistence;

public class MultiplayerGameDbContextFactory : IDesignTimeDbContextFactory<MultiplayerGameDbContext>
{
    public MultiplayerGameDbContext CreateDbContext(string[] args)
    {
        var environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Development";
        var basePath = Path.Combine(Directory.GetCurrentDirectory(), "../MultiplayerGameBackend.API");
        
        var configuration = new ConfigurationBuilder()
            .SetBasePath(basePath)
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
            .AddJsonFile($"appsettings.{environment}.json", optional: true, reloadOnChange: true)
            .AddEnvironmentVariables()
            .Build();

        var connectionString = configuration.GetConnectionString("MultiplayerGameDb");

        var optionsBuilder = new DbContextOptionsBuilder<MultiplayerGameDbContext>();
        optionsBuilder.UseNpgsql(connectionString)
            .EnableSensitiveDataLogging();

        return new MultiplayerGameDbContext(optionsBuilder.Options);
    }
}