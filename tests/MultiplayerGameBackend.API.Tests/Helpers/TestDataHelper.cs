using Microsoft.Extensions.DependencyInjection;
using MultiplayerGameBackend.Domain.Constants;
using MultiplayerGameBackend.Domain.Entities;
using MultiplayerGameBackend.Infrastructure.Persistence;

namespace MultiplayerGameBackend.API.Tests.Helpers;

public static class TestDataHelper
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

        var item = new Item
        {
            Name = name,
            Description = description ?? $"Test {name}",
            Type = type,
            ThumbnailUrl = thumbnailUrl ?? "test.png"
        };

        context.Items.Add(item);
        await context.SaveChangesAsync();

        return item;
    }

    public static async Task<(Item headItem, Item bodyItem)> AddHeadAndBodyItemsToDatabase(IServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<MultiplayerGameDbContext>();

        var headItem = new Item
        {
            Name = "Iron Helmet",
            Description = "A sturdy iron helmet",
            Type = ItemTypes.EquippableOnHead,
            ThumbnailUrl = "assets/helmet.png"
        };

        var bodyItem = new Item
        {
            Name = "Steel Sword",
            Description = "A sharp steel sword",
            Type = ItemTypes.EquippableOnBody,
            ThumbnailUrl = "assets/sword.png"
        };

        context.Items.AddRange(headItem, bodyItem);
        await context.SaveChangesAsync();

        return (headItem, bodyItem);
    }

    public static async Task<UserItem> AddUserItemToDatabase(
        IServiceProvider serviceProvider,
        Guid userId,
        int itemId)
    {
        using var scope = serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<MultiplayerGameDbContext>();

        var userItem = new UserItem
        {
            UserId = userId,
            ItemId = itemId
        };

        context.UserItems.Add(userItem);
        await context.SaveChangesAsync();

        return userItem;
    }

    public static async Task AddUserCustomizationToDatabase(
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

        var customization = new UserCustomization
        {
            UserId = userId,
            HeadColor = headColor ?? "#FF0000",
            BodyColor = bodyColor ?? "#00FF00",
            TailColor = tailColor ?? "#0000FF",
            EyeColor = eyeColor ?? "#FFFF00",
            WingColor = wingColor ?? "#FF00FF",
            HornColor = hornColor ?? "#00FFFF",
            MarkingsColor = markingsColor ?? "#FFFFFF"
        };

        context.UserCustomizations.Add(customization);
        await context.SaveChangesAsync();
    }

    public static async Task<UserItemOffer> AddUserItemOfferToDatabase(
        IServiceProvider serviceProvider,
        Guid sellerId,
        Guid userItemId,
        int price)
    {
        using var scope = serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<MultiplayerGameDbContext>();

        var offer = new UserItemOffer
        {
            UserItemId = userItemId,
            SellerId = sellerId,
            Price = price,
            PublishedAt = DateTime.UtcNow
        };

        context.UserItemOffers.Add(offer);
        await context.SaveChangesAsync();

        return offer;
    }

    public static async Task<InGameMerchant> AddMerchantToDatabase(IServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<MultiplayerGameDbContext>();

        var merchant = new InGameMerchant();
        context.InGameMerchants.Add(merchant);
        await context.SaveChangesAsync();

        return merchant;
    }

    public static async Task<MerchantItemOffer> AddMerchantItemOfferToDatabase(
        IServiceProvider serviceProvider,
        int merchantId,
        int itemId,
        int price)
    {
        using var scope = serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<MultiplayerGameDbContext>();

        var offer = new MerchantItemOffer
        {
            MerchantId = merchantId,
            ItemId = itemId,
            Price = price
        };

        context.MerchantItemOffers.Add(offer);
        await context.SaveChangesAsync();

        return offer;
    }

    public static async Task<(InGameMerchant merchant, Item item, MerchantItemOffer offer)> AddMerchantWithOfferToDatabase(
        IServiceProvider serviceProvider,
        int price)
    {
        var merchant = await AddMerchantToDatabase(serviceProvider);
        var item = await AddItemToDatabase(serviceProvider, "Test Item", ItemTypes.Consumable);
        var offer = await AddMerchantItemOfferToDatabase(serviceProvider, merchant.Id, item.Id, price);

        return (merchant, item, offer);
    }

    public static async Task SetUserBalanceInDatabase(
        IServiceProvider serviceProvider,
        Guid userId,
        int balance)
    {
        using var scope = serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<MultiplayerGameDbContext>();

        var user = await context.Users.FindAsync(userId);
        if (user != null)
        {
            user.Balance = balance;
            await context.SaveChangesAsync();
        }
    }
}

