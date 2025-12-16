using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.Extensions.DependencyInjection;
using MultiplayerGameBackend.API.Tests.Helpers;
using MultiplayerGameBackend.Application.Identity.Requests;
using MultiplayerGameBackend.Application.Identity.Responses;
using MultiplayerGameBackend.Domain.Constants;
using MultiplayerGameBackend.Domain.Entities;
using MultiplayerGameBackend.Infrastructure.Persistence;

namespace MultiplayerGameBackend.API.Tests.Controllers;

public class IdentityControllerTests : IClassFixture<CustomWebApplicationFactory>, IAsyncLifetime
{
    private readonly CustomWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public IdentityControllerTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    public Task InitializeAsync() => Task.CompletedTask;

    public async Task DisposeAsync() => await _factory.ResetDatabase();

    #region Helper Methods

    private string GenerateJwtToken(User user, IEnumerable<string> roles) =>
        JwtTokenHelper.GenerateJwtToken(user, roles);

    #endregion

    #region Register Tests

    [Fact]
    public async Task Register_ShouldReturnBadRequest_WhenEmailIsInvalid()
    {
        // Arrange
        var dto = new RegisterDto
        {
            UserName = "testuser",
            Email = "invalid-email",
            Password = "Password123!"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/v1/identity/register", dto);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Register_ShouldReturnConflict_WhenUserAlreadyExists()
    {
        // Arrange
        await _factory.CreateTestUser("testuser", "test@example.com", "Password123!");

        var dto = new RegisterDto
        {
            UserName = "testuser",
            Email = "test@example.com",
            Password = "Password123!"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/v1/identity/register", dto);

        // Assert
        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    #endregion

    #region Login Tests

    [Fact]
    public async Task Login_ShouldReturnBadRequest_WhenClientTypeHeaderMissing()
    {
        // Arrange
        await _factory.CreateTestUser("testuser", "test@example.com", "Password123!");
        
        var dto = new LoginDto
        {
            UserName = "testuser",
            Password = "Password123!"
        };

        // Act (no X-Client-Type header)
        var response = await _client.PostAsJsonAsync("/v1/identity/login", dto);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }


    #endregion

    #region Logout Tests

    [Fact]
    public async Task Logout_ShouldReturnNoContent_EvenWithInvalidToken()
    {
        // Arrange
        _client.DefaultRequestHeaders.Add("X-Client-Type", ClientTypes.Game);

        // Act
        var response = await _client.PostAsJsonAsync("/v1/identity/logout", "\"invalid-token\"");

        // Assert
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    [Fact]
    public async Task Logout_ShouldReturnBadRequest_WhenClientTypeHeaderMissing()
    {
        // Act
        var response = await _client.PostAsJsonAsync("/v1/identity/logout", "\"some-token\"");

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    #endregion

    #region ChangePassword Tests

    [Fact]
    public async Task ChangePassword_ShouldReturnUnauthorized_WhenNotAuthenticated()
    {
        // Arrange
        var dto = new ChangePasswordDto
        {
            CurrentPassword = "Password123!",
            NewPassword = "NewPassword123!",
            RefreshToken = "some-token"
        };

        _client.DefaultRequestHeaders.Add("X-Client-Type", ClientTypes.Game);

        // Act
        var response = await _client.PostAsJsonAsync("/v1/identity/change-password", dto);

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task ChangePassword_ShouldReturnBadRequest_WhenClientTypeHeaderMissing()
    {
        // Arrange
        var user = await _factory.CreateTestUser("testuser", "test@example.com", "Password123!");
        var token = GenerateJwtToken(user, new[] { "User" });
        
        var dto = new ChangePasswordDto
        {
            CurrentPassword = "Password123!",
            NewPassword = "NewPassword123!",
            RefreshToken = "some-token"
        };

        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Act
        var response = await _client.PostAsJsonAsync("/v1/identity/change-password", dto);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    #endregion

    #region DeleteAccount Tests

    [Fact]
    public async Task DeleteAccount_ShouldReturnUnauthorized_WhenNotAuthenticated()
    {
        // Arrange
        var dto = new DeleteAccountDto
        {
            Password = "Password123!"
        };

        _client.DefaultRequestHeaders.Add("X-Client-Type", ClientTypes.Game);

        // Act
        var response = await _client.SendAsync(new HttpRequestMessage(HttpMethod.Delete, "/v1/identity/delete-account")
        {
            Content = JsonContent.Create(dto)
        });

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task DeleteAccount_ShouldReturnBadRequest_WhenClientTypeHeaderMissing()
    {
        // Arrange
        var user = await _factory.CreateTestUser("testuser", "test@example.com", "Password123!");
        var token = GenerateJwtToken(user, new[] { "User" });
        
        var dto = new DeleteAccountDto
        {
            Password = "Password123!"
        };

        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Act
        var response = await _client.SendAsync(new HttpRequestMessage(HttpMethod.Delete, "/v1/identity/delete-account")
        {
            Content = JsonContent.Create(dto)
        });

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    #endregion

    #region ForgotPassword Tests

    [Fact]
    public async Task ForgotPassword_ShouldReturnOk_WhenEmailExists()
    {
        // Arrange
        await _factory.CreateTestUser("testuser", "test@example.com", "Password123!");
        
        var dto = new ForgotPasswordDto
        {
            Email = "test@example.com"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/v1/identity/forgot-password", dto);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var content = await response.Content.ReadFromJsonAsync<Dictionary<string, string>>();
        Assert.NotNull(content);
        Assert.Contains("message", content.Keys);
    }

    [Fact]
    public async Task ForgotPassword_ShouldReturnOk_EvenWhenEmailDoesNotExist()
    {
        // Arrange
        var dto = new ForgotPasswordDto
        {
            Email = "nonexistent@example.com"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/v1/identity/forgot-password", dto);

        // Assert
        // Should return OK to prevent email enumeration
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    #endregion

    #region Refresh Tests


    [Fact]
    public async Task Refresh_ShouldReturnBadRequest_WhenClientTypeHeaderMissing()
    {
        // Act
        var response = await _client.PostAsJsonAsync("/v1/identity/refresh", "\"some-token\"");

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Refresh_ShouldReturnBadRequest_WhenRefreshTokenMissing()
    {
        // Arrange
        _client.DefaultRequestHeaders.Add("X-Client-Type", ClientTypes.Game);

        // Act
        var response = await _client.PostAsJsonAsync("/v1/identity/refresh", "\"\"");

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    #endregion
}

