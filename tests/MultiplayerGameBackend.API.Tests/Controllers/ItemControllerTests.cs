using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using MultiplayerGameBackend.API.Tests.TestHelpers;
using MultiplayerGameBackend.Application.Items.Requests;
using MultiplayerGameBackend.Application.Items.Responses;
using MultiplayerGameBackend.Domain.Constants;
using MultiplayerGameBackend.Domain.Entities;

namespace MultiplayerGameBackend.API.Tests.Controllers;

public class ItemControllerTests : IClassFixture<CustomWebApplicationFactory>, IAsyncLifetime
{
    private readonly CustomWebApplicationFactory webAppFactory;
    private readonly HttpClient httpClient;

    public ItemControllerTests(CustomWebApplicationFactory factory)
    {
        webAppFactory = factory;
        httpClient = factory.CreateClient();
    }

    public Task InitializeAsync() => Task.CompletedTask;

    public async Task DisposeAsync() => await webAppFactory.ResetDatabase();

    #region Helper Methods

    private string GenerateJwtToken(User user, IEnumerable<string> roles) =>
        JwtTokenHelper.GenerateJwtToken(user, roles);

    private async Task<Item> AddItemToDatabase(string name, string type) =>
        await TestDatabaseHelper.AddItemToDatabase(webAppFactory.Services, name, type);

    #endregion

    #region GetById Tests

    [Fact]
    public async Task GetById_ShouldReturnOk_WhenItemExists()
    {
        // Arrange
        var user = await webAppFactory.CreateTestUser("testuser", "test@example.com", "Password123!", "User");
        var token = GenerateJwtToken(user, new[] { "User" });
        httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var item = await AddItemToDatabase("Test Item", ItemTypes.Consumable);

        // Act
        var response = await httpClient.GetAsync($"/v1/items/{item.Id}");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var returnedItem = await response.Content.ReadFromJsonAsync<ReadItemDto>();
        Assert.NotNull(returnedItem);
        Assert.Equal(item.Id, returnedItem.Id);
        Assert.Equal("Test Item", returnedItem.Name);
    }

    [Fact]
    public async Task GetById_ShouldReturnNotFound_WhenItemDoesNotExist()
    {
        // Arrange
        var user = await webAppFactory.CreateTestUser("testuser", "test@example.com", "Password123!", "User");
        var token = GenerateJwtToken(user, new[] { "User" });
        httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Act
        var response = await httpClient.GetAsync("/v1/items/99999");

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetById_ShouldReturnUnauthorized_WhenNotAuthenticated()
    {
        // Arrange
        var item = await AddItemToDatabase("Test Item", ItemTypes.Consumable);

        // Act
        var response = await httpClient.GetAsync($"/v1/items/{item.Id}");

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    #endregion

    #region GetAll Tests

    [Fact]
    public async Task GetAll_ShouldReturnOkWithItems_WhenItemsExist()
    {
        // Arrange
        var user = await webAppFactory.CreateTestUser("testuser", "test@example.com", "Password123!", "User");
        var token = GenerateJwtToken(user, new[] { "User" });
        httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        await AddItemToDatabase("Item 1", ItemTypes.Consumable);
        await AddItemToDatabase("Item 2", ItemTypes.EquippableOnHead);

        // Act
        var response = await httpClient.GetAsync("/v1/items");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var items = await response.Content.ReadFromJsonAsync<List<ReadItemDto>>();
        Assert.NotNull(items);
        Assert.Equal(2, items.Count);
    }

    [Fact]
    public async Task GetAll_ShouldReturnEmptyList_WhenNoItems()
    {
        // Arrange
        var user = await webAppFactory.CreateTestUser("testuser", "test@example.com", "Password123!", "User");
        var token = GenerateJwtToken(user, new[] { "User" });
        httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Act
        var response = await httpClient.GetAsync("/v1/items");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var items = await response.Content.ReadFromJsonAsync<List<ReadItemDto>>();
        Assert.NotNull(items);
        Assert.Empty(items);
    }

    [Fact]
    public async Task GetAll_ShouldReturnUnauthorized_WhenNotAuthenticated()
    {
        // Act
        var response = await httpClient.GetAsync("/v1/items");

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    #endregion

    #region Create Tests

    [Fact]
    public async Task Create_ShouldReturnCreated_WhenValidDataAndAdmin()
    {
        // Arrange
        var admin = await webAppFactory.CreateTestUser("admin", "admin@example.com", "Password123!", "Admin");
        var token = GenerateJwtToken(admin, new[] { "Admin" });
        httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var dto = new CreateUpdateItemDto
        {
            Name = "New Item",
            Description = "A new test item",
            Type = ItemTypes.Consumable,
            ThumbnailUrl = "new-item.png"
        };

        // Act
        var response = await httpClient.PostAsJsonAsync("/v1/items", dto);

        // Assert
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.NotNull(response.Headers.Location);
    }

    [Fact]
    public async Task Create_ShouldReturnForbidden_WhenNotAdmin()
    {
        // Arrange
        var user = await webAppFactory.CreateTestUser("user", "user@example.com", "Password123!", "User");
        var token = GenerateJwtToken(user, new[] { "User" });
        httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var dto = new CreateUpdateItemDto
        {
            Name = "New Item",
            Description = "A new test item",
            Type = ItemTypes.Consumable,
            ThumbnailUrl = "new-item.png"
        };

        // Act
        var response = await httpClient.PostAsJsonAsync("/v1/items", dto);

        // Assert
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Create_ShouldReturnBadRequest_WhenInvalidData()
    {
        // Arrange
        var admin = await webAppFactory.CreateTestUser("admin", "admin@example.com", "Password123!", "Admin");
        var token = GenerateJwtToken(admin, new[] { "Admin" });
        httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var dto = new CreateUpdateItemDto
        {
            Name = "", // Invalid: empty name
            Description = "A new test item",
            Type = ItemTypes.Consumable,
            ThumbnailUrl = "new-item.png"
        };

        // Act
        var response = await httpClient.PostAsJsonAsync("/v1/items", dto);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Create_ShouldReturnConflict_WhenDuplicateName()
    {
        // Arrange
        var admin = await webAppFactory.CreateTestUser("admin", "admin@example.com", "Password123!", "Admin");
        var token = GenerateJwtToken(admin, new[] { "Admin" });
        httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        await AddItemToDatabase("Existing Item", ItemTypes.Consumable);

        var dto = new CreateUpdateItemDto
        {
            Name = "Existing Item",
            Description = "Duplicate item",
            Type = ItemTypes.Consumable,
            ThumbnailUrl = "duplicate.png"
        };

        // Act
        var response = await httpClient.PostAsJsonAsync("/v1/items", dto);

        // Assert
        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task Create_ShouldReturnUnauthorized_WhenNotAuthenticated()
    {
        // Arrange
        var dto = new CreateUpdateItemDto
        {
            Name = "New Item",
            Description = "A new test item",
            Type = ItemTypes.Consumable,
            ThumbnailUrl = "new-item.png"
        };

        // Act
        var response = await httpClient.PostAsJsonAsync("/v1/items", dto);

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    #endregion

    #region Update Tests

    [Fact]
    public async Task Update_ShouldReturnNoContent_WhenValidDataAndAdmin()
    {
        // Arrange
        var admin = await webAppFactory.CreateTestUser("admin", "admin@example.com", "Password123!", "Admin");
        var token = GenerateJwtToken(admin, new[] { "Admin" });
        httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var item = await AddItemToDatabase("Original Item", ItemTypes.Consumable);

        var dto = new CreateUpdateItemDto
        {
            Name = "Updated Item",
            Description = "Updated description",
            Type = ItemTypes.Consumable,
            ThumbnailUrl = "updated.png"
        };

        // Act
        var response = await httpClient.PatchAsJsonAsync($"/v1/items/{item.Id}", dto);

        // Assert
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        // Verify the update
        httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var getResponse = await httpClient.GetAsync($"/v1/items/{item.Id}");
        var updatedItem = await getResponse.Content.ReadFromJsonAsync<ReadItemDto>();
        Assert.Equal("Updated Item", updatedItem!.Name);
    }

    [Fact]
    public async Task Update_ShouldReturnNotFound_WhenItemDoesNotExist()
    {
        // Arrange
        var admin = await webAppFactory.CreateTestUser("admin", "admin@example.com", "Password123!", "Admin");
        var token = GenerateJwtToken(admin, new[] { "Admin" });
        httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var dto = new CreateUpdateItemDto
        {
            Name = "Updated Item",
            Description = "Updated description",
            Type = ItemTypes.Consumable,
            ThumbnailUrl = "updated.png"
        };

        // Act
        var response = await httpClient.PatchAsJsonAsync("/v1/items/99999", dto);

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Update_ShouldReturnForbidden_WhenNotAdmin()
    {
        // Arrange
        var user = await webAppFactory.CreateTestUser("user", "user@example.com", "Password123!", "User");
        var token = GenerateJwtToken(user, new[] { "User" });
        httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var item = await AddItemToDatabase("Original Item", ItemTypes.Consumable);

        var dto = new CreateUpdateItemDto
        {
            Name = "Updated Item",
            Description = "Updated description",
            Type = ItemTypes.Consumable,
            ThumbnailUrl = "updated.png"
        };

        // Act
        var response = await httpClient.PatchAsJsonAsync($"/v1/items/{item.Id}", dto);

        // Assert
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Update_ShouldReturnBadRequest_WhenInvalidData()
    {
        // Arrange
        var admin = await webAppFactory.CreateTestUser("admin", "admin@example.com", "Password123!", "Admin");
        var token = GenerateJwtToken(admin, new[] { "Admin" });
        httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var item = await AddItemToDatabase("Original Item", ItemTypes.Consumable);

        var dto = new CreateUpdateItemDto
        {
            Name = "", // Invalid: empty name
            Description = "Updated description",
            Type = ItemTypes.Consumable,
            ThumbnailUrl = "updated.png"
        };

        // Act
        var response = await httpClient.PatchAsJsonAsync($"/v1/items/{item.Id}", dto);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    #endregion

    #region Delete Tests

    [Fact]
    public async Task Delete_ShouldReturnNoContent_WhenItemExistsAndAdmin()
    {
        // Arrange
        var admin = await webAppFactory.CreateTestUser("admin", "admin@example.com", "Password123!", "Admin");
        var token = GenerateJwtToken(admin, new[] { "Admin" });
        httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var item = await AddItemToDatabase("Item to Delete", ItemTypes.Consumable);

        // Act
        var response = await httpClient.DeleteAsync($"/v1/items/{item.Id}");

        // Assert
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        // Verify deletion
        var getResponse = await httpClient.GetAsync($"/v1/items/{item.Id}");
        Assert.Equal(HttpStatusCode.NotFound, getResponse.StatusCode);
    }

    [Fact]
    public async Task Delete_ShouldReturnNotFound_WhenItemDoesNotExist()
    {
        // Arrange
        var admin = await webAppFactory.CreateTestUser("admin", "admin@example.com", "Password123!", "Admin");
        var token = GenerateJwtToken(admin, new[] { "Admin" });
        httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Act
        var response = await httpClient.DeleteAsync("/v1/items/99999");

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Delete_ShouldReturnForbidden_WhenNotAdmin()
    {
        // Arrange
        var user = await webAppFactory.CreateTestUser("user", "user@example.com", "Password123!", "User");
        var token = GenerateJwtToken(user, new[] { "User" });
        httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var item = await AddItemToDatabase("Item to Delete", ItemTypes.Consumable);

        // Act
        var response = await httpClient.DeleteAsync($"/v1/items/{item.Id}");

        // Assert
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Delete_ShouldReturnUnauthorized_WhenNotAuthenticated()
    {
        // Arrange
        var item = await AddItemToDatabase("Item to Delete", ItemTypes.Consumable);

        // Act
        var response = await httpClient.DeleteAsync($"/v1/items/{item.Id}");

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    #endregion
}

