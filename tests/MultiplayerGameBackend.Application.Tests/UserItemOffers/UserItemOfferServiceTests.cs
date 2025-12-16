using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using MultiplayerGameBackend.Application.Common;
using MultiplayerGameBackend.Application.Tests.TestHelpers;
using MultiplayerGameBackend.Application.UserItemOffers;
using MultiplayerGameBackend.Application.UserItemOffers.Requests;
using MultiplayerGameBackend.Domain.Constants;
using MultiplayerGameBackend.Domain.Entities;
using MultiplayerGameBackend.Domain.Exceptions;
using NSubstitute;

namespace MultiplayerGameBackend.Application.Tests.UserItemOffers;

public class UserItemOfferServiceTests : IClassFixture<DatabaseFixture>, IAsyncLifetime
{
    private readonly DatabaseFixture _fixture;
    private readonly ILogger<UserItemOfferService> _logger;

    public UserItemOfferServiceTests(DatabaseFixture fixture)
    {
        _fixture = fixture;
        _logger = Substitute.For<ILogger<UserItemOfferService>>();
    }

    public Task InitializeAsync() => Task.CompletedTask;

    public async Task DisposeAsync() => await _fixture.CleanDatabase();

    #region GetOffers Tests

    [Fact]
    public async Task GetOffers_ShouldReturnActiveOffers_WhenShowActiveIsTrue()
    {
        // Arrange
        await using var context = _fixture.CreateDbContext();
        var service = new UserItemOfferService(_logger, context);

        var seller = TestEntityFactory.CreateUser("seller", "seller@example.com");
        var buyer = TestEntityFactory.CreateUser("buyer", "buyer@example.com");
        context.Users.AddRange(seller, buyer);

        var item = TestEntityFactory.CreateItem("Test Item", ItemTypes.Consumable, "Test", "test.png");
        context.Items.Add(item);
        await context.SaveChangesAsync();

        var userItem1 = TestEntityFactory.CreateUserItem(seller.Id, item.Id);
        var userItem2 = TestEntityFactory.CreateUserItem(seller.Id, item.Id);
        context.UserItems.AddRange(userItem1, userItem2);
        await context.SaveChangesAsync();

        var activeOffer = TestEntityFactory.CreateUserItemOffer(seller.Id, userItem1.Id, 100);
        var soldOffer = TestEntityFactory.CreateUserItemOffer(seller.Id, userItem2.Id, 200, buyer.Id, DateTime.UtcNow);
        soldOffer.PublishedAt = DateTime.UtcNow.AddDays(-1);
        context.UserItemOffers.AddRange(activeOffer, soldOffer);
        await context.SaveChangesAsync();

        var query = new PagedQuery { PageNumber = 1, PageSize = 10 };

        // Act
        var result = await service.GetOffers(query, showActive: true, CancellationToken.None);

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
        var service = new UserItemOfferService(_logger, context);

        var seller = TestEntityFactory.CreateUser("seller", "seller@example.com");
        var buyer = TestEntityFactory.CreateUser("buyer", "buyer@example.com");
        context.Users.AddRange(seller, buyer);

        var item = TestEntityFactory.CreateItem("Test Item", ItemTypes.Consumable, "Test", "test.png");
        context.Items.Add(item);
        await context.SaveChangesAsync();

        var userItem1 = TestEntityFactory.CreateUserItem(seller.Id, item.Id);
        var userItem2 = TestEntityFactory.CreateUserItem(seller.Id, item.Id);
        context.UserItems.AddRange(userItem1, userItem2);
        await context.SaveChangesAsync();

        var activeOffer = TestEntityFactory.CreateUserItemOffer(seller.Id, userItem1.Id, 100);
        var soldOffer = TestEntityFactory.CreateUserItemOffer(seller.Id, userItem2.Id, 200, buyer.Id, DateTime.UtcNow);
        soldOffer.PublishedAt = DateTime.UtcNow.AddDays(-1);
        context.UserItemOffers.AddRange(activeOffer, soldOffer);
        await context.SaveChangesAsync();

        var query = new PagedQuery { PageNumber = 1, PageSize = 10 };

        // Act
        var result = await service.GetOffers(query, showActive: false, CancellationToken.None);

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
        var service = new UserItemOfferService(_logger, context);

        var seller = TestEntityFactory.CreateUser("seller", "seller@example.com");
        context.Users.Add(seller);

        var item1 = TestEntityFactory.CreateItem("Magic Sword", ItemTypes.EquippableOnBody, "A magical sword", "sword.png");
        var item2 = TestEntityFactory.CreateItem("Health Potion", ItemTypes.Consumable, "Restores health", "potion.png");
        context.Items.AddRange(item1, item2);
        await context.SaveChangesAsync();

        var userItem1 = TestEntityFactory.CreateUserItem(seller.Id, item1.Id);
        var userItem2 = TestEntityFactory.CreateUserItem(seller.Id, item2.Id);
        context.UserItems.AddRange(userItem1, userItem2);
        await context.SaveChangesAsync();

        var offer1 = TestEntityFactory.CreateUserItemOffer(seller.Id, userItem1.Id, 100);
        var offer2 = TestEntityFactory.CreateUserItemOffer(seller.Id, userItem2.Id, 50);
        context.UserItemOffers.AddRange(offer1, offer2);
        await context.SaveChangesAsync();

        var query = new PagedQuery { PageNumber = 1, PageSize = 10, SearchPhrase = "sword" };

        // Act
        var result = await service.GetOffers(query, showActive: true, CancellationToken.None);

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
        var service = new UserItemOfferService(_logger, context);

        var seller = TestEntityFactory.CreateUser("seller", "seller@example.com");
        context.Users.Add(seller);

        var item = TestEntityFactory.CreateItem("Test Item", ItemTypes.Consumable, "Test", "test.png");
        context.Items.Add(item);
        await context.SaveChangesAsync();

        var userItem1 = TestEntityFactory.CreateUserItem(seller.Id, item.Id);
        var userItem2 = TestEntityFactory.CreateUserItem(seller.Id, item.Id);
        context.UserItems.AddRange(userItem1, userItem2);
        await context.SaveChangesAsync();

        var expensiveOffer = TestEntityFactory.CreateUserItemOffer(seller.Id, userItem1.Id, 200);
        var cheapOffer = TestEntityFactory.CreateUserItemOffer(seller.Id, userItem2.Id, 50);
        context.UserItemOffers.AddRange(expensiveOffer, cheapOffer);
        await context.SaveChangesAsync();

        var query = new PagedQuery
        {
            PageNumber = 1,
            PageSize = 10,
            SortBy = "Price",
            SortDirection = SortDirection.Ascending
        };

        // Act
        var result = await service.GetOffers(query, showActive: true, CancellationToken.None);

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
        var service = new UserItemOfferService(_logger, context);

        var seller = TestEntityFactory.CreateUser("seller", "seller@example.com");
        context.Users.Add(seller);

        var item = TestEntityFactory.CreateItem("Test Item", ItemTypes.Consumable, "Test", "test.png");
        context.Items.Add(item);
        await context.SaveChangesAsync();

        // Create 5 offers
        for (int i = 1; i <= 5; i++)
        {
            var userItem = TestEntityFactory.CreateUserItem(seller.Id, item.Id);
            context.UserItems.Add(userItem);
            await context.SaveChangesAsync();

            var offer = TestEntityFactory.CreateUserItemOffer(seller.Id, userItem.Id, i * 10);
            context.UserItemOffers.Add(offer);
        }
        await context.SaveChangesAsync();

        var query = new PagedQuery { PageNumber = 1, PageSize = 2 };

        // Act
        var result = await service.GetOffers(query, showActive: true, CancellationToken.None);

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
        var service = new UserItemOfferService(_logger, context);

        var user = TestEntityFactory.CreateUser("seller", "seller@example.com");
        context.Users.Add(user);

        var item = TestEntityFactory.CreateItem("Test Item", ItemTypes.Consumable, "Test", "test.png");
        context.Items.Add(item);
        await context.SaveChangesAsync();

        var userItem = TestEntityFactory.CreateUserItem(user.Id, item.Id);
        context.UserItems.Add(userItem);
        await context.SaveChangesAsync();

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
        var service = new UserItemOfferService(_logger, context);

        var user = TestEntityFactory.CreateUser("seller", "seller@example.com");
        context.Users.Add(user);
        await context.SaveChangesAsync();

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
        var service = new UserItemOfferService(_logger, context);

        var owner = TestEntityFactory.CreateUser("owner", "owner@example.com");
        var otherUser = TestEntityFactory.CreateUser("other", "other@example.com");
        context.Users.AddRange(owner, otherUser);

        var item = TestEntityFactory.CreateItem("Test Item", ItemTypes.Consumable, "Test", "test.png");
        context.Items.Add(item);
        await context.SaveChangesAsync();

        var userItem = TestEntityFactory.CreateUserItem(owner.Id, item.Id);
        context.UserItems.Add(userItem);
        await context.SaveChangesAsync();

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
        var service = new UserItemOfferService(_logger, context);

        var user = TestEntityFactory.CreateUser("seller", "seller@example.com");
        context.Users.Add(user);

        var item = TestEntityFactory.CreateItem("Test Item", ItemTypes.Consumable, "Test", "test.png");
        context.Items.Add(item);
        await context.SaveChangesAsync();

        var userItem = TestEntityFactory.CreateUserItem(user.Id, item.Id);
        context.UserItems.Add(userItem);
        await context.SaveChangesAsync();

        var existingOffer = TestEntityFactory.CreateUserItemOffer(user.Id, userItem.Id, 100);
        context.UserItemOffers.Add(existingOffer);
        await context.SaveChangesAsync();

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
        var service = new UserItemOfferService(_logger, context);

        var seller = TestEntityFactory.CreateUser("seller", "seller@example.com");
        var buyer = TestEntityFactory.CreateUser("buyer", "buyer@example.com");
        context.Users.AddRange(seller, buyer);

        var item = TestEntityFactory.CreateItem("Test Item", ItemTypes.Consumable, "Test", "test.png");
        context.Items.Add(item);
        await context.SaveChangesAsync();

        // Create item that was sold
        var userItem = TestEntityFactory.CreateUserItem(seller.Id, item.Id);
        context.UserItems.Add(userItem);
        await context.SaveChangesAsync();

        var soldOffer = TestEntityFactory.CreateUserItemOffer(seller.Id, userItem.Id, 100, buyer.Id, DateTime.UtcNow);
        soldOffer.PublishedAt = DateTime.UtcNow.AddDays(-1);
        context.UserItemOffers.Add(soldOffer);
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
        var service = new UserItemOfferService(_logger, context);

        var user = TestEntityFactory.CreateUser("seller", "seller@example.com");
        context.Users.Add(user);

        var item = TestEntityFactory.CreateItem("Test Item", ItemTypes.Consumable, "Test", "test.png");
        context.Items.Add(item);
        await context.SaveChangesAsync();

        var userItem = TestEntityFactory.CreateUserItem(user.Id, item.Id);
        context.UserItems.Add(userItem);
        await context.SaveChangesAsync();

        var offer = TestEntityFactory.CreateUserItemOffer(user.Id, userItem.Id, 100);
        context.UserItemOffers.Add(offer);
        await context.SaveChangesAsync();

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
        var service = new UserItemOfferService(_logger, context);

        var user = TestEntityFactory.CreateUser("seller", "seller@example.com");
        context.Users.Add(user);
        await context.SaveChangesAsync();

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
        var service = new UserItemOfferService(_logger, context);

        var owner = TestEntityFactory.CreateUser("owner", "owner@example.com");
        var otherUser = TestEntityFactory.CreateUser("other", "other@example.com");
        context.Users.AddRange(owner, otherUser);

        var item = TestEntityFactory.CreateItem("Test Item", ItemTypes.Consumable, "Test", "test.png");
        context.Items.Add(item);
        await context.SaveChangesAsync();

        var userItem = TestEntityFactory.CreateUserItem(owner.Id, item.Id);
        context.UserItems.Add(userItem);
        await context.SaveChangesAsync();

        var offer = TestEntityFactory.CreateUserItemOffer(owner.Id, userItem.Id, 100);
        context.UserItemOffers.Add(offer);
        await context.SaveChangesAsync();

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
        var service = new UserItemOfferService(_logger, context);

        var seller = TestEntityFactory.CreateUser("seller", "seller@example.com", balance: 100);
        var buyer = TestEntityFactory.CreateUser("buyer", "buyer@example.com", balance: 500);
        context.Users.AddRange(seller, buyer);

        var item = TestEntityFactory.CreateItem("Test Item", ItemTypes.Consumable, "Test", "test.png");
        context.Items.Add(item);
        await context.SaveChangesAsync(); // Save users and items

        var userItem = TestEntityFactory.CreateUserItem(seller.Id, item.Id);
        context.UserItems.Add(userItem);
        await context.SaveChangesAsync();

        var offer = TestEntityFactory.CreateUserItemOffer(seller.Id, userItem.Id, 150);
        context.UserItemOffers.Add(offer);
        await context.SaveChangesAsync();

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
        var service = new UserItemOfferService(_logger, context);

        var buyer = TestEntityFactory.CreateUser("buyer", "buyer@example.com");
        buyer.Balance = 500;
        context.Users.Add(buyer);
        await context.SaveChangesAsync();

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
        var service = new UserItemOfferService(_logger, context);

        var user = TestEntityFactory.CreateUser("user", "user@example.com", balance: 500);
        context.Users.Add(user);

        var item = TestEntityFactory.CreateItem("Test Item", ItemTypes.Consumable, "Test", "test.png");
        context.Items.Add(item);
        await context.SaveChangesAsync();

        var userItem = TestEntityFactory.CreateUserItem(user.Id, item.Id);
        context.UserItems.Add(userItem);
        await context.SaveChangesAsync();

        var offer = TestEntityFactory.CreateUserItemOffer(user.Id, userItem.Id, 100);
        context.UserItemOffers.Add(offer);
        await context.SaveChangesAsync();

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
        var service = new UserItemOfferService(_logger, context);

        var seller = TestEntityFactory.CreateUser("seller", "seller@example.com");
        context.Users.Add(seller);

        var item = TestEntityFactory.CreateItem("Test Item", ItemTypes.Consumable, "Test", "test.png");
        context.Items.Add(item);
        await context.SaveChangesAsync();

        var userItem = TestEntityFactory.CreateUserItem(seller.Id, item.Id);
        context.UserItems.Add(userItem);
        await context.SaveChangesAsync();

        var offer = TestEntityFactory.CreateUserItemOffer(seller.Id, userItem.Id, 100);
        context.UserItemOffers.Add(offer);
        await context.SaveChangesAsync();

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
        var service = new UserItemOfferService(_logger, context);

        var seller = TestEntityFactory.CreateUser("seller", "seller@example.com", balance: 100);
        var buyer = TestEntityFactory.CreateUser("buyer", "buyer@example.com", balance: 50); // Not enough
        context.Users.AddRange(seller, buyer);

        var item = TestEntityFactory.CreateItem("Test Item", ItemTypes.Consumable, "Test", "test.png");
        context.Items.Add(item);
        await context.SaveChangesAsync(); // Save users and items

        var userItem = TestEntityFactory.CreateUserItem(seller.Id, item.Id);
        context.UserItems.Add(userItem);
        await context.SaveChangesAsync();

        var offer = TestEntityFactory.CreateUserItemOffer(seller.Id, userItem.Id, 100);
        context.UserItemOffers.Add(offer);
        await context.SaveChangesAsync();

        // Act & Assert
        var exception = await Assert.ThrowsAsync<UnprocessableEntityException>(
            () => service.PurchaseOffer(buyer.Id, offer.Id, CancellationToken.None)
        );

        Assert.Contains("Balance", exception.Errors.Keys);
    }


    #endregion
}

