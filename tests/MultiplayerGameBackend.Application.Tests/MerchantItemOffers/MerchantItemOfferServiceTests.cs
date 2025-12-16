using Microsoft.Extensions.Logging;
using MultiplayerGameBackend.Application.MerchantItemOffers;
using MultiplayerGameBackend.Application.Tests.TestHelpers;
using MultiplayerGameBackend.Domain.Constants;
using MultiplayerGameBackend.Domain.Exceptions;
using MultiplayerGameBackend.Tests.Shared.Factories;
using MultiplayerGameBackend.Tests.Shared.Helpers;
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

        var merchant = await DatabaseHelper.CreateAndSaveMerchant(context);
        var item1 = await DatabaseHelper.CreateAndSaveItem(context, "Health Potion", ItemTypes.Consumable, "Restores 50 HP", "assets/health_potion.png");
        var item2 = await DatabaseHelper.CreateAndSaveItem(context, "Iron Sword", ItemTypes.EquippableOnBody, "A sturdy iron sword", "assets/iron_sword.png");
        
        await DatabaseHelper.CreateAndSaveMerchantItemOffer(context, merchant.Id, item1.Id, 50);
        await DatabaseHelper.CreateAndSaveMerchantItemOffer(context, merchant.Id, item2.Id, 150);


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

        var merchant = await DatabaseHelper.CreateAndSaveMerchant(context);

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

        var merchant1 = await DatabaseHelper.CreateAndSaveMerchant(context);
        var merchant2 = await DatabaseHelper.CreateAndSaveMerchant(context);

        var item1 = await DatabaseHelper.CreateAndSaveItem(context, "Merchant 1 Item", ItemTypes.Consumable, "Item for merchant 1", "assets/item1.png");
        var item2 = await DatabaseHelper.CreateAndSaveItem(context, "Merchant 2 Item", ItemTypes.Consumable, "Item for merchant 2", "assets/item2.png");

        await DatabaseHelper.CreateAndSaveMerchantItemOffer(context, merchant1.Id, item1.Id, 100);
        await DatabaseHelper.CreateAndSaveMerchantItemOffer(context, merchant2.Id, item2.Id, 200);

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
        var userManager = IdentityHelper.CreateUserManager(context);

        var user = await DatabaseHelper.CreateAndSaveUser(userManager, "testuser", "test@example.com", "Password123!", balance: 500);
        var (merchant, item, offer) = await DatabaseHelper.CreateAndSaveMerchantWithOffer(context, 200, "Magic Scroll", ItemTypes.Consumable);

        // Act
        await service.PurchaseOffer(user.Id, offer.Id, CancellationToken.None);

        // Assert
        var updatedUser = await context.Users.FindAsync(user.Id);
        Assert.NotNull(updatedUser);
        Assert.Equal(300, updatedUser.Balance); // 500 - 200

        var userItems = context.UserItems.Where(ui => ui.UserId == user.Id).ToList();
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

        var (merchant, item, offer) = await DatabaseHelper.CreateAndSaveMerchantWithOffer(context, 100);

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
        var userManager = IdentityHelper.CreateUserManager(context);

        var user = await DatabaseHelper.CreateAndSaveUser(userManager, "pooruser", "poor@example.com", "Password123!", balance: 50);
        var (merchant, item, offer) = await DatabaseHelper.CreateAndSaveMerchantWithOffer(context, 200, "Expensive Item", ItemTypes.EquippableOnHead);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<UnprocessableEntityException>(
            () => service.PurchaseOffer(user.Id, offer.Id, CancellationToken.None)
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
        var userManager = IdentityHelper.CreateUserManager(context);

        var user = await DatabaseHelper.CreateAndSaveUser(userManager, "exactuser", "exact@example.com", "Password123!", balance: 100);
        var (merchant, item, offer) = await DatabaseHelper.CreateAndSaveMerchantWithOffer(context, 100, "Exact Price Item", ItemTypes.Consumable);

        // Act
        await service.PurchaseOffer(user.Id, offer.Id, CancellationToken.None);

        // Assert
        var updatedUser = await context.Users.FindAsync(user.Id);
        Assert.NotNull(updatedUser);
        Assert.Equal(0, updatedUser.Balance);

        var userItems = context.UserItems.Where(ui => ui.UserId == user.Id).ToList();
        Assert.Single(userItems);
    }

    [Fact]
    public async Task PurchaseOffer_ShouldAllowMultiplePurchases_OfSameOffer()
    {
        // Arrange
        await using var context = _fixture.CreateDbContext();
        var service = new MerchantItemOfferService(_logger, context);
        var userManager = IdentityHelper.CreateUserManager(context);

        var user = await DatabaseHelper.CreateAndSaveUser(userManager, "collector", "collector@example.com", "Password123!", balance: 500);
        var (merchant, item, offer) = await DatabaseHelper.CreateAndSaveMerchantWithOffer(context, 50, "Stackable Potion", ItemTypes.Consumable);

        // Act
        await service.PurchaseOffer(user.Id, offer.Id, CancellationToken.None);
        await service.PurchaseOffer(user.Id, offer.Id, CancellationToken.None);
        await service.PurchaseOffer(user.Id, offer.Id, CancellationToken.None);

        // Assert
        var updatedUser = await context.Users.FindAsync(user.Id);
        Assert.NotNull(updatedUser);
        Assert.Equal(350, updatedUser.Balance); // 500 - (50 * 3)

        var userItems = context.UserItems.Where(ui => ui.UserId == user.Id).ToList();
        Assert.Equal(3, userItems.Count);
        Assert.All(userItems, ui => Assert.Equal(item.Id, ui.ItemId));
    }

    #endregion
}

