using Microsoft.Extensions.DependencyInjection;
using MultiplayerGameBackend.Domain.Entities;
using MultiplayerGameBackend.Infrastructure.Persistence;
using MultiplayerGameBackend.Tests.Shared.Helpers;

namespace MultiplayerGameBackend.API.Tests.TestHelpers;

public static class TestDatabaseHelper
{
    public static async Task<Item> AddItemToDatabase(
        IServiceProvider serviceProvider,
        string name,
        string type,
        string? description = null,
        string? thumbnailUrl = null)
    {
        using var scope = serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<MultiplayerGameDbContext>();
        return await DatabaseHelper.CreateAndSaveItem(context, name, type, description, thumbnailUrl);
    }

    public static async Task<(Item headItem, Item bodyItem)> AddHeadAndBodyItemsToDatabase(
        IServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<MultiplayerGameDbContext>();
        return await DatabaseHelper.CreateAndSaveHeadAndBodyItems(context);
    }

    public static async Task<UserItem> AddUserItemToDatabase(
        IServiceProvider serviceProvider,
        Guid userId,
        int itemId)
    {
        using var scope = serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<MultiplayerGameDbContext>();
        return await DatabaseHelper.CreateAndSaveUserItem(context, userId, itemId);
    }

    public static async Task<UserCustomization> AddUserCustomizationToDatabase(
        IServiceProvider serviceProvider,
        Guid userId,
        string? headColor = null,
        string? bodyColor = null,
        string? tailColor = null,
        string? eyeColor = null,
        string? wingColor = null,
        string? hornColor = null,
        string? markingsColor = null)
    {
        using var scope = serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<MultiplayerGameDbContext>();
        return await DatabaseHelper.CreateAndSaveUserCustomization(
            context, userId, headColor, bodyColor, tailColor, eyeColor, wingColor, hornColor, markingsColor);
    }

    public static async Task<UserItemOffer> AddUserItemOfferToDatabase(
        IServiceProvider serviceProvider,
        Guid sellerId,
        Guid userItemId,
        int price)
    {
        using var scope = serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<MultiplayerGameDbContext>();
        return await DatabaseHelper.CreateAndSaveUserItemOffer(context, sellerId, userItemId, price);
    }

    public static async Task<InGameMerchant> AddMerchantToDatabase(
        IServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<MultiplayerGameDbContext>();
        return await DatabaseHelper.CreateAndSaveMerchant(context);
    }

    public static async Task<MerchantItemOffer> AddMerchantItemOfferToDatabase(
        IServiceProvider serviceProvider,
        int merchantId,
        int itemId,
        int price)
    {
        using var scope = serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<MultiplayerGameDbContext>();
        return await DatabaseHelper.CreateAndSaveMerchantItemOffer(context, merchantId, itemId, price);
    }

    public static async Task<(InGameMerchant merchant, Item item, MerchantItemOffer offer)> AddMerchantWithOfferToDatabase(
        IServiceProvider serviceProvider,
        int price)
    {
        using var scope = serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<MultiplayerGameDbContext>();
        return await DatabaseHelper.CreateAndSaveMerchantWithOffer(context, price);
    }

    public static async Task SetUserBalanceInDatabase(
        IServiceProvider serviceProvider,
        Guid userId,
        int balance)
    {
        using var scope = serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<MultiplayerGameDbContext>();
        await DatabaseHelper.UpdateUserBalance(context, userId, balance);
    }
}

