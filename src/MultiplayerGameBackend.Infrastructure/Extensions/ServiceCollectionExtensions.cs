using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MultiplayerGameBackend.Application.Interfaces;
using MultiplayerGameBackend.Domain.Entities;
using MultiplayerGameBackend.Infrastructure.Email;
using MultiplayerGameBackend.Infrastructure.Images;
using MultiplayerGameBackend.Infrastructure.Persistence;
using MultiplayerGameBackend.Infrastructure.Seeders;

namespace MultiplayerGameBackend.Infrastructure.Extensions;

public static class ServiceCollectionExtensions
{
    public static void AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("MultiplayerGameDb");
        services.AddDbContext<MultiplayerGameDbContext>(options =>
            options.UseNpgsql(connectionString)
                .EnableSensitiveDataLogging());

        services.AddScoped<IMultiplayerGameDbContext, MultiplayerGameDbContext>();
        services.AddScoped<IMultiplayerGameSeeder, MultiplayerGameSeeder>();
        
        services.AddScoped<IEmailService, EmailService>();
        services.AddScoped<IImageService, ImageService>();
    }
}