using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MultiplayerGameBackend.Infrastructure.Persistence;

namespace MultiplayerGameBackend.Infrastructure.Extensions;

public static class ServiceCollectionExtensions
{
    public static void AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("MultiplayerGameDb");
        services.AddDbContext<MultiplayerGameDbContext>(options =>
            options.UseNpgsql(connectionString)
                .EnableSensitiveDataLogging());
        
        //
        // services.AddIdentityApiEndpoints<User>()
        //     .AddRoles<IdentityRole>()
        //     .AddClaimsPrincipalFactory<RestaurantsUserClaimsPrincipalFactory>()
        //     .AddEntityFrameworkStores<RestaurantsDbContext>();

        //services.AddScoped<IRestaurantSeeder, RestaurantSeeder>();
        //services.AddScoped<IRestaurantsRepository, RestaurantsRepository>();
    }
}