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

        var seller = CreateUser("seller", "seller@example.com");
        var buyer = CreateUser("buyer", "buyer@example.com");
        context.Users.AddRange(seller, buyer);

        var item = new Item
        {
            Name = "Test Item",
            Description = "Test",
            Type = ItemTypes.Consumable,
            ThumbnailUrl = "test.png"
        };
        context.Items.Add(item);
        await context.SaveChangesAsync();

        var userItem1 = new UserItem { UserId = seller.Id, ItemId = item.Id };
        var userItem2 = new UserItem { UserId = seller.Id, ItemId = item.Id };
        context.UserItems.AddRange(userItem1, userItem2);
        await context.SaveChangesAsync();

        var activeOffer = new UserItemOffer
        {
            UserItemId = userItem1.Id,
            SellerId = seller.Id,
            Price = 100,
            PublishedAt = DateTime.UtcNow,
            BuyerId = null
        };
        var soldOffer = new UserItemOffer
        {
            UserItemId = userItem2.Id,
            SellerId = seller.Id,
            Price = 200,
            PublishedAt = DateTime.UtcNow.AddDays(-1),
            BuyerId = buyer.Id,
            BoughtAt = DateTime.UtcNow
        };
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

        var seller = CreateUser("seller", "seller@example.com");
        var buyer = CreateUser("buyer", "buyer@example.com");
        context.Users.AddRange(seller, buyer);

        var item = new Item
        {
            Name = "Test Item",
            Description = "Test",
            Type = ItemTypes.Consumable,
            ThumbnailUrl = "test.png"
        };
        context.Items.Add(item);
        await context.SaveChangesAsync();

        var userItem1 = new UserItem { UserId = seller.Id, ItemId = item.Id };
        var userItem2 = new UserItem { UserId = seller.Id, ItemId = item.Id };
        context.UserItems.AddRange(userItem1, userItem2);
        await context.SaveChangesAsync();

        var activeOffer = new UserItemOffer
        {
            UserItemId = userItem1.Id,
            SellerId = seller.Id,
            Price = 100,
            PublishedAt = DateTime.UtcNow,
            BuyerId = null
        };
        var soldOffer = new UserItemOffer
        {
            UserItemId = userItem2.Id,
            SellerId = seller.Id,
            Price = 200,
            PublishedAt = DateTime.UtcNow.AddDays(-1),
            BuyerId = buyer.Id,
            BoughtAt = DateTime.UtcNow
        };
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

        var seller = CreateUser("seller", "seller@example.com");
        context.Users.Add(seller);

        var item1 = new Item
        {
            Name = "Magic Sword",
            Description = "A magical sword",
            Type = ItemTypes.EquippableOnBody,
            ThumbnailUrl = "sword.png"
        };
        var item2 = new Item
        {
            Name = "Health Potion",
            Description = "Restores health",
            Type = ItemTypes.Consumable,
            ThumbnailUrl = "potion.png"
        };
        context.Items.AddRange(item1, item2);
        await context.SaveChangesAsync();

        var userItem1 = new UserItem { UserId = seller.Id, ItemId = item1.Id };
        var userItem2 = new UserItem { UserId = seller.Id, ItemId = item2.Id };
        context.UserItems.AddRange(userItem1, userItem2);
        await context.SaveChangesAsync();

        var offer1 = new UserItemOffer
        {
            UserItemId = userItem1.Id,
            SellerId = seller.Id,
            Price = 100,
            PublishedAt = DateTime.UtcNow
        };
        var offer2 = new UserItemOffer
        {
            UserItemId = userItem2.Id,
            SellerId = seller.Id,
            Price = 50,
            PublishedAt = DateTime.UtcNow
        };
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

        var seller = CreateUser("seller", "seller@example.com");
        context.Users.Add(seller);

        var item = new Item
        {
            Name = "Test Item",
            Description = "Test",
            Type = ItemTypes.Consumable,
            ThumbnailUrl = "test.png"
        };
        context.Items.Add(item);
        await context.SaveChangesAsync();

        var userItem1 = new UserItem { UserId = seller.Id, ItemId = item.Id };
        var userItem2 = new UserItem { UserId = seller.Id, ItemId = item.Id };
        context.UserItems.AddRange(userItem1, userItem2);
        await context.SaveChangesAsync();

        var expensiveOffer = new UserItemOffer
        {
            UserItemId = userItem1.Id,
            SellerId = seller.Id,
            Price = 200,
            PublishedAt = DateTime.UtcNow
        };
        var cheapOffer = new UserItemOffer
        {
            UserItemId = userItem2.Id,
            SellerId = seller.Id,
            Price = 50,
            PublishedAt = DateTime.UtcNow
        };
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

        var seller = CreateUser("seller", "seller@example.com");
        context.Users.Add(seller);

        var item = new Item
        {
            Name = "Test Item",
            Description = "Test",
            Type = ItemTypes.Consumable,
            ThumbnailUrl = "test.png"
        };
        context.Items.Add(item);
        await context.SaveChangesAsync();

        // Create 5 offers
        for (int i = 1; i <= 5; i++)
        {
            var userItem = new UserItem { UserId = seller.Id, ItemId = item.Id };
            context.UserItems.Add(userItem);
            await context.SaveChangesAsync();

            var offer = new UserItemOffer
            {
                UserItemId = userItem.Id,
                SellerId = seller.Id,
                Price = i * 10,
                PublishedAt = DateTime.UtcNow
            };
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

        var user = CreateUser("seller", "seller@example.com");
        context.Users.Add(user);

        var item = new Item
        {
            Name = "Test Item",
            Description = "Test",
            Type = ItemTypes.Consumable,
            ThumbnailUrl = "test.png"
        };
        context.Items.Add(item);
        await context.SaveChangesAsync();

        var userItem = new UserItem { UserId = user.Id, ItemId = item.Id };
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

        var user = CreateUser("seller", "seller@example.com");
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

        var owner = CreateUser("owner", "owner@example.com");
        var otherUser = CreateUser("other", "other@example.com");
        context.Users.AddRange(owner, otherUser);

        var item = new Item
        {
            Name = "Test Item",
            Description = "Test",
            Type = ItemTypes.Consumable,
            ThumbnailUrl = "test.png"
        };
        context.Items.Add(item);
        await context.SaveChangesAsync();

        var userItem = new UserItem { UserId = owner.Id, ItemId = item.Id };
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

        var user = CreateUser("seller", "seller@example.com");
        context.Users.Add(user);

        var item = new Item
        {
            Name = "Test Item",
            Description = "Test",
            Type = ItemTypes.Consumable,
            ThumbnailUrl = "test.png"
        };
        context.Items.Add(item);
        await context.SaveChangesAsync();

        var userItem = new UserItem { UserId = user.Id, ItemId = item.Id };
        context.UserItems.Add(userItem);
        await context.SaveChangesAsync();

        var existingOffer = new UserItemOffer
        {
            UserItemId = userItem.Id,
            SellerId = user.Id,
            Price = 100,
            PublishedAt = DateTime.UtcNow,
            BuyerId = null
        };
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

        var seller = CreateUser("seller", "seller@example.com");
        var buyer = CreateUser("buyer", "buyer@example.com");
        context.Users.AddRange(seller, buyer);

        var item = new Item
        {
            Name = "Test Item",
            Description = "Test",
            Type = ItemTypes.Consumable,
            ThumbnailUrl = "test.png"
        };
        context.Items.Add(item);
        await context.SaveChangesAsync();

        // Create item that was sold
        var userItem = new UserItem { UserId = seller.Id, ItemId = item.Id };
        context.UserItems.Add(userItem);
        await context.SaveChangesAsync();

        var soldOffer = new UserItemOffer
        {
            UserItemId = userItem.Id,
            SellerId = seller.Id,
            Price = 100,
            PublishedAt = DateTime.UtcNow.AddDays(-1),
            BuyerId = buyer.Id,
            BoughtAt = DateTime.UtcNow
        };
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

        var user = CreateUser("seller", "seller@example.com");
        context.Users.Add(user);

        var item = new Item
        {
            Name = "Test Item",
            Description = "Test",
            Type = ItemTypes.Consumable,
            ThumbnailUrl = "test.png"
        };
        context.Items.Add(item);
        await context.SaveChangesAsync();

        var userItem = new UserItem { UserId = user.Id, ItemId = item.Id };
        context.UserItems.Add(userItem);
        await context.SaveChangesAsync();

        var offer = new UserItemOffer
        {
            UserItemId = userItem.Id,
            SellerId = user.Id,
            Price = 100,
            PublishedAt = DateTime.UtcNow
        };
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

        var user = CreateUser("seller", "seller@example.com");
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

        var owner = CreateUser("owner", "owner@example.com");
        var otherUser = CreateUser("other", "other@example.com");
        context.Users.AddRange(owner, otherUser);

        var item = new Item
        {
            Name = "Test Item",
            Description = "Test",
            Type = ItemTypes.Consumable,
            ThumbnailUrl = "test.png"
        };
        context.Items.Add(item);
        await context.SaveChangesAsync();

        var userItem = new UserItem { UserId = owner.Id, ItemId = item.Id };
        context.UserItems.Add(userItem);
        await context.SaveChangesAsync();

        var offer = new UserItemOffer
        {
            UserItemId = userItem.Id,
            SellerId = owner.Id,
            Price = 100,
            PublishedAt = DateTime.UtcNow
        };
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

        var seller = CreateUser("seller", "seller@example.com");
        seller.Balance = 100;
        var buyer = CreateUser("buyer", "buyer@example.com");
        buyer.Balance = 500;
        context.Users.AddRange(seller, buyer);

        var item = new Item
        {
            Name = "Test Item",
            Description = "Test",
            Type = ItemTypes.Consumable,
            ThumbnailUrl = "test.png"
        };
        context.Items.Add(item);
        await context.SaveChangesAsync(); // Save users and items

        var userItem = new UserItem { UserId = seller.Id, ItemId = item.Id };
        context.UserItems.Add(userItem);
        await context.SaveChangesAsync();

        var offer = new UserItemOffer
        {
            UserItemId = userItem.Id,
            SellerId = seller.Id,
            Price = 150,
            PublishedAt = DateTime.UtcNow
        };
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

        var buyer = CreateUser("buyer", "buyer@example.com");
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

        var user = CreateUser("user", "user@example.com");
        user.Balance = 500;
        context.Users.Add(user);

        var item = new Item
        {
            Name = "Test Item",
            Description = "Test",
            Type = ItemTypes.Consumable,
            ThumbnailUrl = "test.png"
        };
        context.Items.Add(item);
        await context.SaveChangesAsync();

        var userItem = new UserItem { UserId = user.Id, ItemId = item.Id };
        context.UserItems.Add(userItem);
        await context.SaveChangesAsync();

        var offer = new UserItemOffer
        {
            UserItemId = userItem.Id,
            SellerId = user.Id,
            Price = 100,
            PublishedAt = DateTime.UtcNow
        };
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

        var seller = CreateUser("seller", "seller@example.com");
        context.Users.Add(seller);

        var item = new Item
        {
            Name = "Test Item",
            Description = "Test",
            Type = ItemTypes.Consumable,
            ThumbnailUrl = "test.png"
        };
        context.Items.Add(item);
        await context.SaveChangesAsync();

        var userItem = new UserItem { UserId = seller.Id, ItemId = item.Id };
        context.UserItems.Add(userItem);
        await context.SaveChangesAsync();

        var offer = new UserItemOffer
        {
            UserItemId = userItem.Id,
            SellerId = seller.Id,
            Price = 100,
            PublishedAt = DateTime.UtcNow
        };
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

        var seller = CreateUser("seller", "seller@example.com");
        seller.Balance = 100;
        var buyer = CreateUser("buyer", "buyer@example.com");
        buyer.Balance = 50; // Not enough
        context.Users.AddRange(seller, buyer);

        var item = new Item
        {
            Name = "Test Item",
            Description = "Test",
            Type = ItemTypes.Consumable,
            ThumbnailUrl = "test.png"
        };
        context.Items.Add(item);
        await context.SaveChangesAsync(); // Save users and items

        var userItem = new UserItem { UserId = seller.Id, ItemId = item.Id };
        context.UserItems.Add(userItem);
        await context.SaveChangesAsync();

        var offer = new UserItemOffer
        {
            UserItemId = userItem.Id,
            SellerId = seller.Id,
            Price = 100,
            PublishedAt = DateTime.UtcNow
        };
        context.UserItemOffers.Add(offer);
        await context.SaveChangesAsync();

        // Act & Assert
        var exception = await Assert.ThrowsAsync<UnprocessableEntityException>(
            () => service.PurchaseOffer(buyer.Id, offer.Id, CancellationToken.None)
        );

        Assert.Contains("Balance", exception.Errors.Keys);
    }

    #endregion

    #region Helper Methods

    private User CreateUser(string username, string email)
    {
        return new User
        {
            Id = Guid.NewGuid(),
            UserName = username,
            NormalizedUserName = username.ToUpper(),
            Email = email,
            NormalizedEmail = email.ToUpper(),
            PasswordHash = "AQAAAAIAAYagAAAAEDummyHashForTestingPurposesOnly"
        };
    }

    #endregion
}

