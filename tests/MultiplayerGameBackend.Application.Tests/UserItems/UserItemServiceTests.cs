using Microsoft.Extensions.Logging;
using MultiplayerGameBackend.Application.Common;
using MultiplayerGameBackend.Application.Tests.TestHelpers;
using MultiplayerGameBackend.Application.UserItems;
using MultiplayerGameBackend.Application.UserItems.Requests;
using MultiplayerGameBackend.Domain.Constants;
using MultiplayerGameBackend.Domain.Entities;
using MultiplayerGameBackend.Domain.Exceptions;
using NSubstitute;

namespace MultiplayerGameBackend.Application.Tests.UserItems;

public class UserItemServiceTests : IClassFixture<DatabaseFixture>, IAsyncLifetime
{
    private readonly DatabaseFixture _fixture;
    private readonly ILogger<UserItemService> _logger;

    public UserItemServiceTests(DatabaseFixture fixture)
    {
        _fixture = fixture;
        _logger = Substitute.For<ILogger<UserItemService>>();
    }

    public Task InitializeAsync() => Task.CompletedTask;

    public async Task DisposeAsync() => await _fixture.CleanDatabase();

    #region GetCurrentUserItems Tests

    [Fact]
    public async Task GetCurrentUserItems_ShouldReturnUserItems_WhenUserHasItems()
    {
        // Arrange
        await using var context = _fixture.CreateDbContext();
        var service = new UserItemService(_logger, context);

        var userId = Guid.NewGuid();
        var user = new User
        {
            Id = userId,
            UserName = "testuser",
            NormalizedUserName = "TESTUSER",
            Email = "test@example.com",
            NormalizedEmail = "TEST@EXAMPLE.COM",
            PasswordHash = "AQAAAAIAAYagAAAAEDummyHashForTestingPurposesOnly"
        };
        context.Users.Add(user);

        var item1 = new Item
        {
            Name = "Iron Helmet",
            Description = "A sturdy iron helmet",
            Type = ItemTypes.EquippableOnHead,
            ThumbnailUrl = "assets/helmet.png"
        };
        var item2 = new Item
        {
            Name = "Steel Sword",
            Description = "A sharp steel sword",
            Type = ItemTypes.EquippableOnBody,
            ThumbnailUrl = "assets/sword.png"
        };
        context.Items.AddRange(item1, item2);
        await context.SaveChangesAsync();

        var userItem1 = new UserItem { UserId = userId, ItemId = item1.Id };
        var userItem2 = new UserItem { UserId = userId, ItemId = item2.Id };
        context.UserItems.AddRange(userItem1, userItem2);
        await context.SaveChangesAsync();

        var query = new PagedQuery { PageNumber = 1, PageSize = 10 };

        // Act
        var result = await service.GetCurrentUserItems(userId, query, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.TotalItemsCount);
        Assert.Equal(2, result.Items.Count());
        Assert.Contains(result.Items, i => i.Item.Name == "Iron Helmet");
        Assert.Contains(result.Items, i => i.Item.Name == "Steel Sword");
    }

    [Fact]
    public async Task GetCurrentUserItems_ShouldReturnEmptyList_WhenUserHasNoItems()
    {
        // Arrange
        await using var context = _fixture.CreateDbContext();
        var service = new UserItemService(_logger, context);

        var userId = Guid.NewGuid();
        var query = new PagedQuery { PageNumber = 1, PageSize = 10 };

        // Act
        var result = await service.GetCurrentUserItems(userId, query, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(0, result.TotalItemsCount);
        Assert.Empty(result.Items);
    }

    [Fact]
    public async Task GetCurrentUserItems_ShouldReturnOnlyUserItems_NotOtherUsersItems()
    {
        // Arrange
        await using var context = _fixture.CreateDbContext();
        var service = new UserItemService(_logger, context);

        var userId1 = Guid.NewGuid();
        var userId2 = Guid.NewGuid();

        var user1 = new User
        {
            Id = userId1,
            UserName = "user1",
            NormalizedUserName = "USER1",
            Email = "user1@example.com",
            NormalizedEmail = "USER1@EXAMPLE.COM",
            PasswordHash = "AQAAAAIAAYagAAAAEDummyHashForTestingPurposesOnly"
        };
        var user2 = new User
        {
            Id = userId2,
            UserName = "user2",
            NormalizedUserName = "USER2",
            Email = "user2@example.com",
            NormalizedEmail = "USER2@EXAMPLE.COM",
            PasswordHash = "AQAAAAIAAYagAAAAEDummyHashForTestingPurposesOnly"
        };
        context.Users.AddRange(user1, user2);

        var item1 = new Item
        {
            Name = "User1 Item",
            Description = "Item for user 1",
            Type = ItemTypes.Consumable,
            ThumbnailUrl = "assets/item1.png"
        };
        var item2 = new Item
        {
            Name = "User2 Item",
            Description = "Item for user 2",
            Type = ItemTypes.Consumable,
            ThumbnailUrl = "assets/item2.png"
        };
        context.Items.AddRange(item1, item2);
        await context.SaveChangesAsync();

        var userItem1 = new UserItem { UserId = userId1, ItemId = item1.Id };
        var userItem2 = new UserItem { UserId = userId2, ItemId = item2.Id };
        context.UserItems.AddRange(userItem1, userItem2);
        await context.SaveChangesAsync();

        var query = new PagedQuery { PageNumber = 1, PageSize = 10 };

        // Act
        var result = await service.GetCurrentUserItems(userId1, query, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Single(result.Items);
        Assert.Equal("User1 Item", result.Items.First().Item.Name);
    }

    [Fact]
    public async Task GetCurrentUserItems_ShouldFilterBySearchPhrase()
    {
        // Arrange
        await using var context = _fixture.CreateDbContext();
        var service = new UserItemService(_logger, context);

        var userId = Guid.NewGuid();
        var user = new User
        {
            Id = userId,
            UserName = "testuser",
            NormalizedUserName = "TESTUSER",
            Email = "test@example.com",
            NormalizedEmail = "TEST@EXAMPLE.COM",
            PasswordHash = "AQAAAAIAAYagAAAAEDummyHashForTestingPurposesOnly"
        };
        context.Users.Add(user);

        var item1 = new Item
        {
            Name = "Magic Sword",
            Description = "A magical weapon",
            Type = ItemTypes.EquippableOnBody,
            ThumbnailUrl = "assets/sword.png"
        };
        var item2 = new Item
        {
            Name = "Health Potion",
            Description = "Restores health",
            Type = ItemTypes.Consumable,
            ThumbnailUrl = "assets/potion.png"
        };
        context.Items.AddRange(item1, item2);
        await context.SaveChangesAsync();

        var userItem1 = new UserItem { UserId = userId, ItemId = item1.Id };
        var userItem2 = new UserItem { UserId = userId, ItemId = item2.Id };
        context.UserItems.AddRange(userItem1, userItem2);
        await context.SaveChangesAsync();

        var query = new PagedQuery { PageNumber = 1, PageSize = 10, SearchPhrase = "sword" };

        // Act
        var result = await service.GetCurrentUserItems(userId, query, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Single(result.Items);
        Assert.Equal("Magic Sword", result.Items.First().Item.Name);
    }

    [Fact]
    public async Task GetCurrentUserItems_ShouldSortByNameAscending()
    {
        // Arrange
        await using var context = _fixture.CreateDbContext();
        var service = new UserItemService(_logger, context);

        var userId = Guid.NewGuid();
        var user = new User
        {
            Id = userId,
            UserName = "testuser",
            NormalizedUserName = "TESTUSER",
            Email = "test@example.com",
            NormalizedEmail = "TEST@EXAMPLE.COM",
            PasswordHash = "AQAAAAIAAYagAAAAEDummyHashForTestingPurposesOnly"
        };
        context.Users.Add(user);

        var itemZ = new Item
        {
            Name = "Zebra Shield",
            Description = "Shield",
            Type = ItemTypes.EquippableOnBody,
            ThumbnailUrl = "assets/shield.png"
        };
        var itemA = new Item
        {
            Name = "Apple Potion",
            Description = "Potion",
            Type = ItemTypes.Consumable,
            ThumbnailUrl = "assets/potion.png"
        };
        context.Items.AddRange(itemZ, itemA);
        await context.SaveChangesAsync();

        var userItemZ = new UserItem { UserId = userId, ItemId = itemZ.Id };
        var userItemA = new UserItem { UserId = userId, ItemId = itemA.Id };
        context.UserItems.AddRange(userItemZ, userItemA);
        await context.SaveChangesAsync();

        var query = new PagedQuery
        {
            PageNumber = 1,
            PageSize = 10,
            SortBy = "Name",
            SortDirection = SortDirection.Ascending
        };

        // Act
        var result = await service.GetCurrentUserItems(userId, query, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.Items.Count());
        Assert.Equal("Apple Potion", result.Items.First().Item.Name);
        Assert.Equal("Zebra Shield", result.Items.Last().Item.Name);
    }

    [Fact]
    public async Task GetCurrentUserItems_ShouldSortByNameDescending()
    {
        // Arrange
        await using var context = _fixture.CreateDbContext();
        var service = new UserItemService(_logger, context);

        var userId = Guid.NewGuid();
        var user = new User
        {
            Id = userId,
            UserName = "testuser",
            NormalizedUserName = "TESTUSER",
            Email = "test@example.com",
            NormalizedEmail = "TEST@EXAMPLE.COM",
            PasswordHash = "AQAAAAIAAYagAAAAEDummyHashForTestingPurposesOnly"
        };
        context.Users.Add(user);

        var itemZ = new Item
        {
            Name = "Zebra Shield",
            Description = "Shield",
            Type = ItemTypes.EquippableOnBody,
            ThumbnailUrl = "assets/shield.png"
        };
        var itemA = new Item
        {
            Name = "Apple Potion",
            Description = "Potion",
            Type = ItemTypes.Consumable,
            ThumbnailUrl = "assets/potion.png"
        };
        context.Items.AddRange(itemZ, itemA);
        await context.SaveChangesAsync();

        var userItemZ = new UserItem { UserId = userId, ItemId = itemZ.Id };
        var userItemA = new UserItem { UserId = userId, ItemId = itemA.Id };
        context.UserItems.AddRange(userItemZ, userItemA);
        await context.SaveChangesAsync();

        var query = new PagedQuery
        {
            PageNumber = 1,
            PageSize = 10,
            SortBy = "Name",
            SortDirection = SortDirection.Descending
        };

        // Act
        var result = await service.GetCurrentUserItems(userId, query, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.Items.Count());
        Assert.Equal("Zebra Shield", result.Items.First().Item.Name);
        Assert.Equal("Apple Potion", result.Items.Last().Item.Name);
    }

    [Fact]
    public async Task GetCurrentUserItems_ShouldReturnPaginatedResults()
    {
        // Arrange
        await using var context = _fixture.CreateDbContext();
        var service = new UserItemService(_logger, context);

        var userId = Guid.NewGuid();
        var user = new User
        {
            Id = userId,
            UserName = "testuser",
            NormalizedUserName = "TESTUSER",
            Email = "test@example.com",
            NormalizedEmail = "TEST@EXAMPLE.COM",
            PasswordHash = "AQAAAAIAAYagAAAAEDummyHashForTestingPurposesOnly"
        };
        context.Users.Add(user);

        // Create 5 items
        for (int i = 1; i <= 5; i++)
        {
            var item = new Item
            {
                Name = $"Item {i}",
                Description = $"Description {i}",
                Type = ItemTypes.Consumable,
                ThumbnailUrl = $"assets/item{i}.png"
            };
            context.Items.Add(item);
            await context.SaveChangesAsync();

            var userItem = new UserItem { UserId = userId, ItemId = item.Id };
            context.UserItems.Add(userItem);
        }
        await context.SaveChangesAsync();

        var query = new PagedQuery { PageNumber = 1, PageSize = 2 };

        // Act
        var result = await service.GetCurrentUserItems(userId, query, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(5, result.TotalItemsCount);
        Assert.Equal(2, result.Items.Count());
        Assert.Equal(3, result.TotalPages);
    }

    [Fact]
    public async Task GetCurrentUserItems_ShouldIncludeActiveOfferInfo_WhenItemHasActiveOffer()
    {
        // Arrange
        await using var context = _fixture.CreateDbContext();
        var service = new UserItemService(_logger, context);

        var userId = Guid.NewGuid();
        var user = new User
        {
            Id = userId,
            UserName = "testuser",
            NormalizedUserName = "TESTUSER",
            Email = "test@example.com",
            NormalizedEmail = "TEST@EXAMPLE.COM",
            PasswordHash = "AQAAAAIAAYagAAAAEDummyHashForTestingPurposesOnly"
        };
        context.Users.Add(user);

        var item = new Item
        {
            Name = "Rare Sword",
            Description = "A rare sword",
            Type = ItemTypes.EquippableOnBody,
            ThumbnailUrl = "assets/sword.png"
        };
        context.Items.Add(item);
        await context.SaveChangesAsync();

        var userItem = new UserItem { UserId = userId, ItemId = item.Id };
        context.UserItems.Add(userItem);
        await context.SaveChangesAsync();

        var offer = new UserItemOffer
        {
            UserItemId = userItem.Id,
            SellerId = userId,
            Price = 100,
            BuyerId = null // Active offer
        };
        context.UserItemOffers.Add(offer);
        await context.SaveChangesAsync();

        var query = new PagedQuery { PageNumber = 1, PageSize = 10 };

        // Act
        var result = await service.GetCurrentUserItems(userId, query, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Single(result.Items);
        var itemResult = result.Items.First();
        Assert.NotNull(itemResult.ActiveOfferId);
        Assert.Equal(offer.Id, itemResult.ActiveOfferId);
        Assert.Equal(100, itemResult.ActiveOfferPrice);
    }

    [Fact]
    public async Task GetCurrentUserItems_ShouldNotIncludeActiveOfferInfo_WhenOfferIsSold()
    {
        // Arrange
        await using var context = _fixture.CreateDbContext();
        var service = new UserItemService(_logger, context);

        var userId = Guid.NewGuid();
        var buyerId = Guid.NewGuid();
        var user = new User
        {
            Id = userId,
            UserName = "testuser",
            NormalizedUserName = "TESTUSER",
            Email = "test@example.com",
            NormalizedEmail = "TEST@EXAMPLE.COM",
            PasswordHash = "AQAAAAIAAYagAAAAEDummyHashForTestingPurposesOnly"
        };
        var buyer = new User
        {
            Id = buyerId,
            UserName = "buyer",
            NormalizedUserName = "BUYER",
            Email = "buyer@example.com",
            NormalizedEmail = "BUYER@EXAMPLE.COM",
            PasswordHash = "AQAAAAIAAYagAAAAEDummyHashForTestingPurposesOnly"
        };
        context.Users.AddRange(user, buyer);

        var item = new Item
        {
            Name = "Sold Sword",
            Description = "A sold sword",
            Type = ItemTypes.EquippableOnBody,
            ThumbnailUrl = "assets/sword.png"
        };
        context.Items.Add(item);
        await context.SaveChangesAsync();

        var userItem = new UserItem { UserId = userId, ItemId = item.Id };
        context.UserItems.Add(userItem);
        await context.SaveChangesAsync();

        var offer = new UserItemOffer
        {
            UserItemId = userItem.Id,
            SellerId = userId,
            Price = 100,
            BuyerId = buyerId // Sold offer
        };
        context.UserItemOffers.Add(offer);
        await context.SaveChangesAsync();

        var query = new PagedQuery { PageNumber = 1, PageSize = 10 };

        // Act
        var result = await service.GetCurrentUserItems(userId, query, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Single(result.Items);
        var itemResult = result.Items.First();
        Assert.Null(itemResult.ActiveOfferId);
        Assert.Null(itemResult.ActiveOfferPrice);
    }

    #endregion

    #region UpdateEquippedUserItems Tests

    [Fact]
    public async Task UpdateEquippedUserItems_ShouldUpdateHeadItem_WhenValid()
    {
        // Arrange
        await using var context = _fixture.CreateDbContext();
        var service = new UserItemService(_logger, context);

        var userId = Guid.NewGuid();
        var user = new User
        {
            Id = userId,
            UserName = "testuser",
            NormalizedUserName = "TESTUSER",
            Email = "test@example.com",
            NormalizedEmail = "TEST@EXAMPLE.COM",
            PasswordHash = "AQAAAAIAAYagAAAAEDummyHashForTestingPurposesOnly"
        };
        context.Users.Add(user);

        var customization = new UserCustomization
        {
            UserId = userId,
            HeadColor = "#FF0000",
            BodyColor = "#00FF00",
            TailColor = "#0000FF",
            EyeColor = "#FFFF00",
            WingColor = "#FF00FF",
            HornColor = "#00FFFF",
            MarkingsColor = "#FFFFFF"
        };
        context.UserCustomizations.Add(customization);

        var headItem = new Item
        {
            Name = "Cool Helmet",
            Description = "A cool helmet",
            Type = ItemTypes.EquippableOnHead,
            ThumbnailUrl = "assets/helmet.png"
        };
        context.Items.Add(headItem);
        await context.SaveChangesAsync();

        var userHeadItem = new UserItem { UserId = userId, ItemId = headItem.Id };
        context.UserItems.Add(userHeadItem);
        await context.SaveChangesAsync();

        var dto = new UpdateEquippedUserItemsDto
        {
            EquippedHeadUserItemId = userHeadItem.Id
        };

        // Act
        await service.UpdateEquippedUserItems(userId, dto, CancellationToken.None);

        // Assert
        var updatedCustomization = context.UserCustomizations.First(c => c.UserId == userId);
        Assert.Equal(userHeadItem.Id, updatedCustomization.EquippedHeadUserItemId);
    }

    [Fact]
    public async Task UpdateEquippedUserItems_ShouldUpdateBodyItem_WhenValid()
    {
        // Arrange
        await using var context = _fixture.CreateDbContext();
        var service = new UserItemService(_logger, context);

        var userId = Guid.NewGuid();
        var user = new User
        {
            Id = userId,
            UserName = "testuser",
            NormalizedUserName = "TESTUSER",
            Email = "test@example.com",
            NormalizedEmail = "TEST@EXAMPLE.COM",
            PasswordHash = "AQAAAAIAAYagAAAAEDummyHashForTestingPurposesOnly"
        };
        context.Users.Add(user);

        var customization = new UserCustomization
        {
            UserId = userId,
            HeadColor = "#FF0000",
            BodyColor = "#00FF00",
            TailColor = "#0000FF",
            EyeColor = "#FFFF00",
            WingColor = "#FF00FF",
            HornColor = "#00FFFF",
            MarkingsColor = "#FFFFFF"
        };
        context.UserCustomizations.Add(customization);

        var bodyItem = new Item
        {
            Name = "Epic Armor",
            Description = "Epic armor",
            Type = ItemTypes.EquippableOnBody,
            ThumbnailUrl = "assets/armor.png"
        };
        context.Items.Add(bodyItem);
        await context.SaveChangesAsync();

        var userBodyItem = new UserItem { UserId = userId, ItemId = bodyItem.Id };
        context.UserItems.Add(userBodyItem);
        await context.SaveChangesAsync();

        var dto = new UpdateEquippedUserItemsDto
        {
            EquippedBodyUserItemId = userBodyItem.Id
        };

        // Act
        await service.UpdateEquippedUserItems(userId, dto, CancellationToken.None);

        // Assert
        var updatedCustomization = context.UserCustomizations.First(c => c.UserId == userId);
        Assert.Equal(userBodyItem.Id, updatedCustomization.EquippedBodyUserItemId);
    }

    [Fact]
    public async Task UpdateEquippedUserItems_ShouldUpdateBothItems_WhenValid()
    {
        // Arrange
        await using var context = _fixture.CreateDbContext();
        var service = new UserItemService(_logger, context);

        var userId = Guid.NewGuid();
        var user = new User
        {
            Id = userId,
            UserName = "testuser",
            NormalizedUserName = "TESTUSER",
            Email = "test@example.com",
            NormalizedEmail = "TEST@EXAMPLE.COM",
            PasswordHash = "AQAAAAIAAYagAAAAEDummyHashForTestingPurposesOnly"
        };
        context.Users.Add(user);

        var customization = new UserCustomization
        {
            UserId = userId,
            HeadColor = "#FF0000",
            BodyColor = "#00FF00",
            TailColor = "#0000FF",
            EyeColor = "#FFFF00",
            WingColor = "#FF00FF",
            HornColor = "#00FFFF",
            MarkingsColor = "#FFFFFF"
        };
        context.UserCustomizations.Add(customization);

        var headItem = new Item
        {
            Name = "Cool Helmet",
            Description = "A cool helmet",
            Type = ItemTypes.EquippableOnHead,
            ThumbnailUrl = "assets/helmet.png"
        };
        var bodyItem = new Item
        {
            Name = "Epic Armor",
            Description = "Epic armor",
            Type = ItemTypes.EquippableOnBody,
            ThumbnailUrl = "assets/armor.png"
        };
        context.Items.AddRange(headItem, bodyItem);
        await context.SaveChangesAsync();

        var userHeadItem = new UserItem { UserId = userId, ItemId = headItem.Id };
        var userBodyItem = new UserItem { UserId = userId, ItemId = bodyItem.Id };
        context.UserItems.AddRange(userHeadItem, userBodyItem);
        await context.SaveChangesAsync();

        var dto = new UpdateEquippedUserItemsDto
        {
            EquippedHeadUserItemId = userHeadItem.Id,
            EquippedBodyUserItemId = userBodyItem.Id
        };

        // Act
        await service.UpdateEquippedUserItems(userId, dto, CancellationToken.None);

        // Assert
        var updatedCustomization = context.UserCustomizations.First(c => c.UserId == userId);
        Assert.Equal(userHeadItem.Id, updatedCustomization.EquippedHeadUserItemId);
        Assert.Equal(userBodyItem.Id, updatedCustomization.EquippedBodyUserItemId);
    }

    [Fact]
    public async Task UpdateEquippedUserItems_ShouldUnequipItems_WhenPassedNull()
    {
        // Arrange
        await using var context = _fixture.CreateDbContext();
        var service = new UserItemService(_logger, context);

        var userId = Guid.NewGuid();
        var user = new User
        {
            Id = userId,
            UserName = "testuser",
            NormalizedUserName = "TESTUSER",
            Email = "test@example.com",
            NormalizedEmail = "TEST@EXAMPLE.COM",
            PasswordHash = "AQAAAAIAAYagAAAAEDummyHashForTestingPurposesOnly"
        };
        context.Users.Add(user);

        var headItem = new Item
        {
            Name = "Cool Helmet",
            Description = "A cool helmet",
            Type = ItemTypes.EquippableOnHead,
            ThumbnailUrl = "assets/helmet.png"
        };
        context.Items.Add(headItem);
        await context.SaveChangesAsync();

        var userHeadItem = new UserItem { UserId = userId, ItemId = headItem.Id };
        context.UserItems.Add(userHeadItem);

        var customization = new UserCustomization
        {
            UserId = userId,
            HeadColor = "#FF0000",
            BodyColor = "#00FF00",
            TailColor = "#0000FF",
            EyeColor = "#FFFF00",
            WingColor = "#FF00FF",
            HornColor = "#00FFFF",
            MarkingsColor = "#FFFFFF",
            EquippedHeadUserItemId = userHeadItem.Id
        };
        context.UserCustomizations.Add(customization);
        await context.SaveChangesAsync();

        var dto = new UpdateEquippedUserItemsDto
        {
            EquippedHeadUserItemId = null,
            EquippedBodyUserItemId = null
        };

        // Act
        await service.UpdateEquippedUserItems(userId, dto, CancellationToken.None);

        // Assert
        var updatedCustomization = context.UserCustomizations.First(c => c.UserId == userId);
        Assert.Null(updatedCustomization.EquippedHeadUserItemId);
        Assert.Null(updatedCustomization.EquippedBodyUserItemId);
    }

    [Fact]
    public async Task UpdateEquippedUserItems_ShouldThrowNotFoundException_WhenHeadItemDoesNotExist()
    {
        // Arrange
        await using var context = _fixture.CreateDbContext();
        var service = new UserItemService(_logger, context);

        var userId = Guid.NewGuid();
        var user = new User
        {
            Id = userId,
            UserName = "testuser",
            NormalizedUserName = "TESTUSER",
            Email = "test@example.com",
            NormalizedEmail = "TEST@EXAMPLE.COM",
            PasswordHash = "AQAAAAIAAYagAAAAEDummyHashForTestingPurposesOnly"
        };
        context.Users.Add(user);

        var customization = new UserCustomization
        {
            UserId = userId,
            HeadColor = "#FF0000",
            BodyColor = "#00FF00",
            TailColor = "#0000FF",
            EyeColor = "#FFFF00",
            WingColor = "#FF00FF",
            HornColor = "#00FFFF",
            MarkingsColor = "#FFFFFF"
        };
        context.UserCustomizations.Add(customization);
        await context.SaveChangesAsync();

        var dto = new UpdateEquippedUserItemsDto
        {
            EquippedHeadUserItemId = Guid.NewGuid() // Non-existent item
        };

        // Act & Assert
        await Assert.ThrowsAsync<NotFoundException>(
            () => service.UpdateEquippedUserItems(userId, dto, CancellationToken.None)
        );
    }

    [Fact]
    public async Task UpdateEquippedUserItems_ShouldThrowNotFoundException_WhenBodyItemDoesNotExist()
    {
        // Arrange
        await using var context = _fixture.CreateDbContext();
        var service = new UserItemService(_logger, context);

        var userId = Guid.NewGuid();
        var user = new User
        {
            Id = userId,
            UserName = "testuser",
            NormalizedUserName = "TESTUSER",
            Email = "test@example.com",
            NormalizedEmail = "TEST@EXAMPLE.COM",
            PasswordHash = "AQAAAAIAAYagAAAAEDummyHashForTestingPurposesOnly"
        };
        context.Users.Add(user);

        var customization = new UserCustomization
        {
            UserId = userId,
            HeadColor = "#FF0000",
            BodyColor = "#00FF00",
            TailColor = "#0000FF",
            EyeColor = "#FFFF00",
            WingColor = "#FF00FF",
            HornColor = "#00FFFF",
            MarkingsColor = "#FFFFFF"
        };
        context.UserCustomizations.Add(customization);
        await context.SaveChangesAsync();

        var dto = new UpdateEquippedUserItemsDto
        {
            EquippedBodyUserItemId = Guid.NewGuid() // Non-existent item
        };

        // Act & Assert
        await Assert.ThrowsAsync<NotFoundException>(
            () => service.UpdateEquippedUserItems(userId, dto, CancellationToken.None)
        );
    }

    [Fact]
    public async Task UpdateEquippedUserItems_ShouldThrowForbidException_WhenHeadItemDoesNotBelongToUser()
    {
        // Arrange
        await using var context = _fixture.CreateDbContext();
        var service = new UserItemService(_logger, context);

        var userId = Guid.NewGuid();
        var otherUserId = Guid.NewGuid();

        var user = new User
        {
            Id = userId,
            UserName = "testuser",
            NormalizedUserName = "TESTUSER",
            Email = "test@example.com",
            NormalizedEmail = "TEST@EXAMPLE.COM",
            PasswordHash = "AQAAAAIAAYagAAAAEDummyHashForTestingPurposesOnly"
        };
        var otherUser = new User
        {
            Id = otherUserId,
            UserName = "otheruser",
            NormalizedUserName = "OTHERUSER",
            Email = "other@example.com",
            NormalizedEmail = "OTHER@EXAMPLE.COM",
            PasswordHash = "AQAAAAIAAYagAAAAEDummyHashForTestingPurposesOnly"
        };
        context.Users.AddRange(user, otherUser);

        var customization = new UserCustomization
        {
            UserId = userId,
            HeadColor = "#FF0000",
            BodyColor = "#00FF00",
            TailColor = "#0000FF",
            EyeColor = "#FFFF00",
            WingColor = "#FF00FF",
            HornColor = "#00FFFF",
            MarkingsColor = "#FFFFFF"
        };
        context.UserCustomizations.Add(customization);

        var headItem = new Item
        {
            Name = "Other's Helmet",
            Description = "Someone else's helmet",
            Type = ItemTypes.EquippableOnHead,
            ThumbnailUrl = "assets/helmet.png"
        };
        context.Items.Add(headItem);
        await context.SaveChangesAsync();

        var otherUserItem = new UserItem { UserId = otherUserId, ItemId = headItem.Id };
        context.UserItems.Add(otherUserItem);
        await context.SaveChangesAsync();

        var dto = new UpdateEquippedUserItemsDto
        {
            EquippedHeadUserItemId = otherUserItem.Id
        };

        // Act & Assert
        await Assert.ThrowsAsync<ForbidException>(
            () => service.UpdateEquippedUserItems(userId, dto, CancellationToken.None)
        );
    }

    [Fact]
    public async Task UpdateEquippedUserItems_ShouldThrowForbidException_WhenBodyItemDoesNotBelongToUser()
    {
        // Arrange
        await using var context = _fixture.CreateDbContext();
        var service = new UserItemService(_logger, context);

        var userId = Guid.NewGuid();
        var otherUserId = Guid.NewGuid();

        var user = new User
        {
            Id = userId,
            UserName = "testuser",
            NormalizedUserName = "TESTUSER",
            Email = "test@example.com",
            NormalizedEmail = "TEST@EXAMPLE.COM",
            PasswordHash = "AQAAAAIAAYagAAAAEDummyHashForTestingPurposesOnly"
        };
        var otherUser = new User
        {
            Id = otherUserId,
            UserName = "otheruser",
            NormalizedUserName = "OTHERUSER",
            Email = "other@example.com",
            NormalizedEmail = "OTHER@EXAMPLE.COM",
            PasswordHash = "AQAAAAIAAYagAAAAEDummyHashForTestingPurposesOnly"
        };
        context.Users.AddRange(user, otherUser);

        var customization = new UserCustomization
        {
            UserId = userId,
            HeadColor = "#FF0000",
            BodyColor = "#00FF00",
            TailColor = "#0000FF",
            EyeColor = "#FFFF00",
            WingColor = "#FF00FF",
            HornColor = "#00FFFF",
            MarkingsColor = "#FFFFFF"
        };
        context.UserCustomizations.Add(customization);

        var bodyItem = new Item
        {
            Name = "Other's Armor",
            Description = "Someone else's armor",
            Type = ItemTypes.EquippableOnBody,
            ThumbnailUrl = "assets/armor.png"
        };
        context.Items.Add(bodyItem);
        await context.SaveChangesAsync();

        var otherUserItem = new UserItem { UserId = otherUserId, ItemId = bodyItem.Id };
        context.UserItems.Add(otherUserItem);
        await context.SaveChangesAsync();

        var dto = new UpdateEquippedUserItemsDto
        {
            EquippedBodyUserItemId = otherUserItem.Id
        };

        // Act & Assert
        await Assert.ThrowsAsync<ForbidException>(
            () => service.UpdateEquippedUserItems(userId, dto, CancellationToken.None)
        );
    }

    [Fact]
    public async Task UpdateEquippedUserItems_ShouldThrowUnprocessableEntityException_WhenHeadItemIsNotHeadType()
    {
        // Arrange
        await using var context = _fixture.CreateDbContext();
        var service = new UserItemService(_logger, context);

        var userId = Guid.NewGuid();
        var user = new User
        {
            Id = userId,
            UserName = "testuser",
            NormalizedUserName = "TESTUSER",
            Email = "test@example.com",
            NormalizedEmail = "TEST@EXAMPLE.COM",
            PasswordHash = "AQAAAAIAAYagAAAAEDummyHashForTestingPurposesOnly"
        };
        context.Users.Add(user);

        var customization = new UserCustomization
        {
            UserId = userId,
            HeadColor = "#FF0000",
            BodyColor = "#00FF00",
            TailColor = "#0000FF",
            EyeColor = "#FFFF00",
            WingColor = "#FF00FF",
            HornColor = "#00FFFF",
            MarkingsColor = "#FFFFFF"
        };
        context.UserCustomizations.Add(customization);

        var wrongTypeItem = new Item
        {
            Name = "Sword",
            Description = "A sword, not a helmet",
            Type = ItemTypes.EquippableOnBody, // Wrong type!
            ThumbnailUrl = "assets/sword.png"
        };
        context.Items.Add(wrongTypeItem);
        await context.SaveChangesAsync();

        var userItem = new UserItem { UserId = userId, ItemId = wrongTypeItem.Id };
        context.UserItems.Add(userItem);
        await context.SaveChangesAsync();

        var dto = new UpdateEquippedUserItemsDto
        {
            EquippedHeadUserItemId = userItem.Id
        };

        // Act & Assert
        var exception = await Assert.ThrowsAsync<UnprocessableEntityException>(
            () => service.UpdateEquippedUserItems(userId, dto, CancellationToken.None)
        );

        Assert.Contains("EquippedHeadUserItemId", exception.Errors.Keys);
    }

    [Fact]
    public async Task UpdateEquippedUserItems_ShouldThrowUnprocessableEntityException_WhenBodyItemIsNotBodyType()
    {
        // Arrange
        await using var context = _fixture.CreateDbContext();
        var service = new UserItemService(_logger, context);

        var userId = Guid.NewGuid();
        var user = new User
        {
            Id = userId,
            UserName = "testuser",
            NormalizedUserName = "TESTUSER",
            Email = "test@example.com",
            NormalizedEmail = "TEST@EXAMPLE.COM",
            PasswordHash = "AQAAAAIAAYagAAAAEDummyHashForTestingPurposesOnly"
        };
        context.Users.Add(user);

        var customization = new UserCustomization
        {
            UserId = userId,
            HeadColor = "#FF0000",
            BodyColor = "#00FF00",
            TailColor = "#0000FF",
            EyeColor = "#FFFF00",
            WingColor = "#FF00FF",
            HornColor = "#00FFFF",
            MarkingsColor = "#FFFFFF"
        };
        context.UserCustomizations.Add(customization);

        var wrongTypeItem = new Item
        {
            Name = "Helmet",
            Description = "A helmet, not body armor",
            Type = ItemTypes.EquippableOnHead, // Wrong type!
            ThumbnailUrl = "assets/helmet.png"
        };
        context.Items.Add(wrongTypeItem);
        await context.SaveChangesAsync();

        var userItem = new UserItem { UserId = userId, ItemId = wrongTypeItem.Id };
        context.UserItems.Add(userItem);
        await context.SaveChangesAsync();

        var dto = new UpdateEquippedUserItemsDto
        {
            EquippedBodyUserItemId = userItem.Id
        };

        // Act & Assert
        var exception = await Assert.ThrowsAsync<UnprocessableEntityException>(
            () => service.UpdateEquippedUserItems(userId, dto, CancellationToken.None)
        );

        Assert.Contains("EquippedBodyUserItemId", exception.Errors.Keys);
    }

    [Fact]
    public async Task UpdateEquippedUserItems_ShouldThrowNotFoundException_WhenUserHasNoCustomization()
    {
        // Arrange
        await using var context = _fixture.CreateDbContext();
        var service = new UserItemService(_logger, context);

        var userId = Guid.NewGuid();
        var user = new User
        {
            Id = userId,
            UserName = "testuser",
            NormalizedUserName = "TESTUSER",
            Email = "test@example.com",
            NormalizedEmail = "TEST@EXAMPLE.COM",
            PasswordHash = "AQAAAAIAAYagAAAAEDummyHashForTestingPurposesOnly"
        };
        context.Users.Add(user);

        var headItem = new Item
        {
            Name = "Helmet",
            Description = "A helmet",
            Type = ItemTypes.EquippableOnHead,
            ThumbnailUrl = "assets/helmet.png"
        };
        context.Items.Add(headItem);
        await context.SaveChangesAsync();

        var userItem = new UserItem { UserId = userId, ItemId = headItem.Id };
        context.UserItems.Add(userItem);
        await context.SaveChangesAsync();

        var dto = new UpdateEquippedUserItemsDto
        {
            EquippedHeadUserItemId = userItem.Id
        };

        // Act & Assert
        await Assert.ThrowsAsync<NotFoundException>(
            () => service.UpdateEquippedUserItems(userId, dto, CancellationToken.None)
        );
    }

    #endregion
}

