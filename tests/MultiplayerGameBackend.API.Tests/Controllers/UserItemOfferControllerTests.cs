using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using MultiplayerGameBackend.API.Tests.TestHelpers;
using MultiplayerGameBackend.Application.UserItemOffers.Requests;
using MultiplayerGameBackend.Application.UserItemOffers.Responses;
using MultiplayerGameBackend.Domain.Constants;
using MultiplayerGameBackend.Domain.Entities;
using MultiplayerGameBackend.Infrastructure.Persistence;

namespace MultiplayerGameBackend.API.Tests.Controllers;

public class UserItemOfferControllerTests : IClassFixture<CustomWebApplicationFactory>, IAsyncLifetime
{
    private readonly CustomWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public UserItemOfferControllerTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    public Task InitializeAsync() => Task.CompletedTask;

    public async Task DisposeAsync() => await _factory.ResetDatabase();

    #region Helper Methods

    private string GenerateJwtToken(User user, IEnumerable<string> roles) =>
        JwtTokenHelper.GenerateJwtToken(user, roles);

    private async Task<Item> AddItemToDatabase(string name, string type) =>
        await TestDatabaseHelper.AddItemToDatabase(_factory.Services, name, type);

    private async Task<UserItem> AddUserItemToDatabase(Guid userId, int itemId) =>
        await TestDatabaseHelper.AddUserItemToDatabase(_factory.Services, userId, itemId);

    private async Task<UserItemOffer> AddUserItemOfferToDatabase(Guid sellerId, Guid userItemId, int price) =>
        await TestDatabaseHelper.AddUserItemOfferToDatabase(_factory.Services, sellerId, userItemId, price);

    private async Task SetUserBalanceInDatabase(Guid userId, int balance) =>
        await TestDatabaseHelper.SetUserBalanceInDatabase(_factory.Services, userId, balance);

    #endregion

    #region GetOffers Tests

    [Fact]
    public async Task GetOffers_ShouldReturnOk_WhenActiveOffersExist()
    {
        // Arrange
        var seller = await _factory.CreateTestUser("seller", "seller@example.com", "Password123!");
        var user = await _factory.CreateTestUser("testuser", "test@example.com", "Password123!");
        var token = GenerateJwtToken(user, new[] { "User" });
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var item = await AddItemToDatabase("Test Item", ItemTypes.Consumable);
        var userItem = await AddUserItemToDatabase(seller.Id, item.Id);
        await AddUserItemOfferToDatabase(seller.Id, userItem.Id, 100);

        // Act
        var response = await _client.GetAsync("/v1/users/offers?ShowActive=true");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<PagedResult<ReadUserItemOfferDto>>();
        Assert.NotNull(result);
        Assert.Equal(1, result.TotalItemsCount);
        Assert.Single(result.Items);
    }

    [Fact]
    public async Task GetOffers_ShouldReturnEmptyList_WhenNoOffersExist()
    {
        // Arrange
        var user = await _factory.CreateTestUser("testuser", "test@example.com", "Password123!");
        var token = GenerateJwtToken(user, new[] { "User" });
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Act
        var response = await _client.GetAsync("/v1/users/offers?ShowActive=true");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<PagedResult<ReadUserItemOfferDto>>();
        Assert.NotNull(result);
        Assert.Equal(0, result.TotalItemsCount);
        Assert.Empty(result.Items);
    }

    [Fact]
    public async Task GetOffers_ShouldReturnUnauthorized_WhenNotAuthenticated()
    {
        // Act
        var response = await _client.GetAsync("/v1/users/offers?ShowActive=true");

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetOffers_ShouldReturnOnlyActiveOffers_WhenShowActiveIsTrue()
    {
        // Arrange
        var seller = await _factory.CreateTestUser("seller", "seller@example.com", "Password123!");
        var buyer = await _factory.CreateTestUser("buyer", "buyer@example.com", "Password123!");
        var user = await _factory.CreateTestUser("testuser", "test@example.com", "Password123!");
        var token = GenerateJwtToken(user, new[] { "User" });
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var item1 = await AddItemToDatabase("Item 1", ItemTypes.Consumable);
        var item2 = await AddItemToDatabase("Item 2", ItemTypes.Consumable);
        var userItem1 = await AddUserItemToDatabase(seller.Id, item1.Id);
        var userItem2 = await AddUserItemToDatabase(seller.Id, item2.Id);

        var activeOffer = await AddUserItemOfferToDatabase(seller.Id, userItem1.Id, 100);
        var inactiveOffer = await AddUserItemOfferToDatabase(seller.Id, userItem2.Id, 200);

        // Mark one offer as inactive by setting buyer
        using (var scope = _factory.Services.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<MultiplayerGameDbContext>();
            var offer = await context.UserItemOffers.FindAsync(inactiveOffer.Id);
            if (offer != null)
            {
                offer.BuyerId = buyer.Id;
                offer.BoughtAt = DateTime.UtcNow;
                await context.SaveChangesAsync();
            }
        }

        // Act
        var response = await _client.GetAsync("/v1/users/offers?ShowActive=true");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<PagedResult<ReadUserItemOfferDto>>();
        Assert.NotNull(result);
        Assert.Equal(1, result.TotalItemsCount);
        Assert.Single(result.Items);
        Assert.Equal(activeOffer.Id, result.Items.First().Id);
    }

    [Fact]
    public async Task GetOffers_ShouldReturnOnlyInactiveOffers_WhenShowActiveIsFalse()
    {
        // Arrange
        var seller = await _factory.CreateTestUser("seller", "seller@example.com", "Password123!");
        var buyer = await _factory.CreateTestUser("buyer", "buyer@example.com", "Password123!");
        var user = await _factory.CreateTestUser("testuser", "test@example.com", "Password123!");
        var token = GenerateJwtToken(user, new[] { "User" });
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var item1 = await AddItemToDatabase("Item 1", ItemTypes.Consumable);
        var item2 = await AddItemToDatabase("Item 2", ItemTypes.Consumable);
        var userItem1 = await AddUserItemToDatabase(seller.Id, item1.Id);
        var userItem2 = await AddUserItemToDatabase(seller.Id, item2.Id);

        await AddUserItemOfferToDatabase(seller.Id, userItem1.Id, 100);
        var inactiveOffer = await AddUserItemOfferToDatabase(seller.Id, userItem2.Id, 200);

        // Mark one offer as inactive
        using (var scope = _factory.Services.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<MultiplayerGameDbContext>();
            var offer = await context.UserItemOffers.FindAsync(inactiveOffer.Id);
            if (offer != null)
            {
                offer.BuyerId = buyer.Id;
                offer.BoughtAt = DateTime.UtcNow;
                await context.SaveChangesAsync();
            }
        }

        // Act
        var response = await _client.GetAsync("/v1/users/offers?ShowActive=false");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<PagedResult<ReadUserItemOfferDto>>();
        Assert.NotNull(result);
        Assert.Equal(1, result.TotalItemsCount);
        Assert.Single(result.Items);
        Assert.Equal(inactiveOffer.Id, result.Items.First().Id);
    }

    #endregion

    #region CreateOffer Tests

    [Fact]
    public async Task CreateOffer_ShouldReturnNoContent_WhenValidData()
    {
        // Arrange
        var user = await _factory.CreateTestUser("testuser", "test@example.com", "Password123!");
        var token = GenerateJwtToken(user, new[] { "User" });
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var item = await AddItemToDatabase("Test Item", ItemTypes.Consumable);
        var userItem = await AddUserItemToDatabase(user.Id, item.Id);

        var dto = new CreateUserItemOfferDto
        {
            UserItemId = userItem.Id,
            Price = 100
        };

        // Act
        var response = await _client.PostAsJsonAsync("/v1/users/offers", dto);

        // Assert
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        // Verify the offer was created
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<MultiplayerGameDbContext>();
        var offer = await context.UserItemOffers.FirstOrDefaultAsync(o => o.UserItemId == userItem.Id);
        Assert.NotNull(offer);
        Assert.Equal(100, offer.Price);
        Assert.Equal(user.Id, offer.SellerId);
    }

    [Fact]
    public async Task CreateOffer_ShouldReturnUnauthorized_WhenNotAuthenticated()
    {
        // Arrange
        var dto = new CreateUserItemOfferDto
        {
            UserItemId = Guid.NewGuid(),
            Price = 100
        };

        // Act
        var response = await _client.PostAsJsonAsync("/v1/users/offers", dto);

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task CreateOffer_ShouldReturnNotFound_WhenUserItemDoesNotExist()
    {
        // Arrange
        var user = await _factory.CreateTestUser("testuser", "test@example.com", "Password123!");
        var token = GenerateJwtToken(user, new[] { "User" });
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var dto = new CreateUserItemOfferDto
        {
            UserItemId = Guid.NewGuid(),
            Price = 100
        };

        // Act
        var response = await _client.PostAsJsonAsync("/v1/users/offers", dto);

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task CreateOffer_ShouldReturnForbidden_WhenTryingToSellAnotherUsersItem()
    {
        // Arrange
        var seller = await _factory.CreateTestUser("seller", "seller@example.com", "Password123!");
        var user = await _factory.CreateTestUser("testuser", "test@example.com", "Password123!");
        var token = GenerateJwtToken(user, new[] { "User" });
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var item = await AddItemToDatabase("Test Item", ItemTypes.Consumable);
        var userItem = await AddUserItemToDatabase(seller.Id, item.Id);

        var dto = new CreateUserItemOfferDto
        {
            UserItemId = userItem.Id,
            Price = 100
        };

        // Act
        var response = await _client.PostAsJsonAsync("/v1/users/offers", dto);

        // Assert
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task CreateOffer_ShouldReturnConflict_WhenOfferAlreadyExists()
    {
        // Arrange
        var user = await _factory.CreateTestUser("testuser", "test@example.com", "Password123!");
        var token = GenerateJwtToken(user, new[] { "User" });
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var item = await AddItemToDatabase("Test Item", ItemTypes.Consumable);
        var userItem = await AddUserItemToDatabase(user.Id, item.Id);
        await AddUserItemOfferToDatabase(user.Id, userItem.Id, 100);

        var dto = new CreateUserItemOfferDto
        {
            UserItemId = userItem.Id,
            Price = 150
        };

        // Act
        var response = await _client.PostAsJsonAsync("/v1/users/offers", dto);

        // Assert
        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task CreateOffer_ShouldReturnBadRequest_WhenPriceIsNegative()
    {
        // Arrange
        var user = await _factory.CreateTestUser("testuser", "test@example.com", "Password123!");
        var token = GenerateJwtToken(user, new[] { "User" });
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var item = await AddItemToDatabase("Test Item", ItemTypes.Consumable);
        var userItem = await AddUserItemToDatabase(user.Id, item.Id);

        var dto = new CreateUserItemOfferDto
        {
            UserItemId = userItem.Id,
            Price = -1
        };

        // Act
        var response = await _client.PostAsJsonAsync("/v1/users/offers", dto);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task CreateOffer_ShouldReturnBadRequest_WhenPriceExceedsMaximum()
    {
        // Arrange
        var user = await _factory.CreateTestUser("testuser", "test@example.com", "Password123!");
        var token = GenerateJwtToken(user, new[] { "User" });
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var item = await AddItemToDatabase("Test Item", ItemTypes.Consumable);
        var userItem = await AddUserItemToDatabase(user.Id, item.Id);

        var dto = new CreateUserItemOfferDto
        {
            UserItemId = userItem.Id,
            Price = 1_000_000
        };

        // Act
        var response = await _client.PostAsJsonAsync("/v1/users/offers", dto);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    #endregion

    #region DeleteOffer Tests

    [Fact]
    public async Task DeleteOffer_ShouldReturnNoContent_WhenValidData()
    {
        // Arrange
        var user = await _factory.CreateTestUser("testuser", "test@example.com", "Password123!");
        var token = GenerateJwtToken(user, new[] { "User" });
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var item = await AddItemToDatabase("Test Item", ItemTypes.Consumable);
        var userItem = await AddUserItemToDatabase(user.Id, item.Id);
        var offer = await AddUserItemOfferToDatabase(user.Id, userItem.Id, 100);

        // Act
        var response = await _client.DeleteAsync($"/v1/users/offers/{offer.Id}");

        // Assert
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        // Verify the offer was deleted
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<MultiplayerGameDbContext>();
        var deletedOffer = await context.UserItemOffers.FindAsync(offer.Id);
        Assert.Null(deletedOffer);
    }

    [Fact]
    public async Task DeleteOffer_ShouldReturnUnauthorized_WhenNotAuthenticated()
    {
        // Act
        var response = await _client.DeleteAsync($"/v1/users/offers/{Guid.NewGuid()}");

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task DeleteOffer_ShouldReturnNotFound_WhenOfferDoesNotExist()
    {
        // Arrange
        var user = await _factory.CreateTestUser("testuser", "test@example.com", "Password123!");
        var token = GenerateJwtToken(user, new[] { "User" });
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Act
        var response = await _client.DeleteAsync($"/v1/users/offers/{Guid.NewGuid()}");

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task DeleteOffer_ShouldReturnForbidden_WhenTryingToDeleteAnotherUsersOffer()
    {
        // Arrange
        var seller = await _factory.CreateTestUser("seller", "seller@example.com", "Password123!");
        var user = await _factory.CreateTestUser("testuser", "test@example.com", "Password123!");
        var token = GenerateJwtToken(user, new[] { "User" });
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var item = await AddItemToDatabase("Test Item", ItemTypes.Consumable);
        var userItem = await AddUserItemToDatabase(seller.Id, item.Id);
        var offer = await AddUserItemOfferToDatabase(seller.Id, userItem.Id, 100);

        // Act
        var response = await _client.DeleteAsync($"/v1/users/offers/{offer.Id}");

        // Assert
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    #endregion

    #region PurchaseOffer Tests

    [Fact]
    public async Task PurchaseOffer_ShouldReturnNoContent_WhenSuccessful()
    {
        // Arrange
        var seller = await _factory.CreateTestUser("seller", "seller@example.com", "Password123!");
        var buyer = await _factory.CreateTestUser("buyer", "buyer@example.com", "Password123!");
        var token = GenerateJwtToken(buyer, new[] { "User" });
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        await SetUserBalanceInDatabase(buyer.Id, 500);
        await SetUserBalanceInDatabase(seller.Id, 0); // Explicitly set seller balance to 0

        var item = await AddItemToDatabase("Test Item", ItemTypes.Consumable);
        var userItem = await AddUserItemToDatabase(seller.Id, item.Id);
        var offer = await AddUserItemOfferToDatabase(seller.Id, userItem.Id, 100);

        // Act
        var response = await _client.PostAsync($"/v1/users/offers/{offer.Id}/purchase", null);

        // Assert
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        // Verify the purchase
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<MultiplayerGameDbContext>();
        
        var purchasedOffer = await context.UserItemOffers.FindAsync(offer.Id);
        Assert.NotNull(purchasedOffer);
        Assert.Equal(buyer.Id, purchasedOffer.BuyerId);
        Assert.NotNull(purchasedOffer.BoughtAt);

        var buyerUser = await context.Users.FindAsync(buyer.Id);
        Assert.Equal(400, buyerUser!.Balance); // 500 - 100

        var sellerUser = await context.Users.FindAsync(seller.Id);
        Assert.Equal(100, sellerUser!.Balance); // 0 + 100

        var newUserItem = await context.UserItems
            .FirstOrDefaultAsync(ui => ui.UserId == buyer.Id && ui.ItemId == item.Id);
        Assert.NotNull(newUserItem);
    }

    [Fact]
    public async Task PurchaseOffer_ShouldReturnUnauthorized_WhenNotAuthenticated()
    {
        // Act
        var response = await _client.PostAsync($"/v1/users/offers/{Guid.NewGuid()}/purchase", null);

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task PurchaseOffer_ShouldReturnNotFound_WhenOfferDoesNotExist()
    {
        // Arrange
        var buyer = await _factory.CreateTestUser("buyer", "buyer@example.com", "Password123!");
        var token = GenerateJwtToken(buyer, new[] { "User" });
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Act
        var response = await _client.PostAsync($"/v1/users/offers/{Guid.NewGuid()}/purchase", null);

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task PurchaseOffer_ShouldReturnUnprocessableEntity_WhenInsufficientFunds()
    {
        // Arrange
        var seller = await _factory.CreateTestUser("seller", "seller@example.com", "Password123!");
        var buyer = await _factory.CreateTestUser("buyer", "buyer@example.com", "Password123!");
        var token = GenerateJwtToken(buyer, new[] { "User" });
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        await SetUserBalanceInDatabase(buyer.Id, 50);

        var item = await AddItemToDatabase("Test Item", ItemTypes.Consumable);
        var userItem = await AddUserItemToDatabase(seller.Id, item.Id);
        var offer = await AddUserItemOfferToDatabase(seller.Id, userItem.Id, 100);

        // Act
        var response = await _client.PostAsync($"/v1/users/offers/{offer.Id}/purchase", null);

        // Assert
        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
    }

    [Fact]
    public async Task PurchaseOffer_ShouldReturnUnprocessableEntity_WhenTryingToBuyOwnOffer()
    {
        // Arrange
        var user = await _factory.CreateTestUser("testuser", "test@example.com", "Password123!");
        var token = GenerateJwtToken(user, new[] { "User" });
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        await SetUserBalanceInDatabase(user.Id, 500);

        var item = await AddItemToDatabase("Test Item", ItemTypes.Consumable);
        var userItem = await AddUserItemToDatabase(user.Id, item.Id);
        var offer = await AddUserItemOfferToDatabase(user.Id, userItem.Id, 100);

        // Act
        var response = await _client.PostAsync($"/v1/users/offers/{offer.Id}/purchase", null);

        // Assert
        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
    }


    #endregion
}

