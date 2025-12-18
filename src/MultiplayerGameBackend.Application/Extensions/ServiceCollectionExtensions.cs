using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using MultiplayerGameBackend.Application.Common.Mappings;
using MultiplayerGameBackend.Application.FriendRequests;
using MultiplayerGameBackend.Application.Identity;
using MultiplayerGameBackend.Application.MerchantItemOffers;
using MultiplayerGameBackend.Application.Items;
using MultiplayerGameBackend.Application.UserItemOffers;
using MultiplayerGameBackend.Application.UserItems;
using MultiplayerGameBackend.Application.Users;
using MultiplayerGameBackend.Application.Users.Requests.Validators;

namespace MultiplayerGameBackend.Application.Extensions;

public static class ServiceCollectionExtensions
{
    public static void AddApplication(this IServiceCollection services)
    {
        services.AddValidatorsFromAssemblyContaining<ModifyUserRoleDtoValidator>();
        
        services.AddScoped<IItemService, ItemService>();
        services.AddScoped<IUserService, UserService>();
        services.AddScoped<IIdentityService, IdentityService>();
        services.AddScoped<IUserItemService, UserItemService>();
        services.AddScoped<IUserItemOfferService, UserItemOfferService>();
        services.AddScoped<IMerchantItemOfferService, MerchantItemOfferService>();
        services.AddScoped<IFriendRequestService, FriendRequestService>();
        
        services.AddScoped<ItemMapper>();
        services.AddScoped<UserCustomizationMapper>();
        services.AddScoped<FriendRequestMapper>();
        services.AddScoped<UserItemMapper>();
        services.AddScoped<UserItemOfferMapper>();
    }
}