using Microsoft.Extensions.Logging;
using MultiplayerGameBackend.Application.MerchantItemOffers;
using MultiplayerGameBackend.Application.Tests.Common;
using MultiplayerGameBackend.Domain.Constants;
using MultiplayerGameBackend.Domain.Entities;
using MultiplayerGameBackend.Domain.Exceptions;
using NSubstitute;

namespace MultiplayerGameBackend.Application.Tests.MerchantItemOffers;

public class MerchantItemOfferServiceTests : IClassFixture<DatabaseFixture>, IAsyncLifetime
{
    private readonly DatabaseFixture _fixture;
    private readonly ILogger<MerchantItemOfferService> _logger;

    public MerchantItemOfferServiceTests(DatabaseFixture fixture)
    {
        _fixture = fixture;
        _logger = Substitute.For<ILogger<MerchantItemOfferService>>();
    }

    public Task InitializeAsync() => Task.CompletedTask;

    public async Task DisposeAsync() => await _fixture.CleanDatabase();

    #region GetOffers Tests

    [Fact]
    public async Task GetOffers_ShouldReturnAllOffersForMerchant_WhenOffersExist()
    {
        // Arrange
        await using var context = _fixture.CreateDbContext();
        var service = new MerchantItemOfferService(_logger, context);

        var merchant = new InGameMerchant { Id = 1 };
        context.InGameMerchants.Add(merchant);

        var item1 = new Item
        {
            Name = "Health Potion",
            Description = "Restores 50 HP",
            Type = ItemTypes.Consumable,
            ThumbnailUrl = "assets/health_potion.png"
        };
        var item2 = new Item
        {
            Name = "Iron Sword",
            Description = "A sturdy iron sword",
            Type = ItemTypes.EquippableOnBody,
            ThumbnailUrl = "assets/iron_sword.png"
        };
        context.Items.AddRange(item1, item2);
        await context.SaveChangesAsync();

        var offer1 = new MerchantItemOffer
        {
            MerchantId = merchant.Id,
            ItemId = item1.Id,
            Price = 50
        };
        var offer2 = new MerchantItemOffer
        {
            MerchantId = merchant.Id,
            ItemId = item2.Id,
            Price = 150
        };
        context.MerchantItemOffers.AddRange(offer1, offer2);
        await context.SaveChangesAsync();

        // Act
        var result = await service.GetOffers(merchant.Id, CancellationToken.None);

        // Assert
        var offers = result.ToList();
        Assert.Equal(2, offers.Count);
        Assert.Contains(offers, o => o.Item.Name == "Health Potion" && o.Price == 50);
        Assert.Contains(offers, o => o.Item.Name == "Iron Sword" && o.Price == 150);
    }

    [Fact]
    public async Task GetOffers_ShouldReturnEmptyList_WhenMerchantHasNoOffers()
    {
        // Arrange
        await using var context = _fixture.CreateDbContext();
        var service = new MerchantItemOfferService(_logger, context);

        var merchant = new InGameMerchant { Id = 1 };
        context.InGameMerchants.Add(merchant);
        await context.SaveChangesAsync();

        // Act
        var result = await service.GetOffers(merchant.Id, CancellationToken.None);

        // Assert
        var offers = result.ToList();
        Assert.Empty(offers);
    }

    [Fact]
    public async Task GetOffers_ShouldThrowNotFoundException_WhenMerchantDoesNotExist()
    {
        // Arrange
        await using var context = _fixture.CreateDbContext();
        var service = new MerchantItemOfferService(_logger, context);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<NotFoundException>(
            () => service.GetOffers(999, CancellationToken.None)
        );

        Assert.NotNull(exception);
    }

    [Fact]
    public async Task GetOffers_ShouldOnlyReturnOffersForSpecificMerchant()
    {
        // Arrange
        await using var context = _fixture.CreateDbContext();
        var service = new MerchantItemOfferService(_logger, context);

        var merchant1 = new InGameMerchant { Id = 1 };
        var merchant2 = new InGameMerchant { Id = 2 };
        context.InGameMerchants.AddRange(merchant1, merchant2);

        var item1 = new Item
        {
            Name = "Merchant 1 Item",
            Description = "Item for merchant 1",
            Type = ItemTypes.Consumable,
            ThumbnailUrl = "assets/item1.png"
        };
        var item2 = new Item
        {
            Name = "Merchant 2 Item",
            Description = "Item for merchant 2",
            Type = ItemTypes.Consumable,
            ThumbnailUrl = "assets/item2.png"
        };
        context.Items.AddRange(item1, item2);
        await context.SaveChangesAsync();

        var offer1 = new MerchantItemOffer
        {
            MerchantId = merchant1.Id,
            ItemId = item1.Id,
            Price = 100
        };
        var offer2 = new MerchantItemOffer
        {
            MerchantId = merchant2.Id,
            ItemId = item2.Id,
            Price = 200
        };
        context.MerchantItemOffers.AddRange(offer1, offer2);
        await context.SaveChangesAsync();

        // Act
        var result = await service.GetOffers(merchant1.Id, CancellationToken.None);

        // Assert
        var offers = result.ToList();
        Assert.Single(offers);
        Assert.Equal("Merchant 1 Item", offers[0].Item.Name);
        Assert.Equal(100, offers[0].Price);
    }

    #endregion

    #region PurchaseOffer Tests

    [Fact]
    public async Task PurchaseOffer_ShouldSuccessfullyPurchaseOffer_WhenUserHasSufficientBalance()
    {
        // Arrange
        await using var context = _fixture.CreateDbContext();
        var service = new MerchantItemOfferService(_logger, context);

        var userId = Guid.NewGuid();
        var user = new User
        {
            Id = userId,
            UserName = "testuser",
            NormalizedUserName = "TESTUSER",
            Email = "test@example.com",
            NormalizedEmail = "TEST@EXAMPLE.COM",
            PasswordHash = "AQAAAAIAAYagAAAAEDummyHashForTestingPurposesOnly",
            Balance = 500
        };
        context.Users.Add(user);

        var merchant = new InGameMerchant { Id = 1 };
        context.InGameMerchants.Add(merchant);

        var item = new Item
        {
            Name = "Magic Scroll",
            Description = "A powerful magic scroll",
            Type = ItemTypes.Consumable,
            ThumbnailUrl = "assets/scroll.png"
        };
        context.Items.Add(item);
        await context.SaveChangesAsync();

        var offer = new MerchantItemOffer
        {
            MerchantId = merchant.Id,
            ItemId = item.Id,
            Price = 200
        };
        context.MerchantItemOffers.Add(offer);
        await context.SaveChangesAsync();

        // Act
        await service.PurchaseOffer(userId, offer.Id, CancellationToken.None);

        // Assert
        var updatedUser = await context.Users.FindAsync(userId);
        Assert.NotNull(updatedUser);
        Assert.Equal(300, updatedUser.Balance); // 500 - 200

        var userItems = context.UserItems.Where(ui => ui.UserId == userId).ToList();
        Assert.Single(userItems);
        Assert.Equal(item.Id, userItems[0].ItemId);
    }

    [Fact]
    public async Task PurchaseOffer_ShouldThrowNotFoundException_WhenOfferDoesNotExist()
    {
        // Arrange
        await using var context = _fixture.CreateDbContext();
        var service = new MerchantItemOfferService(_logger, context);

        var userId = Guid.NewGuid();

        // Act & Assert
        var exception = await Assert.ThrowsAsync<NotFoundException>(
            () => service.PurchaseOffer(userId, 999, CancellationToken.None)
        );

        Assert.NotNull(exception);
    }

    [Fact]
    public async Task PurchaseOffer_ShouldThrowNotFoundException_WhenUserDoesNotExist()
    {
        // Arrange
        await using var context = _fixture.CreateDbContext();
        var service = new MerchantItemOfferService(_logger, context);

        var merchant = new InGameMerchant { Id = 1 };
        context.InGameMerchants.Add(merchant);

        var item = new Item
        {
            Name = "Test Item",
            Description = "Test",
            Type = ItemTypes.Consumable,
            ThumbnailUrl = "assets/test.png"
        };
        context.Items.Add(item);
        await context.SaveChangesAsync();

        var offer = new MerchantItemOffer
        {
            MerchantId = merchant.Id,
            ItemId = item.Id,
            Price = 100
        };
        context.MerchantItemOffers.Add(offer);
        await context.SaveChangesAsync();

        var nonExistentUserId = Guid.NewGuid();

        // Act & Assert
        var exception = await Assert.ThrowsAsync<NotFoundException>(
            () => service.PurchaseOffer(nonExistentUserId, offer.Id, CancellationToken.None)
        );

        Assert.NotNull(exception);
    }

    [Fact]
    public async Task PurchaseOffer_ShouldThrowUnprocessableEntityException_WhenUserHasInsufficientBalance()
    {
        // Arrange
        await using var context = _fixture.CreateDbContext();
        var service = new MerchantItemOfferService(_logger, context);

        var userId = Guid.NewGuid();
        var user = new User
        {
            Id = userId,
            UserName = "pooruser",
            NormalizedUserName = "POORUSER",
            Email = "poor@example.com",
            NormalizedEmail = "POOR@EXAMPLE.COM",
            PasswordHash = "AQAAAAIAAYagAAAAEDummyHashForTestingPurposesOnly",
            Balance = 50
        };
        context.Users.Add(user);

        var merchant = new InGameMerchant { Id = 1 };
        context.InGameMerchants.Add(merchant);

        var item = new Item
        {
            Name = "Expensive Item",
            Description = "Very expensive",
            Type = ItemTypes.EquippableOnHead,
            ThumbnailUrl = "assets/expensive.png"
        };
        context.Items.Add(item);
        await context.SaveChangesAsync();

        var offer = new MerchantItemOffer
        {
            MerchantId = merchant.Id,
            ItemId = item.Id,
            Price = 200
        };
        context.MerchantItemOffers.Add(offer);
        await context.SaveChangesAsync();

        // Act & Assert
        var exception = await Assert.ThrowsAsync<UnprocessableEntityException>(
            () => service.PurchaseOffer(userId, offer.Id, CancellationToken.None)
        );

        Assert.NotNull(exception.Errors);
        Assert.True(exception.Errors.ContainsKey("Balance"));
    }

    [Fact]
    public async Task PurchaseOffer_ShouldAllowPurchaseWithExactBalance()
    {
        // Arrange
        await using var context = _fixture.CreateDbContext();
        var service = new MerchantItemOfferService(_logger, context);

        var userId = Guid.NewGuid();
        var user = new User
        {
            Id = userId,
            UserName = "exactuser",
            NormalizedUserName = "EXACTUSER",
            Email = "exact@example.com",
            NormalizedEmail = "EXACT@EXAMPLE.COM",
            PasswordHash = "AQAAAAIAAYagAAAAEDummyHashForTestingPurposesOnly",
            Balance = 100
        };
        context.Users.Add(user);

        var merchant = new InGameMerchant { Id = 1 };
        context.InGameMerchants.Add(merchant);

        var item = new Item
        {
            Name = "Exact Price Item",
            Description = "Costs exactly 100",
            Type = ItemTypes.Consumable,
            ThumbnailUrl = "assets/exact.png"
        };
        context.Items.Add(item);
        await context.SaveChangesAsync();

        var offer = new MerchantItemOffer
        {
            MerchantId = merchant.Id,
            ItemId = item.Id,
            Price = 100
        };
        context.MerchantItemOffers.Add(offer);
        await context.SaveChangesAsync();

        // Act
        await service.PurchaseOffer(userId, offer.Id, CancellationToken.None);

        // Assert
        var updatedUser = await context.Users.FindAsync(userId);
        Assert.NotNull(updatedUser);
        Assert.Equal(0, updatedUser.Balance);

        var userItems = context.UserItems.Where(ui => ui.UserId == userId).ToList();
        Assert.Single(userItems);
    }

    [Fact]
    public async Task PurchaseOffer_ShouldAllowMultiplePurchases_OfSameOffer()
    {
        // Arrange
        await using var context = _fixture.CreateDbContext();
        var service = new MerchantItemOfferService(_logger, context);

        var userId = Guid.NewGuid();
        var user = new User
        {
            Id = userId,
            UserName = "collector",
            NormalizedUserName = "COLLECTOR",
            Email = "collector@example.com",
            NormalizedEmail = "COLLECTOR@EXAMPLE.COM",
            PasswordHash = "AQAAAAIAAYagAAAAEDummyHashForTestingPurposesOnly",
            Balance = 500
        };
        context.Users.Add(user);

        var merchant = new InGameMerchant { Id = 1 };
        context.InGameMerchants.Add(merchant);

        var item = new Item
        {
            Name = "Stackable Potion",
            Description = "Can buy multiple",
            Type = ItemTypes.Consumable,
            ThumbnailUrl = "assets/potion.png"
        };
        context.Items.Add(item);
        await context.SaveChangesAsync();

        var offer = new MerchantItemOffer
        {
            MerchantId = merchant.Id,
            ItemId = item.Id,
            Price = 50
        };
        context.MerchantItemOffers.Add(offer);
        await context.SaveChangesAsync();

        // Act
        await service.PurchaseOffer(userId, offer.Id, CancellationToken.None);
        await service.PurchaseOffer(userId, offer.Id, CancellationToken.None);
        await service.PurchaseOffer(userId, offer.Id, CancellationToken.None);

        // Assert
        var updatedUser = await context.Users.FindAsync(userId);
        Assert.NotNull(updatedUser);
        Assert.Equal(350, updatedUser.Balance); // 500 - (50 * 3)

        var userItems = context.UserItems.Where(ui => ui.UserId == userId).ToList();
        Assert.Equal(3, userItems.Count);
        Assert.All(userItems, ui => Assert.Equal(item.Id, ui.ItemId));
    }

    #endregion
}

