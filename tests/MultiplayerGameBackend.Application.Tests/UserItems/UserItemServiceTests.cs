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
        var user = TestEntityFactory.CreateUser("testuser", "test@example.com", userId);
        context.Users.Add(user);

        var (item1, item2) = TestEntityFactory.CreateHeadAndBodyItems();
        context.Items.AddRange(item1, item2);
        await context.SaveChangesAsync();

        var userItem1 = TestEntityFactory.CreateUserItem(userId, item1.Id);
        var userItem2 = TestEntityFactory.CreateUserItem(userId, item2.Id);
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

        var user1 = TestEntityFactory.CreateUser("user1", "user1@example.com", userId1);
        var user2 = TestEntityFactory.CreateUser("user2", "user2@example.com", userId2);
        context.Users.AddRange(user1, user2);

        var item1 = TestEntityFactory.CreateItem("User1 Item", ItemTypes.Consumable, "Item for user 1", "assets/item1.png");
        var item2 = TestEntityFactory.CreateItem("User2 Item", ItemTypes.Consumable, "Item for user 2", "assets/item2.png");
        context.Items.AddRange(item1, item2);
        await context.SaveChangesAsync();

        var userItem1 = TestEntityFactory.CreateUserItem(userId1, item1.Id);
        var userItem2 = TestEntityFactory.CreateUserItem(userId2, item2.Id);
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
        var user = TestEntityFactory.CreateUser("testuser", "test@example.com", userId);
        context.Users.Add(user);

        var item1 = TestEntityFactory.CreateItem("Magic Sword", ItemTypes.EquippableOnBody, "A magical weapon", "assets/sword.png");
        var item2 = TestEntityFactory.CreateItem("Health Potion", ItemTypes.Consumable, "Restores health", "assets/potion.png");
        context.Items.AddRange(item1, item2);
        await context.SaveChangesAsync();

        var userItem1 = TestEntityFactory.CreateUserItem(userId, item1.Id);
        var userItem2 = TestEntityFactory.CreateUserItem(userId, item2.Id);
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
        var user = TestEntityFactory.CreateUser("testuser", "test@example.com", userId);
        context.Users.Add(user);

        var itemZ = TestEntityFactory.CreateItem("Zebra Shield", ItemTypes.EquippableOnBody, "Shield", "assets/shield.png");
        var itemA = TestEntityFactory.CreateItem("Apple Potion", ItemTypes.Consumable, "Potion", "assets/potion.png");
        context.Items.AddRange(itemZ, itemA);
        await context.SaveChangesAsync();

        var userItemZ = TestEntityFactory.CreateUserItem(userId, itemZ.Id);
        var userItemA = TestEntityFactory.CreateUserItem(userId, itemA.Id);
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
        var user = TestEntityFactory.CreateUser("testuser", "test@example.com", userId);
        context.Users.Add(user);

        var itemZ = TestEntityFactory.CreateItem("Zebra Shield", ItemTypes.EquippableOnBody, "Shield", "assets/shield.png");
        var itemA = TestEntityFactory.CreateItem("Apple Potion", ItemTypes.Consumable, "Potion", "assets/potion.png");
        context.Items.AddRange(itemZ, itemA);
        await context.SaveChangesAsync();

        var userItemZ = TestEntityFactory.CreateUserItem(userId, itemZ.Id);
        var userItemA = TestEntityFactory.CreateUserItem(userId, itemA.Id);
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
        var user = TestEntityFactory.CreateUser("testuser", "test@example.com", userId);
        context.Users.Add(user);

        // Create 5 items
        for (int i = 1; i <= 5; i++)
        {
            var item = TestEntityFactory.CreateItem($"Item {i}", ItemTypes.Consumable, $"Description {i}", $"assets/item{i}.png");
            context.Items.Add(item);
            await context.SaveChangesAsync();

            var userItem = TestEntityFactory.CreateUserItem(userId, item.Id);
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
        var user = TestEntityFactory.CreateUser("testuser", "test@example.com", userId);
        context.Users.Add(user);

        var item = TestEntityFactory.CreateItem("Rare Sword", ItemTypes.EquippableOnBody, "A rare sword", "assets/sword.png");
        context.Items.Add(item);
        await context.SaveChangesAsync();

        var userItem = TestEntityFactory.CreateUserItem(userId, item.Id);
        context.UserItems.Add(userItem);
        await context.SaveChangesAsync();

        var offer = TestEntityFactory.CreateUserItemOffer(userId, userItem.Id, 100);
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
        var user = TestEntityFactory.CreateUser("testuser", "test@example.com", userId);
        var buyer = TestEntityFactory.CreateUser("buyer", "buyer@example.com", buyerId);
        context.Users.AddRange(user, buyer);

        var item = TestEntityFactory.CreateItem("Sold Sword", ItemTypes.EquippableOnBody, "A sold sword", "assets/sword.png");
        context.Items.Add(item);
        await context.SaveChangesAsync();

        var userItem = TestEntityFactory.CreateUserItem(userId, item.Id);
        context.UserItems.Add(userItem);
        await context.SaveChangesAsync();

        var offer = TestEntityFactory.CreateUserItemOffer(userId, userItem.Id, 100, buyerId, DateTime.UtcNow);
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
        var user = TestEntityFactory.CreateUser("testuser", "test@example.com", userId);
        context.Users.Add(user);

        var customization = TestEntityFactory.CreateUserCustomization(userId);
        context.UserCustomizations.Add(customization);

        var headItem = TestEntityFactory.CreateItem("Cool Helmet", ItemTypes.EquippableOnHead, "A cool helmet", "assets/helmet.png");
        context.Items.Add(headItem);
        await context.SaveChangesAsync();

        var userHeadItem = TestEntityFactory.CreateUserItem(userId, headItem.Id);
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
        var user = TestEntityFactory.CreateUser("testuser", "test@example.com", userId);
        context.Users.Add(user);

        var customization = TestEntityFactory.CreateUserCustomization(userId);
        context.UserCustomizations.Add(customization);

        var bodyItem = TestEntityFactory.CreateItem("Epic Armor", ItemTypes.EquippableOnBody, "Epic armor", "assets/armor.png");
        context.Items.Add(bodyItem);
        await context.SaveChangesAsync();

        var userBodyItem = TestEntityFactory.CreateUserItem(userId, bodyItem.Id);
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
        var user = TestEntityFactory.CreateUser("testuser", "test@example.com", userId);
        context.Users.Add(user);

        var customization = TestEntityFactory.CreateUserCustomization(userId);
        context.UserCustomizations.Add(customization);

        var headItem = TestEntityFactory.CreateItem("Cool Helmet", ItemTypes.EquippableOnHead, "A cool helmet", "assets/helmet.png");
        var bodyItem = TestEntityFactory.CreateItem("Epic Armor", ItemTypes.EquippableOnBody, "Epic armor", "assets/armor.png");
        context.Items.AddRange(headItem, bodyItem);
        await context.SaveChangesAsync();

        var userHeadItem = TestEntityFactory.CreateUserItem(userId, headItem.Id);
        var userBodyItem = TestEntityFactory.CreateUserItem(userId, bodyItem.Id);
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
        var user = TestEntityFactory.CreateUser("testuser", "test@example.com", userId);
        context.Users.Add(user);

        var headItem = TestEntityFactory.CreateItem("Cool Helmet", ItemTypes.EquippableOnHead, "A cool helmet", "assets/helmet.png");
        context.Items.Add(headItem);
        await context.SaveChangesAsync();

        var userHeadItem = TestEntityFactory.CreateUserItem(userId, headItem.Id);
        context.UserItems.Add(userHeadItem);

        var customization = TestEntityFactory.CreateUserCustomization(userId);
        customization.EquippedHeadUserItemId = userHeadItem.Id;
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
        var user = TestEntityFactory.CreateUser("testuser", "test@example.com", userId);
        context.Users.Add(user);

        var customization = TestEntityFactory.CreateUserCustomization(userId);
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
        var user = TestEntityFactory.CreateUser("testuser", "test@example.com", userId);
        context.Users.Add(user);

        var customization = TestEntityFactory.CreateUserCustomization(userId);
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

        var user = TestEntityFactory.CreateUser("testuser", "test@example.com", userId);
        var otherUser = TestEntityFactory.CreateUser("otheruser", "other@example.com", otherUserId);
        context.Users.AddRange(user, otherUser);

        var customization = TestEntityFactory.CreateUserCustomization(userId);
        context.UserCustomizations.Add(customization);

        var headItem = TestEntityFactory.CreateItem("Other's Helmet", ItemTypes.EquippableOnHead, "Someone else's helmet", "assets/helmet.png");
        context.Items.Add(headItem);
        await context.SaveChangesAsync();

        var otherUserItem = TestEntityFactory.CreateUserItem(otherUserId, headItem.Id);
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

        var user = TestEntityFactory.CreateUser("testuser", "test@example.com", userId);
        var otherUser = TestEntityFactory.CreateUser("otheruser", "other@example.com", otherUserId);
        context.Users.AddRange(user, otherUser);

        var customization = TestEntityFactory.CreateUserCustomization(userId);
        context.UserCustomizations.Add(customization);

        var bodyItem = TestEntityFactory.CreateItem("Other's Armor", ItemTypes.EquippableOnBody, "Someone else's armor", "assets/armor.png");
        context.Items.Add(bodyItem);
        await context.SaveChangesAsync();

        var otherUserItem = TestEntityFactory.CreateUserItem(otherUserId, bodyItem.Id);
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
        var user = TestEntityFactory.CreateUser("testuser", "test@example.com", userId);
        context.Users.Add(user);

        var customization = TestEntityFactory.CreateUserCustomization(userId);
        context.UserCustomizations.Add(customization);

        var wrongTypeItem = TestEntityFactory.CreateItem("Sword", ItemTypes.EquippableOnBody, "A sword, not a helmet", "assets/sword.png");
        context.Items.Add(wrongTypeItem);
        await context.SaveChangesAsync();

        var userItem = TestEntityFactory.CreateUserItem(userId, wrongTypeItem.Id);
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
        var user = TestEntityFactory.CreateUser("testuser", "test@example.com", userId);
        context.Users.Add(user);

        var customization = TestEntityFactory.CreateUserCustomization(userId);
        context.UserCustomizations.Add(customization);

        var wrongTypeItem = TestEntityFactory.CreateItem("Helmet", ItemTypes.EquippableOnHead, "A helmet, not body armor", "assets/helmet.png");
        context.Items.Add(wrongTypeItem);
        await context.SaveChangesAsync();

        var userItem = TestEntityFactory.CreateUserItem(userId, wrongTypeItem.Id);
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
        var user = TestEntityFactory.CreateUser("testuser", "test@example.com", userId);
        context.Users.Add(user);

        var headItem = TestEntityFactory.CreateItem("Helmet", ItemTypes.EquippableOnHead, "A helmet", "assets/helmet.png");
        context.Items.Add(headItem);
        await context.SaveChangesAsync();

        var userItem = TestEntityFactory.CreateUserItem(userId, headItem.Id);
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

