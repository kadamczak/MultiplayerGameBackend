using Microsoft.Extensions.Logging;
using MultiplayerGameBackend.Application.Common;
using MultiplayerGameBackend.Application.Interfaces;
using MultiplayerGameBackend.Application.Tests.TestHelpers;
using MultiplayerGameBackend.Application.UserItems;
using MultiplayerGameBackend.Application.UserItems.Requests;
using MultiplayerGameBackend.Domain.Constants;
using MultiplayerGameBackend.Domain.Exceptions;
using MultiplayerGameBackend.Tests.Shared.Helpers;
using NSubstitute;

namespace MultiplayerGameBackend.Application.Tests.UserItems;

public class UserItemServiceTests : IClassFixture<DatabaseFixture>, IAsyncLifetime
{
    private readonly DatabaseFixture _fixture;
    private readonly ILogger<UserItemService> _logger;
    private readonly ILocalizationService _localizationService;

    public UserItemServiceTests(DatabaseFixture fixture)
    {
        _fixture = fixture;
        _logger = Substitute.For<ILogger<UserItemService>>();
        _localizationService = Substitute.For<ILocalizationService>();
        
        // Setup localization service to return the key as the value (for testing)
        _localizationService.GetString(Arg.Any<string>()).Returns(ci => ci.ArgAt<string>(0));
        _localizationService.GetString(Arg.Any<string>(), Arg.Any<object[]>()).Returns(ci => ci.ArgAt<string>(0));
    }

    public Task InitializeAsync() => Task.CompletedTask;

    public async Task DisposeAsync() => await _fixture.CleanDatabase();

    #region GetCurrentUserItems Tests

    [Fact]
    public async Task GetCurrentUserItems_ShouldReturnUserItems_WhenUserHasItems()
    {
        // Arrange
        await using var context = _fixture.CreateDbContext();
        var service = new UserItemService(_logger, context, _localizationService);
        var userManager = IdentityHelper.CreateUserManager(context);

        var user = await DatabaseHelper.CreateAndSaveUser(userManager, "testuser", "test@example.com", "Password123!");
        var (item1, item2) = await DatabaseHelper.CreateAndSaveHeadAndBodyItems(context);
        await DatabaseHelper.CreateAndSaveUserItem(context, user.Id, item1.Id);
        await DatabaseHelper.CreateAndSaveUserItem(context, user.Id, item2.Id);

        var query = new PagedQuery { PageNumber = 1, PageSize = 10 };
        var dto = new GetUserItemsDto { PagedQuery = query };

        // Act
        var result = await service.GetUserItems(user.Id, dto, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.TotalItemsCount);
        Assert.Equal(2, result.Items.Count());
        Assert.Contains(result.Items, i => i.Item.Name == "Iron Helmet");
        Assert.Contains(result.Items, i => i.Item.Name == "Steel Sword");
    }

    [Fact]
    public async Task GetUserItems_ShouldReturnEmptyList_WhenUserHasNoItems()
    {
        // Arrange
        await using var context = _fixture.CreateDbContext();
        var service = new UserItemService(_logger, context, _localizationService);

        var userId = Guid.NewGuid();
        var query = new PagedQuery { PageNumber = 1, PageSize = 10 };
        var dto = new GetUserItemsDto { PagedQuery = query };

        // Act
        var result = await service.GetUserItems(userId, dto, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(0, result.TotalItemsCount);
        Assert.Empty(result.Items);
    }

    [Fact]
    public async Task GetUserItems_ShouldReturnOnlyUserItems_NotOtherUsersItems()
    {
        // Arrange
        await using var context = _fixture.CreateDbContext();
        var service = new UserItemService(_logger, context, _localizationService);
        var userManager = IdentityHelper.CreateUserManager(context);

        var user1 = await DatabaseHelper.CreateAndSaveUser(userManager, "user1", "user1@example.com", "Password123!");
        var user2 = await DatabaseHelper.CreateAndSaveUser(userManager, "user2", "user2@example.com", "Password123!");

        var item1 = await DatabaseHelper.CreateAndSaveItem(context, "User1 Item", ItemTypes.Consumable, "Item for user 1", "assets/item1.png");
        var item2 = await DatabaseHelper.CreateAndSaveItem(context, "User2 Item", ItemTypes.Consumable, "Item for user 2", "assets/item2.png");

        await DatabaseHelper.CreateAndSaveUserItem(context, user1.Id, item1.Id);
        await DatabaseHelper.CreateAndSaveUserItem(context, user2.Id, item2.Id);

        var query = new PagedQuery { PageNumber = 1, PageSize = 10 };
        var dto = new GetUserItemsDto { PagedQuery = query };

        // Act
        var result = await service.GetUserItems(user1.Id, dto, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Single(result.Items);
        Assert.Equal("User1 Item", result.Items.First().Item.Name);
    }

    [Fact]
    public async Task GetUserItems_ShouldFilterBySearchPhrase()
    {
        // Arrange
        await using var context = _fixture.CreateDbContext();
        var service = new UserItemService(_logger, context, _localizationService);
        var userManager = IdentityHelper.CreateUserManager(context);

        var user = await DatabaseHelper.CreateAndSaveUser(userManager, "testuser", "test@example.com", "Password123!");

        var item1 = await DatabaseHelper.CreateAndSaveItem(context, "Magic Sword", ItemTypes.EquippableOnBody, "A magical weapon", "assets/sword.png");
        var item2 = await DatabaseHelper.CreateAndSaveItem(context, "Health Potion", ItemTypes.Consumable, "Restores health", "assets/potion.png");

        await DatabaseHelper.CreateAndSaveUserItem(context, user.Id, item1.Id);
        await DatabaseHelper.CreateAndSaveUserItem(context, user.Id, item2.Id);

        var query = new PagedQuery { PageNumber = 1, PageSize = 10, SearchPhrase = "sword" };
        var dto = new GetUserItemsDto { PagedQuery = query };

        // Act
        var result = await service.GetUserItems(user.Id, dto, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Single(result.Items);
        Assert.Equal("Magic Sword", result.Items.First().Item.Name);
    }

    [Fact]
    public async Task GetUserItems_ShouldSortByNameAscending()
    {
        // Arrange
        await using var context = _fixture.CreateDbContext();
        var service = new UserItemService(_logger, context, _localizationService);
        var userManager = IdentityHelper.CreateUserManager(context);

        var user = await DatabaseHelper.CreateAndSaveUser(userManager, "testuser", "test@example.com", "Password123!");

        var itemZ = await DatabaseHelper.CreateAndSaveItem(context, "Zebra Shield", ItemTypes.EquippableOnBody, "Shield", "assets/shield.png");
        var itemA = await DatabaseHelper.CreateAndSaveItem(context, "Apple Potion", ItemTypes.Consumable, "Potion", "assets/potion.png");

        await DatabaseHelper.CreateAndSaveUserItem(context, user.Id, itemZ.Id);
        await DatabaseHelper.CreateAndSaveUserItem(context, user.Id, itemA.Id);

        var query = new PagedQuery
        {
            PageNumber = 1,
            PageSize = 10,
            SortBy = "Name",
            SortDirection = SortDirection.Ascending
        };
        var dto = new GetUserItemsDto { PagedQuery = query };

        // Act
        var result = await service.GetUserItems(user.Id, dto, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.Items.Count());
        Assert.Equal("Apple Potion", result.Items.First().Item.Name);
        Assert.Equal("Zebra Shield", result.Items.Last().Item.Name);
    }

    [Fact]
    public async Task GetUserItems_ShouldSortByNameDescending()
    {
        // Arrange
        await using var context = _fixture.CreateDbContext();
        var service = new UserItemService(_logger, context, _localizationService);
        var userManager = IdentityHelper.CreateUserManager(context);

        var user = await DatabaseHelper.CreateAndSaveUser(userManager, "testuser", "test@example.com", "Password123!");

        var itemZ = await DatabaseHelper.CreateAndSaveItem(context, "Zebra Shield", ItemTypes.EquippableOnBody, "Shield", "assets/shield.png");
        var itemA = await DatabaseHelper.CreateAndSaveItem(context, "Apple Potion", ItemTypes.Consumable, "Potion", "assets/potion.png");

        await DatabaseHelper.CreateAndSaveUserItem(context, user.Id, itemZ.Id);
        await DatabaseHelper.CreateAndSaveUserItem(context, user.Id, itemA.Id);

        var query = new PagedQuery
        {
            PageNumber = 1,
            PageSize = 10,
            SortBy = "Name",
            SortDirection = SortDirection.Descending
        };
        var dto = new GetUserItemsDto { PagedQuery = query };

        // Act
        var result = await service.GetUserItems(user.Id, dto, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.Items.Count());
        Assert.Equal("Zebra Shield", result.Items.First().Item.Name);
        Assert.Equal("Apple Potion", result.Items.Last().Item.Name);
    }

    [Fact]
    public async Task GetUserItems_ShouldReturnPaginatedResults()
    {
        // Arrange
        await using var context = _fixture.CreateDbContext();
        var service = new UserItemService(_logger, context, _localizationService);
        var userManager = IdentityHelper.CreateUserManager(context);

        var user = await DatabaseHelper.CreateAndSaveUser(userManager, "testuser", "test@example.com", "Password123!");

        // Create 5 items
        for (int i = 1; i <= 5; i++)
        {
            var item = await DatabaseHelper.CreateAndSaveItem(context, $"Item {i}", ItemTypes.Consumable, $"Description {i}", $"assets/item{i}.png");
            await DatabaseHelper.CreateAndSaveUserItem(context, user.Id, item.Id);
        }

        var query = new PagedQuery { PageNumber = 1, PageSize = 2 };
        var dto = new GetUserItemsDto { PagedQuery = query };

        // Act
        var result = await service.GetUserItems(user.Id, dto, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(5, result.TotalItemsCount);
        Assert.Equal(2, result.Items.Count());
        Assert.Equal(3, result.TotalPages);
    }

    [Fact]
    public async Task GetUserItems_ShouldIncludeActiveOfferInfo_WhenItemHasActiveOffer()
    {
        // Arrange
        await using var context = _fixture.CreateDbContext();
        var service = new UserItemService(_logger, context, _localizationService);
        var userManager = IdentityHelper.CreateUserManager(context);

        var user = await DatabaseHelper.CreateAndSaveUser(userManager, "testuser", "test@example.com", "Password123!");
        var item = await DatabaseHelper.CreateAndSaveItem(context, "Rare Sword", ItemTypes.EquippableOnBody, "A rare sword", "assets/sword.png");
        var userItem = await DatabaseHelper.CreateAndSaveUserItem(context, user.Id, item.Id);
        var offer = await DatabaseHelper.CreateAndSaveUserItemOffer(context, user.Id, userItem.Id, 100);

        var query = new PagedQuery { PageNumber = 1, PageSize = 10 };
        var dto = new GetUserItemsDto { PagedQuery = query };

        // Act
        var result = await service.GetUserItems(user.Id, dto, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Single(result.Items);
        var itemResult = result.Items.First();
        Assert.NotNull(itemResult.ActiveOfferId);
        Assert.Equal(offer.Id, itemResult.ActiveOfferId);
        Assert.Equal(100, itemResult.ActiveOfferPrice);
    }

    [Fact]
    public async Task GetUserItems_ShouldNotIncludeActiveOfferInfo_WhenOfferIsSold()
    {
        // Arrange
        await using var context = _fixture.CreateDbContext();
        var service = new UserItemService(_logger, context, _localizationService);
        var userManager = IdentityHelper.CreateUserManager(context);

        var user = await DatabaseHelper.CreateAndSaveUser(userManager, "testuser", "test@example.com", "Password123!");
        var buyer = await DatabaseHelper.CreateAndSaveUser(userManager, "buyer", "buyer@example.com", "Password123!");
        var item = await DatabaseHelper.CreateAndSaveItem(context, "Sold Sword", ItemTypes.EquippableOnBody, "A sold sword", "assets/sword.png");
        var userItem = await DatabaseHelper.CreateAndSaveUserItem(context, user.Id, item.Id);
        await DatabaseHelper.CreateAndSaveUserItemOffer(context, user.Id, userItem.Id, 100, buyer.Id, DateTime.UtcNow);

        var query = new PagedQuery { PageNumber = 1, PageSize = 10 };
        var dto = new GetUserItemsDto { PagedQuery = query };

        // Act
        var result = await service.GetUserItems(user.Id, dto, CancellationToken.None);

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
        var service = new UserItemService(_logger, context, _localizationService);
        var userManager = IdentityHelper.CreateUserManager(context);

        var user = await DatabaseHelper.CreateAndSaveUser(userManager, "testuser", "test@example.com", "Password123!");
        await DatabaseHelper.CreateAndSaveUserCustomization(context, user.Id);
        var headItem = await DatabaseHelper.CreateAndSaveItem(context, "Cool Helmet", ItemTypes.EquippableOnHead, "A cool helmet", "assets/helmet.png");
        var userHeadItem = await DatabaseHelper.CreateAndSaveUserItem(context, user.Id, headItem.Id);

        var dto = new UpdateEquippedUserItemsDto
        {
            EquippedHeadUserItemId = userHeadItem.Id
        };

        // Act
        await service.UpdateEquippedUserItems(user.Id, dto, CancellationToken.None);

        // Assert
        var updatedCustomization = context.UserCustomizations.First(c => c.UserId == user.Id);
        Assert.Equal(userHeadItem.Id, updatedCustomization.EquippedHeadUserItemId);
    }

    [Fact]
    public async Task UpdateEquippedUserItems_ShouldUpdateBodyItem_WhenValid()
    {
        // Arrange
        await using var context = _fixture.CreateDbContext();
        var service = new UserItemService(_logger, context, _localizationService);
        var userManager = IdentityHelper.CreateUserManager(context);

        var user = await DatabaseHelper.CreateAndSaveUser(userManager, "testuser", "test@example.com", "Password123!");
        await DatabaseHelper.CreateAndSaveUserCustomization(context, user.Id);
        var bodyItem = await DatabaseHelper.CreateAndSaveItem(context, "Epic Armor", ItemTypes.EquippableOnBody, "Epic armor", "assets/armor.png");
        var userBodyItem = await DatabaseHelper.CreateAndSaveUserItem(context, user.Id, bodyItem.Id);

        var dto = new UpdateEquippedUserItemsDto
        {
            EquippedBodyUserItemId = userBodyItem.Id
        };

        // Act
        await service.UpdateEquippedUserItems(user.Id, dto, CancellationToken.None);

        // Assert
        var updatedCustomization = context.UserCustomizations.First(c => c.UserId == user.Id);
        Assert.Equal(userBodyItem.Id, updatedCustomization.EquippedBodyUserItemId);
    }

    [Fact]
    public async Task UpdateEquippedUserItems_ShouldUpdateBothItems_WhenValid()
    {
        // Arrange
        await using var context = _fixture.CreateDbContext();
        var service = new UserItemService(_logger, context, _localizationService);
        var userManager = IdentityHelper.CreateUserManager(context);

        var user = await DatabaseHelper.CreateAndSaveUser(userManager, "testuser", "test@example.com", "Password123!");
        await DatabaseHelper.CreateAndSaveUserCustomization(context, user.Id);
        
        var headItem = await DatabaseHelper.CreateAndSaveItem(context, "Cool Helmet", ItemTypes.EquippableOnHead, "A cool helmet", "assets/helmet.png");
        var bodyItem = await DatabaseHelper.CreateAndSaveItem(context, "Epic Armor", ItemTypes.EquippableOnBody, "Epic armor", "assets/armor.png");
        
        var userHeadItem = await DatabaseHelper.CreateAndSaveUserItem(context, user.Id, headItem.Id);
        var userBodyItem = await DatabaseHelper.CreateAndSaveUserItem(context, user.Id, bodyItem.Id);

        var dto = new UpdateEquippedUserItemsDto
        {
            EquippedHeadUserItemId = userHeadItem.Id,
            EquippedBodyUserItemId = userBodyItem.Id
        };

        // Act
        await service.UpdateEquippedUserItems(user.Id, dto, CancellationToken.None);

        // Assert
        var updatedCustomization = context.UserCustomizations.First(c => c.UserId == user.Id);
        Assert.Equal(userHeadItem.Id, updatedCustomization.EquippedHeadUserItemId);
        Assert.Equal(userBodyItem.Id, updatedCustomization.EquippedBodyUserItemId);
    }

    [Fact]
    public async Task UpdateEquippedUserItems_ShouldUnequipItems_WhenPassedNull()
    {
        // Arrange
        await using var context = _fixture.CreateDbContext();
        var service = new UserItemService(_logger, context, _localizationService);
        var userManager = IdentityHelper.CreateUserManager(context);

        var user = await DatabaseHelper.CreateAndSaveUser(userManager, "testuser", "test@example.com", "Password123!");
        var headItem = await DatabaseHelper.CreateAndSaveItem(context, "Cool Helmet", ItemTypes.EquippableOnHead, "A cool helmet", "assets/helmet.png");
        var userHeadItem = await DatabaseHelper.CreateAndSaveUserItem(context, user.Id, headItem.Id);
        
        var customization = await DatabaseHelper.CreateAndSaveUserCustomization(context, user.Id);
        customization.EquippedHeadUserItemId = userHeadItem.Id;
        await context.SaveChangesAsync();

        var dto = new UpdateEquippedUserItemsDto
        {
            EquippedHeadUserItemId = null,
            EquippedBodyUserItemId = null
        };

        // Act
        await service.UpdateEquippedUserItems(user.Id, dto, CancellationToken.None);

        // Assert
        var updatedCustomization = context.UserCustomizations.First(c => c.UserId == user.Id);
        Assert.Null(updatedCustomization.EquippedHeadUserItemId);
        Assert.Null(updatedCustomization.EquippedBodyUserItemId);
    }

    [Fact]
    public async Task UpdateEquippedUserItems_ShouldThrowNotFoundException_WhenHeadItemDoesNotExist()
    {
        // Arrange
        await using var context = _fixture.CreateDbContext();
        var service = new UserItemService(_logger, context, _localizationService);
        var userManager = IdentityHelper.CreateUserManager(context);

        var user = await DatabaseHelper.CreateAndSaveUser(userManager, "testuser", "test@example.com", "Password123!");
        await DatabaseHelper.CreateAndSaveUserCustomization(context, user.Id);

        var dto = new UpdateEquippedUserItemsDto
        {
            EquippedHeadUserItemId = Guid.NewGuid() // Non-existent item
        };

        // Act & Assert
        await Assert.ThrowsAsync<NotFoundException>(
            () => service.UpdateEquippedUserItems(user.Id, dto, CancellationToken.None)
        );
    }

    [Fact]
    public async Task UpdateEquippedUserItems_ShouldThrowNotFoundException_WhenBodyItemDoesNotExist()
    {
        // Arrange
        await using var context = _fixture.CreateDbContext();
        var service = new UserItemService(_logger, context, _localizationService);
        var userManager = IdentityHelper.CreateUserManager(context);

        var user = await DatabaseHelper.CreateAndSaveUser(userManager, "testuser", "test@example.com", "Password123!");
        await DatabaseHelper.CreateAndSaveUserCustomization(context, user.Id);

        var dto = new UpdateEquippedUserItemsDto
        {
            EquippedBodyUserItemId = Guid.NewGuid() // Non-existent item
        };

        // Act & Assert
        await Assert.ThrowsAsync<NotFoundException>(
            () => service.UpdateEquippedUserItems(user.Id, dto, CancellationToken.None)
        );
    }

    [Fact]
    public async Task UpdateEquippedUserItems_ShouldThrowForbidException_WhenHeadItemDoesNotBelongToUser()
    {
        // Arrange
        await using var context = _fixture.CreateDbContext();
        var service = new UserItemService(_logger, context, _localizationService);
        var userManager = IdentityHelper.CreateUserManager(context);

        var user = await DatabaseHelper.CreateAndSaveUser(userManager, "testuser", "test@example.com", "Password123!");
        var otherUser = await DatabaseHelper.CreateAndSaveUser(userManager, "otheruser", "other@example.com", "Password123!");
        
        await DatabaseHelper.CreateAndSaveUserCustomization(context, user.Id);
        
        var headItem = await DatabaseHelper.CreateAndSaveItem(context, "Other's Helmet", ItemTypes.EquippableOnHead, "Someone else's helmet", "assets/helmet.png");
        var otherUserItem = await DatabaseHelper.CreateAndSaveUserItem(context, otherUser.Id, headItem.Id);

        var dto = new UpdateEquippedUserItemsDto
        {
            EquippedHeadUserItemId = otherUserItem.Id
        };

        // Act & Assert
        await Assert.ThrowsAsync<ForbidException>(
            () => service.UpdateEquippedUserItems(user.Id, dto, CancellationToken.None)
        );
    }

    [Fact]
    public async Task UpdateEquippedUserItems_ShouldThrowForbidException_WhenBodyItemDoesNotBelongToUser()
    {
        // Arrange
        await using var context = _fixture.CreateDbContext();
        var service = new UserItemService(_logger, context, _localizationService);
        var userManager = IdentityHelper.CreateUserManager(context);

        var user = await DatabaseHelper.CreateAndSaveUser(userManager, "testuser", "test@example.com", "Password123!");
        var otherUser = await DatabaseHelper.CreateAndSaveUser(userManager, "otheruser", "other@example.com", "Password123!");
        
        await DatabaseHelper.CreateAndSaveUserCustomization(context, user.Id);
        
        var bodyItem = await DatabaseHelper.CreateAndSaveItem(context, "Other's Armor", ItemTypes.EquippableOnBody, "Someone else's armor", "assets/armor.png");
        var otherUserItem = await DatabaseHelper.CreateAndSaveUserItem(context, otherUser.Id, bodyItem.Id);

        var dto = new UpdateEquippedUserItemsDto
        {
            EquippedBodyUserItemId = otherUserItem.Id
        };

        // Act & Assert
        await Assert.ThrowsAsync<ForbidException>(
            () => service.UpdateEquippedUserItems(user.Id, dto, CancellationToken.None)
        );
    }

    [Fact]
    public async Task UpdateEquippedUserItems_ShouldThrowUnprocessableEntityException_WhenHeadItemIsNotHeadType()
    {
        // Arrange
        await using var context = _fixture.CreateDbContext();
        var service = new UserItemService(_logger, context, _localizationService);
        var userManager = IdentityHelper.CreateUserManager(context);

        var user = await DatabaseHelper.CreateAndSaveUser(userManager, "testuser", "test@example.com", "Password123!");
        await DatabaseHelper.CreateAndSaveUserCustomization(context, user.Id);
        
        var wrongTypeItem = await DatabaseHelper.CreateAndSaveItem(context, "Sword", ItemTypes.EquippableOnBody, "A sword, not a helmet", "assets/sword.png");
        var userItem = await DatabaseHelper.CreateAndSaveUserItem(context, user.Id, wrongTypeItem.Id);

        var dto = new UpdateEquippedUserItemsDto
        {
            EquippedHeadUserItemId = userItem.Id
        };

        // Act & Assert
        var exception = await Assert.ThrowsAsync<UnprocessableEntityException>(
            () => service.UpdateEquippedUserItems(user.Id, dto, CancellationToken.None)
        );

        Assert.Contains("EquippedHeadUserItemId", exception.Errors.Keys);
    }

    [Fact]
    public async Task UpdateEquippedUserItems_ShouldThrowUnprocessableEntityException_WhenBodyItemIsNotBodyType()
    {
        // Arrange
        await using var context = _fixture.CreateDbContext();
        var service = new UserItemService(_logger, context, _localizationService);
        var userManager = IdentityHelper.CreateUserManager(context);

        var user = await DatabaseHelper.CreateAndSaveUser(userManager, "testuser", "test@example.com", "Password123!");
        await DatabaseHelper.CreateAndSaveUserCustomization(context, user.Id);
        
        var wrongTypeItem = await DatabaseHelper.CreateAndSaveItem(context, "Helmet", ItemTypes.EquippableOnHead, "A helmet, not body armor", "assets/helmet.png");
        var userItem = await DatabaseHelper.CreateAndSaveUserItem(context, user.Id, wrongTypeItem.Id);

        var dto = new UpdateEquippedUserItemsDto
        {
            EquippedBodyUserItemId = userItem.Id
        };

        // Act & Assert
        var exception = await Assert.ThrowsAsync<UnprocessableEntityException>(
            () => service.UpdateEquippedUserItems(user.Id, dto, CancellationToken.None)
        );

        Assert.Contains("EquippedBodyUserItemId", exception.Errors.Keys);
    }

    [Fact]
    public async Task UpdateEquippedUserItems_ShouldThrowNotFoundException_WhenUserHasNoCustomization()
    {
        // Arrange
        await using var context = _fixture.CreateDbContext();
        var service = new UserItemService(_logger, context, _localizationService);
        var userManager = IdentityHelper.CreateUserManager(context);

        var user = await DatabaseHelper.CreateAndSaveUser(userManager, "testuser", "test@example.com", "Password123!");
        var headItem = await DatabaseHelper.CreateAndSaveItem(context, "Helmet", ItemTypes.EquippableOnHead, "A helmet", "assets/helmet.png");
        var userItem = await DatabaseHelper.CreateAndSaveUserItem(context, user.Id, headItem.Id);

        var dto = new UpdateEquippedUserItemsDto
        {
            EquippedHeadUserItemId = userItem.Id
        };

        // Act & Assert
        await Assert.ThrowsAsync<NotFoundException>(
            () => service.UpdateEquippedUserItems(user.Id, dto, CancellationToken.None)
        );
    }

    #endregion
}

