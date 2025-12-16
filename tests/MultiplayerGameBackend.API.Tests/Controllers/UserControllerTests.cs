using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using MultiplayerGameBackend.API.Tests.Helpers;
using MultiplayerGameBackend.Application.Users.Requests;
using MultiplayerGameBackend.Application.Users.Responses;
using MultiplayerGameBackend.Domain.Constants;
using MultiplayerGameBackend.Domain.Entities;
using MultiplayerGameBackend.Infrastructure.Persistence;

namespace MultiplayerGameBackend.API.Tests.Controllers;

public class UserControllerTests : IClassFixture<CustomWebApplicationFactory>, IAsyncLifetime
{
    private readonly CustomWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public UserControllerTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    public Task InitializeAsync() => Task.CompletedTask;

    public async Task DisposeAsync() => await _factory.ResetDatabase();

    #region Helper Methods

    private string GenerateJwtToken(User user, IEnumerable<string> roles) =>
        JwtTokenHelper.GenerateJwtToken(user, roles);

    private async Task EnsureRoleExists(string roleName)
    {
        using var scope = _factory.Services.CreateScope();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole<Guid>>>();
        
        if (!await roleManager.RoleExistsAsync(roleName))
        {
            await roleManager.CreateAsync(new IdentityRole<Guid> 
            { 
                Name = roleName, 
                NormalizedName = roleName.ToUpper() 
            });
        }
    }

    #endregion

    #region AssignUserRole Tests

    [Fact]
    public async Task AssignUserRole_ShouldReturnNoContent_WhenAdminAssignsRole()
    {
        // Arrange
        await EnsureRoleExists("Admin");
        await EnsureRoleExists("User");
        
        var admin = await _factory.CreateTestUser("admin", "admin@example.com", "Password123!", "Admin");
        var targetUser = await _factory.CreateTestUser("user", "user@example.com", "Password123!");

        var token = GenerateJwtToken(admin, new[] { "Admin" });
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var dto = new ModifyUserRoleDto { RoleName = "Admin" };

        // Act
        var response = await _client.PostAsJsonAsync($"/v1/users/{targetUser.Id}/roles", dto);

        // Assert
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        // Verify role was assigned
        using var scope = _factory.Services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<User>>();
        var user = await userManager.FindByIdAsync(targetUser.Id.ToString());
        var isInRole = await userManager.IsInRoleAsync(user!, "Admin");
        Assert.True(isInRole);
    }

    [Fact]
    public async Task AssignUserRole_ShouldReturnForbidden_WhenNonAdminTriesToAssignRole()
    {
        // Arrange
        await EnsureRoleExists("Admin");
        
        var regularUser = await _factory.CreateTestUser("user", "user@example.com", "Password123!");
        var targetUser = await _factory.CreateTestUser("target", "target@example.com", "Password123!");

        var token = GenerateJwtToken(regularUser, new[] { "User" });
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var dto = new ModifyUserRoleDto { RoleName = "Admin" };

        // Act
        var response = await _client.PostAsJsonAsync($"/v1/users/{targetUser.Id}/roles", dto);

        // Assert
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task AssignUserRole_ShouldReturnNotFound_WhenUserDoesNotExist()
    {
        // Arrange
        await EnsureRoleExists("Admin");
        
        var admin = await _factory.CreateTestUser("admin", "admin@example.com", "Password123!", "Admin");

        var token = GenerateJwtToken(admin, new[] { "Admin" });
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var dto = new ModifyUserRoleDto { RoleName = "Admin" };

        // Act
        var response = await _client.PostAsJsonAsync($"/v1/users/{Guid.NewGuid()}/roles", dto);

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task AssignUserRole_ShouldReturnBadRequest_WhenRoleNameIsInvalid()
    {
        // Arrange
        await EnsureRoleExists("Admin");
        
        var admin = await _factory.CreateTestUser("admin", "admin@example.com", "Password123!", "Admin");
        var targetUser = await _factory.CreateTestUser("user", "user@example.com", "Password123!");

        var token = GenerateJwtToken(admin, new[] { "Admin" });
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var dto = new ModifyUserRoleDto { RoleName = "" }; // Invalid: empty

        // Act
        var response = await _client.PostAsJsonAsync($"/v1/users/{targetUser.Id}/roles", dto);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task AssignUserRole_ShouldReturnUnauthorized_WhenNotAuthenticated()
    {
        // Arrange
        var targetUser = await _factory.CreateTestUser("user", "user@example.com", "Password123!");
        var dto = new ModifyUserRoleDto { RoleName = "Admin" };

        // Act
        var response = await _client.PostAsJsonAsync($"/v1/users/{targetUser.Id}/roles", dto);

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    #endregion

    #region UnassignUserRole Tests

    [Fact]
    public async Task UnassignUserRole_ShouldReturnNoContent_WhenAdminRemovesRole()
    {
        // Arrange
        await EnsureRoleExists("Admin");
        await EnsureRoleExists("User");
        
        var admin = await _factory.CreateTestUser("admin", "admin@example.com", "Password123!", "Admin");
        
        // Create a user with both User and Admin roles
        using var setupScope = _factory.Services.CreateScope();
        var setupUserManager = setupScope.ServiceProvider.GetRequiredService<UserManager<User>>();
        var targetUser = new User
        {
            UserName = "user",
            Email = "user@example.com",
            EmailConfirmed = true
        };
        await setupUserManager.CreateAsync(targetUser, "Password123!");
        await setupUserManager.AddToRoleAsync(targetUser, "User");
        await setupUserManager.AddToRoleAsync(targetUser, "Admin");

        var token = GenerateJwtToken(admin, new[] { "Admin" });
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var dto = new ModifyUserRoleDto { RoleName = "Admin" };

        // Act - DELETE with body content
        var request = new HttpRequestMessage(HttpMethod.Delete, $"/v1/users/{targetUser.Id}/roles")
        {
            Content = JsonContent.Create(dto)
        };
        var response = await _client.SendAsync(request);

        // Assert
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        using var scope = _factory.Services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<User>>();
        var user = await userManager.FindByIdAsync(targetUser.Id.ToString());
        var isInRole = await userManager.IsInRoleAsync(user!, "Admin");
        Assert.False(isInRole);
    }

    [Fact]
    public async Task UnassignUserRole_ShouldReturnForbidden_WhenNonAdminTriesToRemoveRole()
    {
        // Arrange
        await EnsureRoleExists("Admin");
        
        var regularUser = await _factory.CreateTestUser("user", "user@example.com", "Password123!");
        var targetUser = await _factory.CreateTestUser("target", "target@example.com", "Password123!", "Admin");

        var token = GenerateJwtToken(regularUser, new[] { "User" });
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var dto = new ModifyUserRoleDto { RoleName = "Admin" };
        var request = new HttpRequestMessage(HttpMethod.Delete, $"/v1/users/{targetUser.Id}/roles")
        {
            Content = JsonContent.Create(dto)
        };

        // Act
        var response = await _client.SendAsync(request);

        // Assert
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    #endregion

    #region GetCurrentUserGameInfo Tests

    [Fact]
    public async Task GetCurrentUserGameInfo_ShouldReturnUserInfo_WithoutOptionalData()
    {
        // Arrange
        var user = await _factory.CreateTestUser("gamer", "gamer@example.com", "Password123!");

        var token = GenerateJwtToken(user, new[] { "User" });
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Act
        var response = await _client.GetAsync("/v1/users/me/game-info");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var userInfo = await response.Content.ReadFromJsonAsync<UserGameInfoDto>();
        Assert.NotNull(userInfo);
        Assert.Equal(user.Id, userInfo.Id);
        Assert.Equal("gamer", userInfo.UserName);
        Assert.Null(userInfo.Customization);
        Assert.Null(userInfo.UserItems);
    }

    [Fact]
    public async Task GetCurrentUserGameInfo_ShouldReturnUserInfo_WithCustomization()
    {
        // Arrange
        var user = await _factory.CreateTestUser("gamer", "gamer@example.com", "Password123!");

        // Add customization
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<MultiplayerGameDbContext>();
        var customization = new UserCustomization
        {
            UserId = user.Id,
            HeadColor = "#FF0000",
            BodyColor = "#00FF00",
            TailColor = "#0000FF",
            EyeColor = "#FFFF00",
            WingColor = "#FF00FF",
            HornColor = "#00FFFF",
            MarkingsColor = "#FFFFFF"
        };
        context.UserCustomizations.Add(customization);
        await context.SaveChangesAsync();

        var token = GenerateJwtToken(user, new[] { "User" });
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Act
        var response = await _client.GetAsync("/v1/users/me/game-info?includeCustomization=true");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var userInfo = await response.Content.ReadFromJsonAsync<UserGameInfoDto>();
        Assert.NotNull(userInfo);
        Assert.NotNull(userInfo.Customization);
        Assert.Equal("#FF0000", userInfo.Customization.HeadColor);
    }

    [Fact]
    public async Task GetCurrentUserGameInfo_ShouldReturnUserInfo_WithUserItems()
    {
        // Arrange
        var user = await _factory.CreateTestUser("gamer", "gamer@example.com", "Password123!");

        // Add user items
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<MultiplayerGameDbContext>();
        
        var item = new Item
        {
            Name = "Cool Sword",
            Description = "A very cool sword",
            Type = ItemTypes.EquippableOnBody,
            ThumbnailUrl = "sword.png"
        };
        context.Items.Add(item);
        await context.SaveChangesAsync();

        var userItem = new UserItem { UserId = user.Id, ItemId = item.Id };
        context.UserItems.Add(userItem);
        await context.SaveChangesAsync();

        var token = GenerateJwtToken(user, new[] { "User" });
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Act
        var response = await _client.GetAsync("/v1/users/me/game-info?includeUserItems=true");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var userInfo = await response.Content.ReadFromJsonAsync<UserGameInfoDto>();
        Assert.NotNull(userInfo);
        Assert.NotNull(userInfo.UserItems);
        Assert.NotEmpty(userInfo.UserItems);
        Assert.Equal("Cool Sword", userInfo.UserItems[0].Item.Name);
    }

    [Fact]
    public async Task GetCurrentUserGameInfo_ShouldReturnUnauthorized_WhenNotAuthenticated()
    {
        // Act
        var response = await _client.GetAsync("/v1/users/me/game-info");

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    #endregion

    #region UpdateUserAppearance Tests

    [Fact]
    public async Task UpdateUserAppearance_ShouldReturnNoContent_WhenValid()
    {
        // Arrange
        var user = await _factory.CreateTestUser("gamer", "gamer@example.com", "Password123!");

        var token = GenerateJwtToken(user, new[] { "User" });
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var dto = new UpdateUserAppearanceDto
        {
            HeadColor = "#FF0000",
            BodyColor = "#00FF00",
            TailColor = "#0000FF",
            EyeColor = "#FFFF00",
            WingColor = "#FF00FF",
            HornColor = "#00FFFF",
            MarkingsColor = "#FFFFFF"
        };

        // Act
        var response = await _client.PutAsJsonAsync("/v1/users/me/appearance", dto);

        // Assert
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        // Verify customization was saved
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<MultiplayerGameDbContext>();
        var customization = context.UserCustomizations.FirstOrDefault(c => c.UserId == user.Id);
        Assert.NotNull(customization);
        Assert.Equal("#FF0000", customization.HeadColor);
    }

    [Fact]
    public async Task UpdateUserAppearance_ShouldUpdateExisting_WhenCustomizationExists()
    {
        // Arrange
        var user = await _factory.CreateTestUser("gamer", "gamer@example.com", "Password123!");

        // Create initial customization
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<MultiplayerGameDbContext>();
        var initialCustomization = new UserCustomization
        {
            UserId = user.Id,
            HeadColor = "#000000",
            BodyColor = "#000000",
            TailColor = "#000000",
            EyeColor = "#000000",
            WingColor = "#000000",
            HornColor = "#000000",
            MarkingsColor = "#000000"
        };
        context.UserCustomizations.Add(initialCustomization);
        await context.SaveChangesAsync();

        var token = GenerateJwtToken(user, new[] { "User" });
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var dto = new UpdateUserAppearanceDto
        {
            HeadColor = "#FFFFFF",
            BodyColor = "#FFFFFF",
            TailColor = "#FFFFFF",
            EyeColor = "#FFFFFF",
            WingColor = "#FFFFFF",
            HornColor = "#FFFFFF",
            MarkingsColor = "#FFFFFF"
        };

        // Act
        var response = await _client.PutAsJsonAsync("/v1/users/me/appearance", dto);

        // Assert
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        // Verify customization was updated
        using var verifyScope = _factory.Services.CreateScope();
        var verifyContext = verifyScope.ServiceProvider.GetRequiredService<MultiplayerGameDbContext>();
        var customization = verifyContext.UserCustomizations.FirstOrDefault(c => c.UserId == user.Id);
        Assert.NotNull(customization);
        Assert.Equal("#FFFFFF", customization.HeadColor);
    }

    [Fact]
    public async Task UpdateUserAppearance_ShouldReturnUnauthorized_WhenNotAuthenticated()
    {
        // Arrange
        var dto = new UpdateUserAppearanceDto
        {
            HeadColor = "#FF0000",
            BodyColor = "#00FF00",
            TailColor = "#0000FF",
            EyeColor = "#FFFF00",
            WingColor = "#FF00FF",
            HornColor = "#00FFFF",
            MarkingsColor = "#FFFFFF"
        };

        // Act
        var response = await _client.PutAsJsonAsync("/v1/users/me/appearance", dto);

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    #endregion

    #region DeleteProfilePicture Tests

    [Fact]
    public async Task DeleteProfilePicture_ShouldReturnNoContent_WhenPictureExists()
    {
        // Arrange
        var user = await _factory.CreateTestUser("gamer", "gamer@example.com", "Password123!");

        // Set profile picture
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<MultiplayerGameDbContext>();
        var dbUser = await context.Users.FindAsync(user.Id);
        dbUser!.ProfilePictureUrl = "/uploads/profiles/test.jpg";
        await context.SaveChangesAsync();

        var token = GenerateJwtToken(user, new[] { "User" });
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Act
        var response = await _client.DeleteAsync("/v1/users/me/profile-picture");

        // Assert
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        // Verify profile picture was removed
        using var verifyScope = _factory.Services.CreateScope();
        var verifyContext = verifyScope.ServiceProvider.GetRequiredService<MultiplayerGameDbContext>();
        var updatedUser = await verifyContext.Users.FindAsync(user.Id);
        Assert.Null(updatedUser!.ProfilePictureUrl);
    }

    [Fact]
    public async Task DeleteProfilePicture_ShouldReturnNotFound_WhenNoPictureExists()
    {
        // Arrange
        var user = await _factory.CreateTestUser("gamer", "gamer@example.com", "Password123!");

        var token = GenerateJwtToken(user, new[] { "User" });
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Act
        var response = await _client.DeleteAsync("/v1/users/me/profile-picture");

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task DeleteProfilePicture_ShouldReturnUnauthorized_WhenNotAuthenticated()
    {
        // Act
        var response = await _client.DeleteAsync("/v1/users/me/profile-picture");

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    #endregion
}

