using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using MultiplayerGameBackend.API.Tests.TestHelpers;
using MultiplayerGameBackend.Application.UserItems.Requests;
using MultiplayerGameBackend.Application.UserItems.Responses;
using MultiplayerGameBackend.Domain.Constants;
using MultiplayerGameBackend.Domain.Entities;
using MultiplayerGameBackend.Infrastructure.Persistence;

namespace MultiplayerGameBackend.API.Tests.Controllers;

public class UserItemControllerTests : IClassFixture<CustomWebApplicationFactory>, IAsyncLifetime
{
    private readonly CustomWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public UserItemControllerTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    public Task InitializeAsync() => Task.CompletedTask;

    public async Task DisposeAsync() => await _factory.ResetDatabase();

    #region Helper Methods

    private string GenerateJwtToken(User user, IEnumerable<string> roles) =>
        JwtTokenHelper.GenerateJwtToken(user, roles);

    private async Task<(Item headItem, Item bodyItem)> AddHeadAndBodyItemsToDatabase() =>
        await TestDatabaseHelper.AddHeadAndBodyItemsToDatabase(_factory.Services);

    private async Task<UserItem> AddUserItemToDatabase(Guid userId, int itemId) =>
        await TestDatabaseHelper.AddUserItemToDatabase(_factory.Services, userId, itemId);

    private async Task AddUserCustomizationToDatabase(Guid userId) =>
        await TestDatabaseHelper.AddUserCustomizationToDatabase(_factory.Services, userId);

    #endregion

    #region GetCurrentUserItems Tests

    [Fact]
    public async Task GetCurrentUserItems_ShouldReturnOk_WhenUserHasItems()
    {
        // Arrange
        var user = await _factory.CreateTestUser("testuser", "test@example.com", "Password123!");
        var token = GenerateJwtToken(user, new[] { "User" });
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var (headItem, bodyItem) = await AddHeadAndBodyItemsToDatabase();
        await AddUserItemToDatabase(user.Id, headItem.Id);
        await AddUserItemToDatabase(user.Id, bodyItem.Id);

        // Act
        var response = await _client.GetAsync("/v1/users/me/items");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<PagedResult<ReadUserItemDto>>();
        Assert.NotNull(result);
        Assert.Equal(2, result.TotalItemsCount);
        Assert.Equal(2, result.Items.Count());
    }

    [Fact]
    public async Task GetCurrentUserItems_ShouldReturnEmptyList_WhenUserHasNoItems()
    {
        // Arrange
        var user = await _factory.CreateTestUser("testuser", "test@example.com", "Password123!");
        var token = GenerateJwtToken(user, new[] { "User" });
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Act
        var response = await _client.GetAsync("/v1/users/me/items");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<PagedResult<ReadUserItemDto>>();
        Assert.NotNull(result);
        Assert.Equal(0, result.TotalItemsCount);
        Assert.Empty(result.Items);
    }

    [Fact]
    public async Task GetCurrentUserItems_ShouldReturnUnauthorized_WhenNotAuthenticated()
    {
        // Act
        var response = await _client.GetAsync("/v1/users/me/items");

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetCurrentUserItems_ShouldRespectPagination()
    {
        // Arrange
        var user = await _factory.CreateTestUser("testuser", "test@example.com", "Password123!");
        var token = GenerateJwtToken(user, new[] { "User" });
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var (headItem, bodyItem) = await AddHeadAndBodyItemsToDatabase();
        await AddUserItemToDatabase(user.Id, headItem.Id);
        await AddUserItemToDatabase(user.Id, bodyItem.Id);

        // Act - use PageSize=5 which is an allowed value
        var response = await _client.GetAsync("/v1/users/me/items?PagedQuery.PageNumber=1&PagedQuery.PageSize=5");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<PagedResult<ReadUserItemDto>>();
        Assert.NotNull(result);
        Assert.Equal(2, result.TotalItemsCount);
        Assert.Equal(2, result.Items.Count()); // Both items should be returned since we have only 2 items
    }

    [Fact]
    public async Task GetCurrentUserItems_ShouldOnlyReturnCurrentUserItems()
    {
        // Arrange
        var user1 = await _factory.CreateTestUser("user1", "user1@example.com", "Password123!");
        var user2 = await _factory.CreateTestUser("user2", "user2@example.com", "Password123!");
        var token = GenerateJwtToken(user1, new[] { "User" });
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var (headItem, bodyItem) = await AddHeadAndBodyItemsToDatabase();
        await AddUserItemToDatabase(user1.Id, headItem.Id);
        await AddUserItemToDatabase(user2.Id, bodyItem.Id);

        // Act
        var response = await _client.GetAsync("/v1/users/me/items");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<PagedResult<ReadUserItemDto>>();
        Assert.NotNull(result);
        Assert.Equal(1, result.TotalItemsCount);
        Assert.Single(result.Items);
        Assert.Equal(headItem.Id, result.Items.First().Item.Id);
    }

    #endregion

    #region UpdateEquippedUserItems Tests

    [Fact]
    public async Task UpdateEquippedUserItems_ShouldReturnNoContent_WhenValidData()
    {
        // Arrange
        var user = await _factory.CreateTestUser("testuser", "test@example.com", "Password123!");
        var token = GenerateJwtToken(user, new[] { "User" });
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        await AddUserCustomizationToDatabase(user.Id);
        var (headItem, _) = await AddHeadAndBodyItemsToDatabase();
        var userHeadItem = await AddUserItemToDatabase(user.Id, headItem.Id);

        var dto = new UpdateEquippedUserItemsDto
        {
            EquippedHeadUserItemId = userHeadItem.Id,
            EquippedBodyUserItemId = null
        };

        // Act
        var response = await _client.PutAsJsonAsync("/v1/users/me/equipped-items", dto);

        // Assert
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        // Verify the update in database
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<MultiplayerGameDbContext>();
        var customization = await context.UserCustomizations.FirstOrDefaultAsync(uc => uc.UserId == user.Id);
        Assert.NotNull(customization);
        Assert.Equal(userHeadItem.Id, customization.EquippedHeadUserItemId);
        Assert.Null(customization.EquippedBodyUserItemId);
    }

    [Fact]
    public async Task UpdateEquippedUserItems_ShouldReturnNoContent_WhenEquippingBothItems()
    {
        // Arrange
        var user = await _factory.CreateTestUser("testuser", "test@example.com", "Password123!");
        var token = GenerateJwtToken(user, new[] { "User" });
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        await AddUserCustomizationToDatabase(user.Id);
        var (headItem, bodyItem) = await AddHeadAndBodyItemsToDatabase();
        var userHeadItem = await AddUserItemToDatabase(user.Id, headItem.Id);
        var userBodyItem = await AddUserItemToDatabase(user.Id, bodyItem.Id);

        var dto = new UpdateEquippedUserItemsDto
        {
            EquippedHeadUserItemId = userHeadItem.Id,
            EquippedBodyUserItemId = userBodyItem.Id
        };

        // Act
        var response = await _client.PutAsJsonAsync("/v1/users/me/equipped-items", dto);

        // Assert
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        // Verify the update in database
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<MultiplayerGameDbContext>();
        var customization = await context.UserCustomizations.FirstOrDefaultAsync(uc => uc.UserId == user.Id);
        Assert.NotNull(customization);
        Assert.Equal(userHeadItem.Id, customization.EquippedHeadUserItemId);
        Assert.Equal(userBodyItem.Id, customization.EquippedBodyUserItemId);
    }

    [Fact]
    public async Task UpdateEquippedUserItems_ShouldReturnNoContent_WhenUnequippingItems()
    {
        // Arrange
        var user = await _factory.CreateTestUser("testuser", "test@example.com", "Password123!");
        var token = GenerateJwtToken(user, new[] { "User" });
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var (headItem, _) = await AddHeadAndBodyItemsToDatabase();
        var userHeadItem = await AddUserItemToDatabase(user.Id, headItem.Id);

        // First equip an item
        using (var setupScope = _factory.Services.CreateScope())
        {
            var setupContext = setupScope.ServiceProvider.GetRequiredService<MultiplayerGameDbContext>();
            var setupCustomization = new UserCustomization
            {
                UserId = user.Id,
                EquippedHeadUserItemId = userHeadItem.Id,
                HeadColor = "#FF0000",
                BodyColor = "#00FF00",
                TailColor = "#0000FF",
                EyeColor = "#FFFF00",
                WingColor = "#FF00FF",
                HornColor = "#00FFFF",
                MarkingsColor = "#FFFFFF"
            };
            setupContext.UserCustomizations.Add(setupCustomization);
            await setupContext.SaveChangesAsync();
        }

        var dto = new UpdateEquippedUserItemsDto
        {
            EquippedHeadUserItemId = null,
            EquippedBodyUserItemId = null
        };

        // Act
        var response = await _client.PutAsJsonAsync("/v1/users/me/equipped-items", dto);

        // Assert
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        // Verify the update in database
        using var verifyScope = _factory.Services.CreateScope();
        var verifyContext = verifyScope.ServiceProvider.GetRequiredService<MultiplayerGameDbContext>();
        var verifyCustomization = await verifyContext.UserCustomizations.FirstOrDefaultAsync(uc => uc.UserId == user.Id);
        Assert.NotNull(verifyCustomization);
        Assert.Null(verifyCustomization.EquippedHeadUserItemId);
        Assert.Null(verifyCustomization.EquippedBodyUserItemId);
    }

    [Fact]
    public async Task UpdateEquippedUserItems_ShouldReturnUnauthorized_WhenNotAuthenticated()
    {
        // Arrange
        var dto = new UpdateEquippedUserItemsDto
        {
            EquippedHeadUserItemId = Guid.NewGuid(),
            EquippedBodyUserItemId = null
        };

        // Act
        var response = await _client.PutAsJsonAsync("/v1/users/me/equipped-items", dto);

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task UpdateEquippedUserItems_ShouldReturnNotFound_WhenUserItemDoesNotExist()
    {
        // Arrange
        var user = await _factory.CreateTestUser("testuser", "test@example.com", "Password123!");
        var token = GenerateJwtToken(user, new[] { "User" });
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var dto = new UpdateEquippedUserItemsDto
        {
            EquippedHeadUserItemId = Guid.NewGuid(),
            EquippedBodyUserItemId = null
        };

        // Act
        var response = await _client.PutAsJsonAsync("/v1/users/me/equipped-items", dto);

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task UpdateEquippedUserItems_ShouldReturnUnprocessableEntity_WhenEquippingWrongItemType()
    {
        // Arrange
        var user = await _factory.CreateTestUser("testuser", "test@example.com", "Password123!");
        var token = GenerateJwtToken(user, new[] { "User" });
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var (headItem, _) = await AddHeadAndBodyItemsToDatabase();
        var userHeadItem = await AddUserItemToDatabase(user.Id, headItem.Id);

        // Try to equip a head item as body item
        var dto = new UpdateEquippedUserItemsDto
        {
            EquippedHeadUserItemId = null,
            EquippedBodyUserItemId = userHeadItem.Id // This is a head item, not body
        };

        // Act
        var response = await _client.PutAsJsonAsync("/v1/users/me/equipped-items", dto);

        // Assert
        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
    }

    [Fact]
    public async Task UpdateEquippedUserItems_ShouldReturnForbidden_WhenTryingToEquipAnotherUsersItem()
    {
        // Arrange
        var user1 = await _factory.CreateTestUser("user1", "user1@example.com", "Password123!");
        var user2 = await _factory.CreateTestUser("user2", "user2@example.com", "Password123!");
        var token = GenerateJwtToken(user1, new[] { "User" });
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        await AddUserCustomizationToDatabase(user1.Id);
        var (headItem, _) = await AddHeadAndBodyItemsToDatabase();
        var user2HeadItem = await AddUserItemToDatabase(user2.Id, headItem.Id);

        var dto = new UpdateEquippedUserItemsDto
        {
            EquippedHeadUserItemId = user2HeadItem.Id,
            EquippedBodyUserItemId = null
        };

        // Act
        var response = await _client.PutAsJsonAsync("/v1/users/me/equipped-items", dto);

        // Assert
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    #endregion
}


