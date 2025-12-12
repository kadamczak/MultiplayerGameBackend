using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using MultiplayerGameBackend.API.Tests.Common;
using MultiplayerGameBackend.Application.MerchantItemOffers.Responses;
using MultiplayerGameBackend.Domain.Constants;
using MultiplayerGameBackend.Domain.Entities;
using MultiplayerGameBackend.Infrastructure.Persistence;

namespace MultiplayerGameBackend.API.Tests.Controllers;

public class MerchantItemOfferControllerTests : IClassFixture<CustomWebApplicationFactory>, IAsyncLifetime
{
    private readonly CustomWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public MerchantItemOfferControllerTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    public Task InitializeAsync() => Task.CompletedTask;

    public async Task DisposeAsync() => await _factory.ResetDatabase();

    #region Helper Methods

    private string GenerateJwtToken(User user, IEnumerable<string> roles)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Name, user.UserName!),
            new(ClaimTypes.Email, user.Email!)
        };

        claims.AddRange(roles.Select(role => new Claim(ClaimTypes.Role, role)));

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(
            "ThisIsAVeryLongSecretKeyForTestingPurposesOnly1234567890"));

        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: "TestIssuer",
            audience: "TestAudience",
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(30),
            signingCredentials: creds
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private async Task<(InGameMerchant merchant, Item item, MerchantItemOffer offer)> CreateTestMerchantWithOffer(int price)
    {
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<MultiplayerGameDbContext>();

        var merchant = new InGameMerchant();
        context.InGameMerchants.Add(merchant);
        await context.SaveChangesAsync();

        var item = new Item
        {
            Name = "Test Item",
            Description = "A test item",
            Type = ItemTypes.Consumable,
            ThumbnailUrl = "item.png"
        };
        context.Items.Add(item);
        await context.SaveChangesAsync();

        var offer = new MerchantItemOffer
        {
            MerchantId = merchant.Id,
            ItemId = item.Id,
            Price = price
        };
        context.MerchantItemOffers.Add(offer);
        await context.SaveChangesAsync();

        return (merchant, item, offer);
    }

    #endregion

    #region GetOffers Tests

    [Fact]
    public async Task GetOffers_ShouldReturnOk_WhenMerchantHasOffers()
    {
        // Arrange
        var user = await _factory.CreateTestUser("testuser", "test@example.com", "Password123!");
        var token = GenerateJwtToken(user, new[] { "User" });
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var (merchant, _, offer) = await CreateTestMerchantWithOffer(100);

        // Act
        var response = await _client.GetAsync($"/v1/merchants/{merchant.Id}/offers");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var offers = await response.Content.ReadFromJsonAsync<List<ReadMerchantOfferDto>>();
        Assert.NotNull(offers);
        Assert.NotEmpty(offers);
        Assert.Equal(offer.Id, offers[0].Id);
        Assert.Equal(100, offers[0].Price);
    }

    [Fact]
    public async Task GetOffers_ShouldReturnOk_WhenMerchantHasNoOffers()
    {
        // Arrange
        var user = await _factory.CreateTestUser("testuser", "test@example.com", "Password123!");
        var token = GenerateJwtToken(user, new[] { "User" });
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<MultiplayerGameDbContext>();

        var merchant = new InGameMerchant();
        context.InGameMerchants.Add(merchant);
        await context.SaveChangesAsync();

        // Act
        var response = await _client.GetAsync($"/v1/merchants/{merchant.Id}/offers");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var offers = await response.Content.ReadFromJsonAsync<List<ReadMerchantOfferDto>>();
        Assert.NotNull(offers);
        Assert.Empty(offers);
    }

    [Fact]
    public async Task GetOffers_ShouldReturnNotFound_WhenMerchantDoesNotExist()
    {
        // Arrange
        var user = await _factory.CreateTestUser("testuser", "test@example.com", "Password123!");
        var token = GenerateJwtToken(user, new[] { "User" });
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Act
        var response = await _client.GetAsync("/v1/merchants/99999/offers");

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetOffers_ShouldReturnUnauthorized_WhenNotAuthenticated()
    {
        // Arrange
        var (merchant, _, _) = await CreateTestMerchantWithOffer(100);

        // Act
        var response = await _client.GetAsync($"/v1/merchants/{merchant.Id}/offers");

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetOffers_ShouldReturnMultipleOffers_WhenMerchantHasMany()
    {
        // Arrange
        var user = await _factory.CreateTestUser("testuser", "test@example.com", "Password123!");
        var token = GenerateJwtToken(user, new[] { "User" });
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<MultiplayerGameDbContext>();

        var merchant = new InGameMerchant();
        context.InGameMerchants.Add(merchant);
        await context.SaveChangesAsync();

        var item1 = new Item { Name = "Item 1", Description = "Desc 1", Type = ItemTypes.Consumable, ThumbnailUrl = "1.png" };
        var item2 = new Item { Name = "Item 2", Description = "Desc 2", Type = ItemTypes.EquippableOnHead, ThumbnailUrl = "2.png" };
        var item3 = new Item { Name = "Item 3", Description = "Desc 3", Type = ItemTypes.EquippableOnBody, ThumbnailUrl = "3.png" };
        context.Items.AddRange(item1, item2, item3);
        await context.SaveChangesAsync();

        var offer1 = new MerchantItemOffer { MerchantId = merchant.Id, ItemId = item1.Id, Price = 50 };
        var offer2 = new MerchantItemOffer { MerchantId = merchant.Id, ItemId = item2.Id, Price = 100 };
        var offer3 = new MerchantItemOffer { MerchantId = merchant.Id, ItemId = item3.Id, Price = 150 };
        context.MerchantItemOffers.AddRange(offer1, offer2, offer3);
        await context.SaveChangesAsync();

        // Act
        var response = await _client.GetAsync($"/v1/merchants/{merchant.Id}/offers");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var offers = await response.Content.ReadFromJsonAsync<List<ReadMerchantOfferDto>>();
        Assert.NotNull(offers);
        Assert.Equal(3, offers.Count);
    }

    #endregion

    #region PurchaseOffer Tests

    [Fact]
    public async Task PurchaseOffer_ShouldReturnNoContent_WhenPurchaseSuccessful()
    {
        // Arrange
        var user = await _factory.CreateTestUser("buyer", "buyer@example.com", "Password123!");
        
        // Update user balance
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<MultiplayerGameDbContext>();
        var dbUser = await context.Users.FindAsync(user.Id);
        dbUser!.Balance = 500;
        await context.SaveChangesAsync();

        var token = GenerateJwtToken(user, new[] { "User" });
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var (_, _, offer) = await CreateTestMerchantWithOffer(100);

        // Act
        var response = await _client.PostAsync($"/v1/merchants/offers/{offer.Id}/purchase", null);

        // Assert
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        // Verify balance was deducted
        using var verifyScope = _factory.Services.CreateScope();
        var verifyContext = verifyScope.ServiceProvider.GetRequiredService<MultiplayerGameDbContext>();
        var updatedUser = await verifyContext.Users.FindAsync(user.Id);
        Assert.Equal(400, updatedUser!.Balance); // 500 - 100

        // Verify user received the item
        var userItem = verifyContext.UserItems.FirstOrDefault(ui => ui.UserId == user.Id);
        Assert.NotNull(userItem);
    }

    [Fact]
    public async Task PurchaseOffer_ShouldReturnUnprocessableEntity_WhenInsufficientBalance()
    {
        // Arrange
        var user = await _factory.CreateTestUser("poorbuyer", "poor@example.com", "Password123!");
        
        // Set low balance
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<MultiplayerGameDbContext>();
        var dbUser = await context.Users.FindAsync(user.Id);
        dbUser!.Balance = 50; // Not enough for 100
        await context.SaveChangesAsync();

        var token = GenerateJwtToken(user, new[] { "User" });
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var (_, _, offer) = await CreateTestMerchantWithOffer(100);

        // Act
        var response = await _client.PostAsync($"/v1/merchants/offers/{offer.Id}/purchase", null);

        // Assert
        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);

        // Verify balance unchanged
        using var verifyScope = _factory.Services.CreateScope();
        var verifyContext = verifyScope.ServiceProvider.GetRequiredService<MultiplayerGameDbContext>();
        var updatedUser = await verifyContext.Users.FindAsync(user.Id);
        Assert.Equal(50, updatedUser!.Balance);
    }

    [Fact]
    public async Task PurchaseOffer_ShouldReturnNotFound_WhenOfferDoesNotExist()
    {
        // Arrange
        var user = await _factory.CreateTestUser("buyer", "buyer@example.com", "Password123!");
        var token = GenerateJwtToken(user, new[] { "User" });
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Act
        var response = await _client.PostAsync("/v1/merchants/offers/99999/purchase", null);

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task PurchaseOffer_ShouldReturnUnauthorized_WhenNotAuthenticated()
    {
        // Arrange
        var (_, _, offer) = await CreateTestMerchantWithOffer(100);

        // Act
        var response = await _client.PostAsync($"/v1/merchants/offers/{offer.Id}/purchase", null);

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task PurchaseOffer_ShouldDeductExactAmount_WhenPurchasing()
    {
        // Arrange
        var user = await _factory.CreateTestUser("buyer", "buyer@example.com", "Password123!");
        
        // Set exact balance
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<MultiplayerGameDbContext>();
        var dbUser = await context.Users.FindAsync(user.Id);
        dbUser!.Balance = 150;
        await context.SaveChangesAsync();

        var token = GenerateJwtToken(user, new[] { "User" });
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var (_, _, offer) = await CreateTestMerchantWithOffer(150);

        // Act
        var response = await _client.PostAsync($"/v1/merchants/offers/{offer.Id}/purchase", null);

        // Assert
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        // Verify balance is now 0
        using var verifyScope = _factory.Services.CreateScope();
        var verifyContext = verifyScope.ServiceProvider.GetRequiredService<MultiplayerGameDbContext>();
        var updatedUser = await verifyContext.Users.FindAsync(user.Id);
        Assert.Equal(0, updatedUser!.Balance);
    }

    [Fact]
    public async Task PurchaseOffer_ShouldAllowMultiplePurchases_FromSameMerchant()
    {
        // Arrange
        var user = await _factory.CreateTestUser("richbuyer", "rich@example.com", "Password123!");
        
        // Set high balance
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<MultiplayerGameDbContext>();
        var dbUser = await context.Users.FindAsync(user.Id);
        dbUser!.Balance = 1000;
        await context.SaveChangesAsync();

        var token = GenerateJwtToken(user, new[] { "User" });
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var (merchant, item1, offer1) = await CreateTestMerchantWithOffer(100);
        
        // Create second offer from same merchant
        using var scope2 = _factory.Services.CreateScope();
        var context2 = scope2.ServiceProvider.GetRequiredService<MultiplayerGameDbContext>();
        var item2 = new Item { Name = "Item 2", Description = "Desc 2", Type = ItemTypes.Consumable, ThumbnailUrl = "2.png" };
        context2.Items.Add(item2);
        await context2.SaveChangesAsync();
        
        var offer2 = new MerchantItemOffer { MerchantId = merchant.Id, ItemId = item2.Id, Price = 150 };
        context2.MerchantItemOffers.Add(offer2);
        await context2.SaveChangesAsync();

        // Act
        var response1 = await _client.PostAsync($"/v1/merchants/offers/{offer1.Id}/purchase", null);
        var response2 = await _client.PostAsync($"/v1/merchants/offers/{offer2.Id}/purchase", null);

        // Assert
        Assert.Equal(HttpStatusCode.NoContent, response1.StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, response2.StatusCode);

        // Verify total deduction
        using var verifyScope = _factory.Services.CreateScope();
        var verifyContext = verifyScope.ServiceProvider.GetRequiredService<MultiplayerGameDbContext>();
        var updatedUser = await verifyContext.Users.FindAsync(user.Id);
        Assert.Equal(750, updatedUser!.Balance); // 1000 - 100 - 150

        // Verify user has both items
        var userItems = verifyContext.UserItems.Where(ui => ui.UserId == user.Id).ToList();
        Assert.Equal(2, userItems.Count);
    }

    [Fact]
    public async Task PurchaseOffer_ShouldWorkForDifferentUsers_PurchasingSameOffer()
    {
        // Arrange
        var user1 = await _factory.CreateTestUser("buyer1", "buyer1@example.com", "Password123!");
        var user2 = await _factory.CreateTestUser("buyer2", "buyer2@example.com", "Password123!");
        
        // Set balances
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<MultiplayerGameDbContext>();
        var dbUser1 = await context.Users.FindAsync(user1.Id);
        var dbUser2 = await context.Users.FindAsync(user2.Id);
        dbUser1!.Balance = 200;
        dbUser2!.Balance = 200;
        await context.SaveChangesAsync();

        var (_, _, offer) = await CreateTestMerchantWithOffer(100);

        // Act - User 1 purchases
        var token1 = GenerateJwtToken(user1, new[] { "User" });
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token1);
        var response1 = await _client.PostAsync($"/v1/merchants/offers/{offer.Id}/purchase", null);

        // Act - User 2 purchases same offer
        var token2 = GenerateJwtToken(user2, new[] { "User" });
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token2);
        var response2 = await _client.PostAsync($"/v1/merchants/offers/{offer.Id}/purchase", null);

        // Assert
        Assert.Equal(HttpStatusCode.NoContent, response1.StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, response2.StatusCode);

        // Verify both users got the item
        using var verifyScope = _factory.Services.CreateScope();
        var verifyContext = verifyScope.ServiceProvider.GetRequiredService<MultiplayerGameDbContext>();
        
        var user1Items = verifyContext.UserItems.Where(ui => ui.UserId == user1.Id).ToList();
        var user2Items = verifyContext.UserItems.Where(ui => ui.UserId == user2.Id).ToList();
        
        Assert.Single(user1Items);
        Assert.Single(user2Items);
    }

    #endregion
}

