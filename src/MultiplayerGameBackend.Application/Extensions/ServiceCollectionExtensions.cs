using Microsoft.Extensions.DependencyInjection;
using MultiplayerGameBackend.Application.Common;

namespace MultiplayerGameBackend.Application.Extensions;

public static class ServiceCollectionExtensions
{
    public static void AddApplication(this IServiceCollection services)
    {
        var applicationAssembly = typeof(ServiceCollectionExtensions).Assembly;
        services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(applicationAssembly));
        
        services.AddAutoMapper(typeof(MappingProfile).Assembly);
        
        //
        // services.AddValidatorsFromAssembly(applicationAssembly)
        //     .AddFluentValidationAutoValidation();
        //
        // services.AddScoped<IUserContext, UserContext>();
        //
        // services.AddHttpContextAccessor();
    }
}