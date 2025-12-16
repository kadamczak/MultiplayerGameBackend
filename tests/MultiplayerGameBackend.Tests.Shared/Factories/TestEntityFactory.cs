using Microsoft.AspNetCore.Identity;
using MultiplayerGameBackend.Domain.Constants;
using MultiplayerGameBackend.Domain.Entities;

namespace MultiplayerGameBackend.Tests.Shared.Factories;

/// <summary>
/// Factory for creating test entities without database interaction.
/// Use this for creating in-memory test objects.
/// </summary>
public static class TestEntityFactory
{
    /// <summary>
    /// Creates a test user with minimal required fields
    /// </summary>
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
    
    /// <summary>
    /// Creates a test item
    /// </summary>
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
    
    /// <summary>
    /// Creates test items for head and body slots
    /// </summary>
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
    
    /// <summary>
    /// Creates a test user item
    /// </summary>
    public static UserItem CreateUserItem(Guid userId, int itemId)
    {
        return new UserItem
        {
            UserId = userId,
            ItemId = itemId
        };
    }
    
    /// <summary>
    /// Creates a test user customization
    /// </summary>
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
    
    /// <summary>
    /// Creates a test in-game merchant
    /// </summary>
    public static InGameMerchant CreateMerchant()
    {
        return new InGameMerchant();
    }
    
    /// <summary>
    /// Creates a test merchant item offer
    /// </summary>
    public static MerchantItemOffer CreateMerchantItemOffer(int merchantId, int itemId, int price)
    {
        return new MerchantItemOffer
        {
            MerchantId = merchantId,
            ItemId = itemId,
            Price = price
        };
    }
    
    /// <summary>
    /// Creates a test user item offer
    /// </summary>
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
    
    /// <summary>
    /// Creates a test identity role
    /// </summary>
    public static IdentityRole<Guid> CreateRole(string roleName)
    {
        return new IdentityRole<Guid>
        {
            Name = roleName,
            NormalizedName = roleName.ToUpper()
        };
    }
    
    /// <summary>
    /// Creates a test friend request
    /// </summary>
    public static FriendRequest CreateFriendRequest(
        Guid requesterId,
        Guid receiverId,
        string status = FriendRequestStatuses.Pending,
        DateTime? respondedAt = null)
    {
        return new FriendRequest
        {
            RequesterId = requesterId,
            ReceiverId = receiverId,
            Status = status,
            CreatedAt = DateTime.UtcNow,
            RespondedAt = respondedAt
        };
    }
}

