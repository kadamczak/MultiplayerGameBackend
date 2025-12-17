using Microsoft.Extensions.DependencyInjection;
using MultiplayerGameBackend.Domain.Entities;
using MultiplayerGameBackend.Infrastructure.Persistence;
using MultiplayerGameBackend.Tests.Shared.Helpers;

namespace MultiplayerGameBackend.API.Tests.TestHelpers;

/// <summary>
/// Wrapper around DatabaseHelper for WebApplicationFactory-based tests.
/// Handles service provider scope creation and context retrieval.
/// </summary>
public static class TestDatabaseHelper
{
    private static async Task<TResult> ExecuteWithContext<TResult>(
        IServiceProvider serviceProvider,
        Func<MultiplayerGameDbContext, Task<TResult>> action)
    {
        using var scope = serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<MultiplayerGameDbContext>();
        return await action(context);
    }

    private static async Task ExecuteWithContext(
        IServiceProvider serviceProvider,
        Func<MultiplayerGameDbContext, Task> action)
    {
        using var scope = serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<MultiplayerGameDbContext>();
        await action(context);
    }

    public static Task<Item> AddItemToDatabase(
        IServiceProvider serviceProvider,
        string name,
        string type,
        string? description = null,
        string? thumbnailUrl = null) =>
        ExecuteWithContext(serviceProvider, context =>
            DatabaseHelper.CreateAndSaveItem(context, name, type, description, thumbnailUrl));

    public static Task<(Item headItem, Item bodyItem)> AddHeadAndBodyItemsToDatabase(
        IServiceProvider serviceProvider) =>
        ExecuteWithContext(serviceProvider, DatabaseHelper.CreateAndSaveHeadAndBodyItems);

    public static Task<UserItem> AddUserItemToDatabase(
        IServiceProvider serviceProvider,
        Guid userId,
        int itemId) =>
        ExecuteWithContext(serviceProvider, context =>
            DatabaseHelper.CreateAndSaveUserItem(context, userId, itemId));

    public static Task<UserCustomization> AddUserCustomizationToDatabase(
        IServiceProvider serviceProvider,
        Guid userId,
        string? headColor = null,
        string? bodyColor = null,
        string? tailColor = null,
        string? eyeColor = null,
        string? wingColor = null,
        string? hornColor = null,
        string? markingsColor = null) =>
        ExecuteWithContext(serviceProvider, context =>
            DatabaseHelper.CreateAndSaveUserCustomization(
                context, userId, headColor, bodyColor, tailColor, eyeColor, wingColor, hornColor, markingsColor));

    public static Task<UserItemOffer> AddUserItemOfferToDatabase(
        IServiceProvider serviceProvider,
        Guid sellerId,
        Guid userItemId,
        int price) =>
        ExecuteWithContext(serviceProvider, context =>
            DatabaseHelper.CreateAndSaveUserItemOffer(context, sellerId, userItemId, price));

    public static Task<InGameMerchant> AddMerchantToDatabase(
        IServiceProvider serviceProvider) =>
        ExecuteWithContext(serviceProvider, DatabaseHelper.CreateAndSaveMerchant);

    public static Task<MerchantItemOffer> AddMerchantItemOfferToDatabase(
        IServiceProvider serviceProvider,
        int merchantId,
        int itemId,
        int price) =>
        ExecuteWithContext(serviceProvider, context =>
            DatabaseHelper.CreateAndSaveMerchantItemOffer(context, merchantId, itemId, price));

    public static Task<(InGameMerchant merchant, Item item, MerchantItemOffer offer)> AddMerchantWithOfferToDatabase(
        IServiceProvider serviceProvider,
        int price) =>
        ExecuteWithContext(serviceProvider, context =>
            DatabaseHelper.CreateAndSaveMerchantWithOffer(context, price));

    public static Task SetUserBalanceInDatabase(
        IServiceProvider serviceProvider,
        Guid userId,
        int balance) =>
        ExecuteWithContext(serviceProvider, context =>
            DatabaseHelper.UpdateUserBalance(context, userId, balance));

    public static Task<FriendRequest> AddFriendRequestToDatabase(
        IServiceProvider serviceProvider,
        Guid requesterId,
        Guid receiverId,
        string status = "Pending",
        DateTime? respondedAt = null) =>
        ExecuteWithContext(serviceProvider, context =>
            DatabaseHelper.CreateAndSaveFriendRequest(context, requesterId, receiverId, status, respondedAt));
}

