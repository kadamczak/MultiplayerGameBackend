using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using MultiplayerGameBackend.Application.Common.Mappings;
using MultiplayerGameBackend.Application.Identity;
using MultiplayerGameBackend.Application.Items;
using MultiplayerGameBackend.Application.Users;
using MultiplayerGameBackend.Application.Users.Requests.Validators;

namespace MultiplayerGameBackend.Application.Extensions;

public static class ServiceCollectionExtensions
{
    public static void AddApplication(this IServiceCollection services)
    {
        services.AddValidatorsFromAssemblyContaining<ModifyUserRoleDtoValidator>();
        
        services.AddScoped<IUserContext, UserContext>();
        services.AddHttpContextAccessor();
        
        services.AddHostedService<RefreshTokenCleanupService>();
        
        services.AddScoped<IItemService, ItemService>();
        services.AddScoped<IUserService, UserService>();
        services.AddScoped<IIdentityService, IdentityService>();
        
        services.AddScoped<ItemMapper>();
    }
}