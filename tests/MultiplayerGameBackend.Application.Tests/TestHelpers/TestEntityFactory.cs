using MultiplayerGameBackend.Domain.Constants;
using MultiplayerGameBackend.Domain.Entities;

namespace MultiplayerGameBackend.Application.Tests.TestHelpers;

public static class TestEntityFactory
{
    public static User CreateUser(
        string userName = "testuser",
        string email = "test@example.com",
        Guid? userId = null,
        int balance = 0)
    {
        return new User
        {
            Id = userId ?? Guid.NewGuid(),
            UserName = userName,
            NormalizedUserName = userName.ToUpper(),
            Email = email,
            NormalizedEmail = email.ToUpper(),
            PasswordHash = "AQAAAAIAAYagAAAAEDummyHashForTestingPurposesOnly",
            Balance = balance,
            EmailConfirmed = true
        };
    }
    
    public static Item CreateItem(
        string name = "Test Item",
        string type = ItemTypes.Consumable,
        string? description = null,
        string? thumbnailUrl = null)
    {
        return new Item
        {
            Name = name,
            Description = description ?? $"Description for {name}",
            Type = type,
            ThumbnailUrl = thumbnailUrl ?? "assets/default.png"
        };
    }
    
    public static (Item headItem, Item bodyItem) CreateHeadAndBodyItems()
    {
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

        return (headItem, bodyItem);
    }
    
    public static UserItem CreateUserItem(Guid userId, int itemId)
    {
        return new UserItem
        {
            UserId = userId,
            ItemId = itemId
        };
    }
    
    public static UserCustomization CreateUserCustomization(
        Guid userId,
        string? headColor = null,
        string? bodyColor = null,
        string? tailColor = null,
        string? eyeColor = null,
        string? wingColor = null,
        string? hornColor = null,
        string? markingsColor = null)
    {
        return new UserCustomization
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
    }
    
    public static InGameMerchant CreateMerchant()
    {
        return new InGameMerchant();
    }
    
    public static MerchantItemOffer CreateMerchantItemOffer(int merchantId, int itemId, int price)
    {
        return new MerchantItemOffer
        {
            MerchantId = merchantId,
            ItemId = itemId,
            Price = price
        };
    }
    
    public static UserItemOffer CreateUserItemOffer(
        Guid sellerId,
        Guid userItemId,
        int price,
        Guid? buyerId = null,
        DateTime? boughtAt = null)
    {
        return new UserItemOffer
        {
            UserItemId = userItemId,
            SellerId = sellerId,
            Price = price,
            PublishedAt = DateTime.UtcNow,
            BuyerId = buyerId,
            BoughtAt = boughtAt
        };
    }
    
    public static Microsoft.AspNetCore.Identity.IdentityRole<Guid> CreateRole(string roleName)
    {
        return new Microsoft.AspNetCore.Identity.IdentityRole<Guid>
        {
            Name = roleName,
            NormalizedName = roleName.ToUpper()
        };
    }
}

