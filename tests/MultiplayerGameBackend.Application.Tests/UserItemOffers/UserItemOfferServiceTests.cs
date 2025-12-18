using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using MultiplayerGameBackend.Application.Common;
using MultiplayerGameBackend.Application.Common.Mappings;
using MultiplayerGameBackend.Application.Tests.TestHelpers;
using MultiplayerGameBackend.Application.UserItemOffers;
using MultiplayerGameBackend.Application.UserItemOffers.Requests;
using MultiplayerGameBackend.Domain.Constants;
using MultiplayerGameBackend.Domain.Exceptions;
using MultiplayerGameBackend.Tests.Shared.Helpers;
using NSubstitute;

namespace MultiplayerGameBackend.Application.Tests.UserItemOffers;

public class UserItemOfferServiceTests : IClassFixture<DatabaseFixture>, IAsyncLifetime
{
    private readonly DatabaseFixture _fixture;
    private readonly ILogger<UserItemOfferService> _logger;
    private readonly UserItemOfferMapper _userItemOfferMapper;

    public UserItemOfferServiceTests(DatabaseFixture fixture)
    {
        _fixture = fixture;
        _logger = Substitute.For<ILogger<UserItemOfferService>>();
        var itemMapper = new ItemMapper();
        _userItemOfferMapper = new UserItemOfferMapper(itemMapper);
    }

    public Task InitializeAsync() => Task.CompletedTask;

    public async Task DisposeAsync() => await _fixture.CleanDatabase();

    #region GetOffers Tests

    [Fact]
    public async Task GetOffers_ShouldReturnActiveOffers_WhenShowActiveIsTrue()
    {
        // Arrange
        await using var context = _fixture.CreateDbContext();
        var service = new UserItemOfferService(_logger, context, _userItemOfferMapper);
        var userManager = IdentityHelper.CreateUserManager(context);

        var seller = await DatabaseHelper.CreateAndSaveUser(userManager, "seller", "seller@example.com", "Password123!");
        var buyer = await DatabaseHelper.CreateAndSaveUser(userManager, "buyer", "buyer@example.com", "Password123!");
        var item = await DatabaseHelper.CreateAndSaveItem(context, "Test Item", ItemTypes.Consumable, "Test", "test.png");
        
        var userItem1 = await DatabaseHelper.CreateAndSaveUserItem(context, seller.Id, item.Id);
        var userItem2 = await DatabaseHelper.CreateAndSaveUserItem(context, seller.Id, item.Id);

        var activeOffer = await DatabaseHelper.CreateAndSaveUserItemOffer(context, seller.Id, userItem1.Id, 100);
        var soldOffer = await DatabaseHelper.CreateAndSaveUserItemOffer(context, seller.Id, userItem2.Id, 200, buyer.Id, DateTime.UtcNow);
        soldOffer.PublishedAt = DateTime.UtcNow.AddDays(-1);
        await context.SaveChangesAsync();

        var query = new PagedQuery { PageNumber = 1, PageSize = 10 };
        var dto = new GetOffersDto() { PagedQuery = query, ShowActive = true };

        // Act
        var result = await service.GetOffers(dto, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Single(result.Items);
        Assert.Equal(activeOffer.Id, result.Items.First().Id);
        Assert.Null(result.Items.First().BuyerId);
    }

    [Fact]
    public async Task GetOffers_ShouldReturnSoldOffers_WhenShowActiveIsFalse()
    {
        // Arrange
        await using var context = _fixture.CreateDbContext();
        var service = new UserItemOfferService(_logger, context, _userItemOfferMapper);
        var userManager = IdentityHelper.CreateUserManager(context);

        var seller = await DatabaseHelper.CreateAndSaveUser(userManager, "seller", "seller@example.com", "Password123!");
        var buyer = await DatabaseHelper.CreateAndSaveUser(userManager, "buyer", "buyer@example.com", "Password123!");
        var item = await DatabaseHelper.CreateAndSaveItem(context, "Test Item", ItemTypes.Consumable, "Test", "test.png");

        var userItem1 = await DatabaseHelper.CreateAndSaveUserItem(context, seller.Id, item.Id);
        var userItem2 = await DatabaseHelper.CreateAndSaveUserItem(context, seller.Id, item.Id);

        var activeOffer = await DatabaseHelper.CreateAndSaveUserItemOffer(context, seller.Id, userItem1.Id, 100);
        var soldOffer = await DatabaseHelper.CreateAndSaveUserItemOffer(context, seller.Id, userItem2.Id, 200, buyer.Id, DateTime.UtcNow);
        soldOffer.PublishedAt = DateTime.UtcNow.AddDays(-1);
        await context.SaveChangesAsync();

        var query = new PagedQuery { PageNumber = 1, PageSize = 10 };
        var dto = new GetOffersDto() { PagedQuery = query, ShowActive = false };

        // Act
        var result = await service.GetOffers(dto, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Single(result.Items);
        Assert.Equal(soldOffer.Id, result.Items.First().Id);
        Assert.NotNull(result.Items.First().BuyerId);
        Assert.Equal(buyer.Id, result.Items.First().BuyerId);
    }

    [Fact]
    public async Task GetOffers_ShouldFilterBySearchPhrase()
    {
        // Arrange
        await using var context = _fixture.CreateDbContext();
        var service = new UserItemOfferService(_logger, context, _userItemOfferMapper);
        var userManager = IdentityHelper.CreateUserManager(context);

        var seller = await DatabaseHelper.CreateAndSaveUser(userManager, "seller", "seller@example.com", "Password123!");
        var item1 = await DatabaseHelper.CreateAndSaveItem(context, "Magic Sword", ItemTypes.EquippableOnBody, "A magical sword", "sword.png");
        var item2 = await DatabaseHelper.CreateAndSaveItem(context, "Health Potion", ItemTypes.Consumable, "Restores health", "potion.png");

        var userItem1 = await DatabaseHelper.CreateAndSaveUserItem(context, seller.Id, item1.Id);
        var userItem2 = await DatabaseHelper.CreateAndSaveUserItem(context, seller.Id, item2.Id);

        await DatabaseHelper.CreateAndSaveUserItemOffer(context, seller.Id, userItem1.Id, 100);
        await DatabaseHelper.CreateAndSaveUserItemOffer(context, seller.Id, userItem2.Id, 50);

        var query = new PagedQuery { PageNumber = 1, PageSize = 10, SearchPhrase = "sword" };
        var dto = new GetOffersDto() { PagedQuery = query, ShowActive = true };

        // Act
        var result = await service.GetOffers(dto, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Single(result.Items);
        Assert.Equal("Magic Sword", result.Items.First().UserItem.Item.Name);
    }

    [Fact]
    public async Task GetOffers_ShouldSortByPrice_Ascending()
    {
        // Arrange
        await using var context = _fixture.CreateDbContext();
        var service = new UserItemOfferService(_logger, context, _userItemOfferMapper);
        var userManager = IdentityHelper.CreateUserManager(context);

        var seller = await DatabaseHelper.CreateAndSaveUser(userManager, "seller", "seller@example.com", "Password123!");
        var item = await DatabaseHelper.CreateAndSaveItem(context, "Test Item", ItemTypes.Consumable, "Test", "test.png");

        var userItem1 = await DatabaseHelper.CreateAndSaveUserItem(context, seller.Id, item.Id);
        var userItem2 = await DatabaseHelper.CreateAndSaveUserItem(context, seller.Id, item.Id);

        await DatabaseHelper.CreateAndSaveUserItemOffer(context, seller.Id, userItem1.Id, 200);
        await DatabaseHelper.CreateAndSaveUserItemOffer(context, seller.Id, userItem2.Id, 50);

        var query = new PagedQuery
        {
            PageNumber = 1,
            PageSize = 10,
            SortBy = "Price",
            SortDirection = SortDirection.Ascending
        };
        var dto = new GetOffersDto() { PagedQuery = query, ShowActive = true };

        // Act
        var result = await service.GetOffers(dto, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.Items.Count());
        Assert.Equal(50, result.Items.First().Price);
        Assert.Equal(200, result.Items.Last().Price);
    }

    [Fact]
    public async Task GetOffers_ShouldReturnPaginatedResults()
    {
        // Arrange
        await using var context = _fixture.CreateDbContext();
        var service = new UserItemOfferService(_logger, context, _userItemOfferMapper);
        var userManager = IdentityHelper.CreateUserManager(context);

        var seller = await DatabaseHelper.CreateAndSaveUser(userManager, "seller", "seller@example.com", "Password123!");
        var item = await DatabaseHelper.CreateAndSaveItem(context, "Test Item", ItemTypes.Consumable, "Test", "test.png");

        // Create 5 offers
        for (int i = 1; i <= 5; i++)
        {
            var userItem = await DatabaseHelper.CreateAndSaveUserItem(context, seller.Id, item.Id);
            await DatabaseHelper.CreateAndSaveUserItemOffer(context, seller.Id, userItem.Id, i * 10);
        }

        var query = new PagedQuery { PageNumber = 1, PageSize = 2 };
        var dto = new GetOffersDto() { PagedQuery = query, ShowActive = true };

        // Act
        var result = await service.GetOffers(dto, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(5, result.TotalItemsCount);
        Assert.Equal(2, result.Items.Count());
        Assert.Equal(3, result.TotalPages);
    }

    #endregion

    #region CreateOffer Tests

    [Fact]
    public async Task CreateOffer_ShouldCreateOffer_WhenValid()
    {
        // Arrange
        await using var context = _fixture.CreateDbContext();
        var service = new UserItemOfferService(_logger, context, _userItemOfferMapper);
        var userManager = IdentityHelper.CreateUserManager(context);

        var user = await DatabaseHelper.CreateAndSaveUser(userManager, "seller", "seller@example.com", "Password123!");
        var item = await DatabaseHelper.CreateAndSaveItem(context, "Test Item", ItemTypes.Consumable, "Test", "test.png");
        var userItem = await DatabaseHelper.CreateAndSaveUserItem(context, user.Id, item.Id);

        var dto = new CreateUserItemOfferDto
        {
            UserItemId = userItem.Id,
            Price = 100
        };

        // Act
        await service.CreateOffer(user.Id, dto, CancellationToken.None);

        // Assert
        var offer = context.UserItemOffers.FirstOrDefault(o => o.UserItemId == userItem.Id);
        Assert.NotNull(offer);
        Assert.Equal(100, offer.Price);
        Assert.Equal(user.Id, offer.SellerId);
        Assert.Null(offer.BuyerId);
    }

    [Fact]
    public async Task CreateOffer_ShouldThrowNotFoundException_WhenUserItemDoesNotExist()
    {
        // Arrange
        await using var context = _fixture.CreateDbContext();
        var service = new UserItemOfferService(_logger, context, _userItemOfferMapper);
        var userManager = IdentityHelper.CreateUserManager(context);

        var user = await DatabaseHelper.CreateAndSaveUser(userManager, "seller", "seller@example.com", "Password123!");

        var dto = new CreateUserItemOfferDto
        {
            UserItemId = Guid.NewGuid(),
            Price = 100
        };

        // Act & Assert
        await Assert.ThrowsAsync<NotFoundException>(
            () => service.CreateOffer(user.Id, dto, CancellationToken.None)
        );
    }

    [Fact]
    public async Task CreateOffer_ShouldThrowForbidException_WhenUserDoesNotOwnItem()
    {
        // Arrange
        await using var context = _fixture.CreateDbContext();
        var service = new UserItemOfferService(_logger, context, _userItemOfferMapper);
        var userManager = IdentityHelper.CreateUserManager(context);

        var owner = await DatabaseHelper.CreateAndSaveUser(userManager, "owner", "owner@example.com", "Password123!");
        var otherUser = await DatabaseHelper.CreateAndSaveUser(userManager, "other", "other@example.com", "Password123!");
        var item = await DatabaseHelper.CreateAndSaveItem(context, "Test Item", ItemTypes.Consumable, "Test", "test.png");
        var userItem = await DatabaseHelper.CreateAndSaveUserItem(context, owner.Id, item.Id);

        var dto = new CreateUserItemOfferDto
        {
            UserItemId = userItem.Id,
            Price = 100
        };

        // Act & Assert
        await Assert.ThrowsAsync<ForbidException>(
            () => service.CreateOffer(otherUser.Id, dto, CancellationToken.None)
        );
    }

    [Fact]
    public async Task CreateOffer_ShouldThrowConflictException_WhenActiveOfferAlreadyExists()
    {
        // Arrange
        await using var context = _fixture.CreateDbContext();
        var service = new UserItemOfferService(_logger, context, _userItemOfferMapper);
        var userManager = IdentityHelper.CreateUserManager(context);

        var user = await DatabaseHelper.CreateAndSaveUser(userManager, "seller", "seller@example.com", "Password123!");
        var item = await DatabaseHelper.CreateAndSaveItem(context, "Test Item", ItemTypes.Consumable, "Test", "test.png");
        var userItem = await DatabaseHelper.CreateAndSaveUserItem(context, user.Id, item.Id);
        await DatabaseHelper.CreateAndSaveUserItemOffer(context, user.Id, userItem.Id, 100);

        var dto = new CreateUserItemOfferDto
        {
            UserItemId = userItem.Id,
            Price = 150
        };

        // Act & Assert
        await Assert.ThrowsAsync<ConflictException>(
            () => service.CreateOffer(user.Id, dto, CancellationToken.None)
        );
    }

    [Fact]
    public async Task CreateOffer_ShouldSucceed_WhenPreviousOfferWasSold()
    {
        // Arrange
        await using var context = _fixture.CreateDbContext();
        var service = new UserItemOfferService(_logger, context, _userItemOfferMapper);
        var userManager = IdentityHelper.CreateUserManager(context);

        var seller = await DatabaseHelper.CreateAndSaveUser(userManager, "seller", "seller@example.com", "Password123!");
        var buyer = await DatabaseHelper.CreateAndSaveUser(userManager, "buyer", "buyer@example.com", "Password123!");
        var item = await DatabaseHelper.CreateAndSaveItem(context, "Test Item", ItemTypes.Consumable, "Test", "test.png");

        // Create item that was sold
        var userItem = await DatabaseHelper.CreateAndSaveUserItem(context, seller.Id, item.Id);
        var soldOffer = await DatabaseHelper.CreateAndSaveUserItemOffer(context, seller.Id, userItem.Id, 100, buyer.Id, DateTime.UtcNow);
        soldOffer.PublishedAt = DateTime.UtcNow.AddDays(-1);
        await context.SaveChangesAsync();

        // Now seller has it back somehow and wants to sell again
        userItem.UserId = seller.Id;
        await context.SaveChangesAsync();

        var dto = new CreateUserItemOfferDto
        {
            UserItemId = userItem.Id,
            Price = 150
        };

        // Act
        await service.CreateOffer(seller.Id, dto, CancellationToken.None);

        // Assert
        var offers = context.UserItemOffers.Where(o => o.UserItemId == userItem.Id).ToList();
        Assert.Equal(2, offers.Count);
        Assert.Single(offers.Where(o => o.BuyerId == null));
    }

    #endregion

    #region DeleteOffer Tests

    [Fact]
    public async Task DeleteOffer_ShouldDeleteOffer_WhenValid()
    {
        // Arrange
        await using var context = _fixture.CreateDbContext();
        var service = new UserItemOfferService(_logger, context, _userItemOfferMapper);
        var userManager = IdentityHelper.CreateUserManager(context);

        var user = await DatabaseHelper.CreateAndSaveUser(userManager, "seller", "seller@example.com", "Password123!");
        var item = await DatabaseHelper.CreateAndSaveItem(context, "Test Item", ItemTypes.Consumable, "Test", "test.png");
        var userItem = await DatabaseHelper.CreateAndSaveUserItem(context, user.Id, item.Id);
        var offer = await DatabaseHelper.CreateAndSaveUserItemOffer(context, user.Id, userItem.Id, 100);

        // Act
        await service.DeleteOffer(user.Id, offer.Id, CancellationToken.None);

        // Assert
        var deletedOffer = context.UserItemOffers.FirstOrDefault(o => o.Id == offer.Id);
        Assert.Null(deletedOffer);
    }

    [Fact]
    public async Task DeleteOffer_ShouldThrowNotFoundException_WhenOfferDoesNotExist()
    {
        // Arrange
        await using var context = _fixture.CreateDbContext();
        var service = new UserItemOfferService(_logger, context, _userItemOfferMapper);
        var userManager = IdentityHelper.CreateUserManager(context);

        var user = await DatabaseHelper.CreateAndSaveUser(userManager, "seller", "seller@example.com", "Password123!");

        // Act & Assert
        await Assert.ThrowsAsync<NotFoundException>(
            () => service.DeleteOffer(user.Id, Guid.NewGuid(), CancellationToken.None)
        );
    }

    [Fact]
    public async Task DeleteOffer_ShouldThrowForbidException_WhenUserDoesNotOwnOffer()
    {
        // Arrange
        await using var context = _fixture.CreateDbContext();
        var service = new UserItemOfferService(_logger, context, _userItemOfferMapper);
        var userManager = IdentityHelper.CreateUserManager(context);

        var owner = await DatabaseHelper.CreateAndSaveUser(userManager, "owner", "owner@example.com", "Password123!");
        var otherUser = await DatabaseHelper.CreateAndSaveUser(userManager, "other", "other@example.com", "Password123!");
        var item = await DatabaseHelper.CreateAndSaveItem(context, "Test Item", ItemTypes.Consumable, "Test", "test.png");
        var userItem = await DatabaseHelper.CreateAndSaveUserItem(context, owner.Id, item.Id);
        var offer = await DatabaseHelper.CreateAndSaveUserItemOffer(context, owner.Id, userItem.Id, 100);

        // Act & Assert
        await Assert.ThrowsAsync<ForbidException>(
            () => service.DeleteOffer(otherUser.Id, offer.Id, CancellationToken.None)
        );
    }

    #endregion

    #region PurchaseOffer Tests

    [Fact]
    public async Task PurchaseOffer_ShouldCompletePurchase_WhenValid()
    {
        // Arrange
        await using var context = _fixture.CreateDbContext();
        var service = new UserItemOfferService(_logger, context, _userItemOfferMapper);
        var userManager = IdentityHelper.CreateUserManager(context);

        var seller = await DatabaseHelper.CreateAndSaveUser(userManager, "seller", "seller@example.com", "Password123!", balance: 100);
        var buyer = await DatabaseHelper.CreateAndSaveUser(userManager, "buyer", "buyer@example.com", "Password123!", balance: 500);
        var item = await DatabaseHelper.CreateAndSaveItem(context, "Test Item", ItemTypes.Consumable, "Test", "test.png");

        var userItem = await DatabaseHelper.CreateAndSaveUserItem(context, seller.Id, item.Id);
        var offer = await DatabaseHelper.CreateAndSaveUserItemOffer(context, seller.Id, userItem.Id, 150);

        // Act
        await service.PurchaseOffer(buyer.Id, offer.Id, CancellationToken.None);

        // Assert - Create a new context to avoid cached entities
        await using var verifyContext = _fixture.CreateDbContext();
        var updatedOffer = await verifyContext.UserItemOffers.FirstAsync(o => o.Id == offer.Id);
        Assert.Equal(buyer.Id, updatedOffer.BuyerId);
        Assert.NotNull(updatedOffer.BoughtAt);

        var updatedUserItem = await verifyContext.UserItems.FirstAsync(ui => ui.Id == userItem.Id);
        Assert.Equal(buyer.Id, updatedUserItem.UserId);

        var updatedSeller = await verifyContext.Users.FirstAsync(u => u.Id == seller.Id);
        Assert.Equal(250, updatedSeller.Balance); // 100 + 150

        var updatedBuyer = await verifyContext.Users.FirstAsync(u => u.Id == buyer.Id);
        Assert.Equal(350, updatedBuyer.Balance); // 500 - 150
    }

    [Fact]
    public async Task PurchaseOffer_ShouldThrowNotFoundException_WhenOfferDoesNotExist()
    {
        // Arrange
        await using var context = _fixture.CreateDbContext();
        var service = new UserItemOfferService(_logger, context, _userItemOfferMapper);
        var userManager = IdentityHelper.CreateUserManager(context);

        var buyer = await DatabaseHelper.CreateAndSaveUser(userManager, "buyer", "buyer@example.com", "Password123!", balance: 500);

        // Act & Assert
        await Assert.ThrowsAsync<NotFoundException>(
            () => service.PurchaseOffer(buyer.Id, Guid.NewGuid(), CancellationToken.None)
        );
    }

    [Fact]
    public async Task PurchaseOffer_ShouldThrowUnprocessableEntityException_WhenUserTriesToBuyOwnItem()
    {
        // Arrange
        await using var context = _fixture.CreateDbContext();
        var service = new UserItemOfferService(_logger, context, _userItemOfferMapper);
        var userManager = IdentityHelper.CreateUserManager(context);

        var user = await DatabaseHelper.CreateAndSaveUser(userManager, "user", "user@example.com", "Password123!", balance: 500);
        var item = await DatabaseHelper.CreateAndSaveItem(context, "Test Item", ItemTypes.Consumable, "Test", "test.png");
        var userItem = await DatabaseHelper.CreateAndSaveUserItem(context, user.Id, item.Id);
        var offer = await DatabaseHelper.CreateAndSaveUserItemOffer(context, user.Id, userItem.Id, 100);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<UnprocessableEntityException>(
            () => service.PurchaseOffer(user.Id, offer.Id, CancellationToken.None)
        );

        Assert.Contains("Offer", exception.Errors.Keys);
    }

    [Fact]
    public async Task PurchaseOffer_ShouldThrowNotFoundException_WhenBuyerDoesNotExist()
    {
        // Arrange
        await using var context = _fixture.CreateDbContext();
        var service = new UserItemOfferService(_logger, context, _userItemOfferMapper);
        var userManager = IdentityHelper.CreateUserManager(context);

        var seller = await DatabaseHelper.CreateAndSaveUser(userManager, "seller", "seller@example.com", "Password123!");
        var item = await DatabaseHelper.CreateAndSaveItem(context, "Test Item", ItemTypes.Consumable, "Test", "test.png");
        var userItem = await DatabaseHelper.CreateAndSaveUserItem(context, seller.Id, item.Id);
        var offer = await DatabaseHelper.CreateAndSaveUserItemOffer(context, seller.Id, userItem.Id, 100);

        // Act & Assert
        await Assert.ThrowsAsync<NotFoundException>(
            () => service.PurchaseOffer(Guid.NewGuid(), offer.Id, CancellationToken.None)
        );
    }

    [Fact]
    public async Task PurchaseOffer_ShouldThrowUnprocessableEntityException_WhenBuyerHasInsufficientBalance()
    {
        // Arrange
        await using var context = _fixture.CreateDbContext();
        var service = new UserItemOfferService(_logger, context, _userItemOfferMapper);
        var userManager = IdentityHelper.CreateUserManager(context);

        var seller = await DatabaseHelper.CreateAndSaveUser(userManager, "seller", "seller@example.com", "Password123!", balance: 100);
        var buyer = await DatabaseHelper.CreateAndSaveUser(userManager, "buyer", "buyer@example.com", "Password123!", balance: 50); // Not enough
        var item = await DatabaseHelper.CreateAndSaveItem(context, "Test Item", ItemTypes.Consumable, "Test", "test.png");

        var userItem = await DatabaseHelper.CreateAndSaveUserItem(context, seller.Id, item.Id);
        var offer = await DatabaseHelper.CreateAndSaveUserItemOffer(context, seller.Id, userItem.Id, 100);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<UnprocessableEntityException>(
            () => service.PurchaseOffer(buyer.Id, offer.Id, CancellationToken.None)
        );

        Assert.Contains("Balance", exception.Errors.Keys);
    }

    #endregion
}
