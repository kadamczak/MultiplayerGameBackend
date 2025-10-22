using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using MultiplayerGameBackend.Application.Common.Mappings;
using MultiplayerGameBackend.Application.Items;
using MultiplayerGameBackend.Application.Items.Requests.Validators;
using MultiplayerGameBackend.Domain.Entities;

namespace MultiplayerGameBackend.Application.Extensions;

public static class ServiceCollectionExtensions
{
    public static void AddApplication(this IServiceCollection services)
    {
        // add validators
        services.AddValidatorsFromAssemblyContaining<CreateItemDtoValidator>();
        
        services.AddScoped<IItemService, ItemService>();
        
        services.AddScoped<ItemMapper>();
        
        // services.AddScoped<IUserContext, UserContext>();
        // services.AddHttpContextAccessor();
    }
}