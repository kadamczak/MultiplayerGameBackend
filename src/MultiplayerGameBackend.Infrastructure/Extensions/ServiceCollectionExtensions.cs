using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace MultiplayerGameBackend.Infrastructure.Extensions;

public static class ServiceCollectionExtensions
{
    public static void AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        // var connectionString = configuration.GetConnectionString("RestaurantsDb");
        // services.AddDbContext<RestaurantsDbContext>(options => 
        //     options.UseSqlServer(connectionString)
        //         .EnableSensitiveDataLogging());
        //
        // services.AddIdentityApiEndpoints<User>()
        //     .AddRoles<IdentityRole>()
        //     .AddClaimsPrincipalFactory<RestaurantsUserClaimsPrincipalFactory>()
        //     .AddEntityFrameworkStores<RestaurantsDbContext>();

        //services.AddScoped<IRestaurantSeeder, RestaurantSeeder>();
        //services.AddScoped<IRestaurantsRepository, RestaurantsRepository>();
    }
}