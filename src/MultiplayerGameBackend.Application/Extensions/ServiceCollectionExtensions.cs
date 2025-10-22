using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using MultiplayerGameBackend.Application.Common.Mappings;
using MultiplayerGameBackend.Application.Items;
using MultiplayerGameBackend.Application.Items.Requests.Validators;
using MultiplayerGameBackend.Application.Users;

namespace MultiplayerGameBackend.Application.Extensions;

public static class ServiceCollectionExtensions
{
    public static void AddApplication(this IServiceCollection services)
    {
        services.AddValidatorsFromAssemblyContaining<CreateItemDtoValidator>();
        
        services.AddScoped<IUserContext, UserContext>();
        services.AddHttpContextAccessor();
        
        services.AddScoped<IItemService, ItemService>();
        services.AddScoped<IUserService, UserService>();
        
        services.AddScoped<ItemMapper>();
    }
}