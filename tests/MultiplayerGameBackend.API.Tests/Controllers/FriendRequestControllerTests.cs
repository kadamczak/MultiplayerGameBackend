using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using MultiplayerGameBackend.API.Tests.TestHelpers;
using MultiplayerGameBackend.Application.FriendRequests.Requests;
using MultiplayerGameBackend.Application.FriendRequests.Responses;
using MultiplayerGameBackend.Application.Friends.Responses;
using MultiplayerGameBackend.Domain.Constants;
using MultiplayerGameBackend.Domain.Entities;
using MultiplayerGameBackend.Infrastructure.Persistence;

namespace MultiplayerGameBackend.API.Tests.Controllers;

public class FriendRequestControllerTests : IClassFixture<CustomWebApplicationFactory>, IAsyncLifetime
{
    private readonly CustomWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public FriendRequestControllerTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    public Task InitializeAsync() => Task.CompletedTask;

    public async Task DisposeAsync() => await _factory.ResetDatabase();

    #region Helper Methods

    private string GenerateJwtToken(User user, IEnumerable<string> roles) =>
        JwtTokenHelper.GenerateJwtToken(user, roles);

    private async Task<FriendRequest> AddFriendRequestToDatabase(
        Guid requesterId,
        Guid receiverId,
        string status = FriendRequestStatuses.Pending,
        DateTime? respondedAt = null) =>
        await TestDatabaseHelper.AddFriendRequestToDatabase(
            _factory.Services,
            requesterId,
            receiverId,
            status,
            respondedAt);

    #endregion

    #region SendFriendRequest Tests

    [Fact]
    public async Task SendFriendRequest_ShouldReturnCreated_WhenValidRequest()
    {
        // Arrange
        var sender = await _factory.CreateTestUser("sender", "sender@example.com", "Password123!");
        var receiver = await _factory.CreateTestUser("receiver", "receiver@example.com", "Password123!");

        var token = GenerateJwtToken(sender, new[] { "User" });
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var dto = new SendFriendRequestDto { ReceiverId = receiver.Id };

        // Act
        var response = await _client.PostAsJsonAsync("/v1/friends/requests", dto);

        // Assert
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        // Verify the friend request was created
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<MultiplayerGameDbContext>();
        var friendRequest = await context.FriendRequests
            .FirstOrDefaultAsync(fr => fr.RequesterId == sender.Id && fr.ReceiverId == receiver.Id);
        Assert.NotNull(friendRequest);
        Assert.Equal(FriendRequestStatuses.Pending, friendRequest.Status);
    }

    [Fact]
    public async Task SendFriendRequest_ShouldReturnBadRequest_WhenSendingToSelf()
    {
        // Arrange
        var user = await _factory.CreateTestUser("user", "user@example.com", "Password123!");

        var token = GenerateJwtToken(user, new[] { "User" });
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var dto = new SendFriendRequestDto { ReceiverId = user.Id };

        // Act
        var response = await _client.PostAsJsonAsync("/v1/friends/requests", dto);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task SendFriendRequest_ShouldReturnNotFound_WhenReceiverDoesNotExist()
    {
        // Arrange
        var sender = await _factory.CreateTestUser("sender", "sender@example.com", "Password123!");

        var token = GenerateJwtToken(sender, new[] { "User" });
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var dto = new SendFriendRequestDto { ReceiverId = Guid.NewGuid() };

        // Act
        var response = await _client.PostAsJsonAsync("/v1/friends/requests", dto);

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task SendFriendRequest_ShouldReturnConflict_WhenAlreadyFriends()
    {
        // Arrange
        var user1 = await _factory.CreateTestUser("user1", "user1@example.com", "Password123!");
        var user2 = await _factory.CreateTestUser("user2", "user2@example.com", "Password123!");

        // Create an accepted friend request
        await AddFriendRequestToDatabase(user1.Id, user2.Id, FriendRequestStatuses.Accepted, DateTime.UtcNow);

        var token = GenerateJwtToken(user1, new[] { "User" });
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var dto = new SendFriendRequestDto { ReceiverId = user2.Id };

        // Act
        var response = await _client.PostAsJsonAsync("/v1/friends/requests", dto);

        // Assert
        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task SendFriendRequest_ShouldReturnConflict_WhenPendingRequestExists()
    {
        // Arrange
        var sender = await _factory.CreateTestUser("sender", "sender@example.com", "Password123!");
        var receiver = await _factory.CreateTestUser("receiver", "receiver@example.com", "Password123!");

        // Create a pending friend request
        await AddFriendRequestToDatabase(sender.Id, receiver.Id);

        var token = GenerateJwtToken(sender, new[] { "User" });
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var dto = new SendFriendRequestDto { ReceiverId = receiver.Id };

        // Act
        var response = await _client.PostAsJsonAsync("/v1/friends/requests", dto);

        // Assert
        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task SendFriendRequest_ShouldAutoAccept_WhenReceiverAlreadySentRequest()
    {
        // Arrange
        var user1 = await _factory.CreateTestUser("user1", "user1@example.com", "Password123!");
        var user2 = await _factory.CreateTestUser("user2", "user2@example.com", "Password123!");

        // User2 already sent a request to User1
        var existingRequest = await AddFriendRequestToDatabase(user2.Id, user1.Id);

        var token = GenerateJwtToken(user1, new[] { "User" });
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var dto = new SendFriendRequestDto { ReceiverId = user2.Id };

        // Act
        var response = await _client.PostAsJsonAsync("/v1/friends/requests", dto);

        // Assert
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        // Verify the existing request was accepted
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<MultiplayerGameDbContext>();
        var friendRequest = await context.FriendRequests.FindAsync(existingRequest.Id);
        Assert.NotNull(friendRequest);
        Assert.Equal(FriendRequestStatuses.Accepted, friendRequest.Status);
        Assert.NotNull(friendRequest.RespondedAt);
    }

    [Fact]
    public async Task SendFriendRequest_ShouldReturnUnauthorized_WhenNotAuthenticated()
    {
        // Arrange
        var receiver = await _factory.CreateTestUser("receiver", "receiver@example.com", "Password123!");
        var dto = new SendFriendRequestDto { ReceiverId = receiver.Id };

        // Act
        var response = await _client.PostAsJsonAsync("/v1/friends/requests", dto);

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    #endregion

    #region AcceptFriendRequest Tests

    [Fact]
    public async Task AcceptFriendRequest_ShouldReturnNoContent_WhenValidRequest()
    {
        // Arrange
        var requester = await _factory.CreateTestUser("requester", "requester@example.com", "Password123!");
        var receiver = await _factory.CreateTestUser("receiver", "receiver@example.com", "Password123!");

        var friendRequest = await AddFriendRequestToDatabase(requester.Id, receiver.Id);

        var token = GenerateJwtToken(receiver, new[] { "User" });
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Act
        var response = await _client.PostAsync($"/v1/friends/requests/{friendRequest.Id}/accept", null);

        // Assert
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        // Verify the friend request was accepted
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<MultiplayerGameDbContext>();
        var updatedRequest = await context.FriendRequests.FindAsync(friendRequest.Id);
        Assert.NotNull(updatedRequest);
        Assert.Equal(FriendRequestStatuses.Accepted, updatedRequest.Status);
        Assert.NotNull(updatedRequest.RespondedAt);
    }

    [Fact]
    public async Task AcceptFriendRequest_ShouldReturnNotFound_WhenRequestDoesNotExist()
    {
        // Arrange
        var user = await _factory.CreateTestUser("user", "user@example.com", "Password123!");

        var token = GenerateJwtToken(user, new[] { "User" });
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Act
        var response = await _client.PostAsync($"/v1/friends/requests/{Guid.NewGuid()}/accept", null);

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task AcceptFriendRequest_ShouldReturnForbidden_WhenNotTheReceiver()
    {
        // Arrange
        var requester = await _factory.CreateTestUser("requester", "requester@example.com", "Password123!");
        var receiver = await _factory.CreateTestUser("receiver", "receiver@example.com", "Password123!");
        var otherUser = await _factory.CreateTestUser("other", "other@example.com", "Password123!");

        var friendRequest = await AddFriendRequestToDatabase(requester.Id, receiver.Id);

        var token = GenerateJwtToken(otherUser, new[] { "User" });
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Act
        var response = await _client.PostAsync($"/v1/friends/requests/{friendRequest.Id}/accept", null);

        // Assert
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task AcceptFriendRequest_ShouldReturnBadRequest_WhenRequestAlreadyAccepted()
    {
        // Arrange
        var requester = await _factory.CreateTestUser("requester", "requester@example.com", "Password123!");
        var receiver = await _factory.CreateTestUser("receiver", "receiver@example.com", "Password123!");

        var friendRequest = await AddFriendRequestToDatabase(
            requester.Id,
            receiver.Id,
            FriendRequestStatuses.Accepted,
            DateTime.UtcNow);

        var token = GenerateJwtToken(receiver, new[] { "User" });
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Act
        var response = await _client.PostAsync($"/v1/friends/requests/{friendRequest.Id}/accept", null);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task AcceptFriendRequest_ShouldReturnUnauthorized_WhenNotAuthenticated()
    {
        // Arrange
        var requester = await _factory.CreateTestUser("requester", "requester@example.com", "Password123!");
        var receiver = await _factory.CreateTestUser("receiver", "receiver@example.com", "Password123!");

        var friendRequest = await AddFriendRequestToDatabase(requester.Id, receiver.Id);

        // Act
        var response = await _client.PostAsync($"/v1/friends/requests/{friendRequest.Id}/accept", null);

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    #endregion

    #region RejectFriendRequest Tests

    [Fact]
    public async Task RejectFriendRequest_ShouldReturnNoContent_WhenValidRequest()
    {
        // Arrange
        var requester = await _factory.CreateTestUser("requester", "requester@example.com", "Password123!");
        var receiver = await _factory.CreateTestUser("receiver", "receiver@example.com", "Password123!");

        var friendRequest = await AddFriendRequestToDatabase(requester.Id, receiver.Id);

        var token = GenerateJwtToken(receiver, new[] { "User" });
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Act
        var response = await _client.PostAsync($"/v1/friends/requests/{friendRequest.Id}/reject", null);

        // Assert
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        // Verify the friend request was rejected
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<MultiplayerGameDbContext>();
        var updatedRequest = await context.FriendRequests.FindAsync(friendRequest.Id);
        Assert.NotNull(updatedRequest);
        Assert.Equal(FriendRequestStatuses.Rejected, updatedRequest.Status);
        Assert.NotNull(updatedRequest.RespondedAt);
    }

    [Fact]
    public async Task RejectFriendRequest_ShouldReturnNotFound_WhenRequestDoesNotExist()
    {
        // Arrange
        var user = await _factory.CreateTestUser("user", "user@example.com", "Password123!");

        var token = GenerateJwtToken(user, new[] { "User" });
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Act
        var response = await _client.PostAsync($"/v1/friends/requests/{Guid.NewGuid()}/reject", null);

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task RejectFriendRequest_ShouldReturnForbidden_WhenNotTheReceiver()
    {
        // Arrange
        var requester = await _factory.CreateTestUser("requester", "requester@example.com", "Password123!");
        var receiver = await _factory.CreateTestUser("receiver", "receiver@example.com", "Password123!");
        var otherUser = await _factory.CreateTestUser("other", "other@example.com", "Password123!");

        var friendRequest = await AddFriendRequestToDatabase(requester.Id, receiver.Id);

        var token = GenerateJwtToken(otherUser, new[] { "User" });
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Act
        var response = await _client.PostAsync($"/v1/friends/requests/{friendRequest.Id}/reject", null);

        // Assert
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task RejectFriendRequest_ShouldReturnUnauthorized_WhenNotAuthenticated()
    {
        // Arrange
        var requester = await _factory.CreateTestUser("requester", "requester@example.com", "Password123!");
        var receiver = await _factory.CreateTestUser("receiver", "receiver@example.com", "Password123!");

        var friendRequest = await AddFriendRequestToDatabase(requester.Id, receiver.Id);

        // Act
        var response = await _client.PostAsync($"/v1/friends/requests/{friendRequest.Id}/reject", null);

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    #endregion

    #region CancelFriendRequest Tests

    [Fact]
    public async Task CancelFriendRequest_ShouldReturnNoContent_WhenValidRequest()
    {
        // Arrange
        var requester = await _factory.CreateTestUser("requester", "requester@example.com", "Password123!");
        var receiver = await _factory.CreateTestUser("receiver", "receiver@example.com", "Password123!");

        var friendRequest = await AddFriendRequestToDatabase(requester.Id, receiver.Id);

        var token = GenerateJwtToken(requester, new[] { "User" });
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Act
        var response = await _client.DeleteAsync($"/v1/friends/requests/{friendRequest.Id}");

        // Assert
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        // Verify the friend request was deleted
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<MultiplayerGameDbContext>();
        var deletedRequest = await context.FriendRequests.FindAsync(friendRequest.Id);
        Assert.Null(deletedRequest);
    }

    [Fact]
    public async Task CancelFriendRequest_ShouldReturnNotFound_WhenRequestDoesNotExist()
    {
        // Arrange
        var user = await _factory.CreateTestUser("user", "user@example.com", "Password123!");

        var token = GenerateJwtToken(user, new[] { "User" });
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Act
        var response = await _client.DeleteAsync($"/v1/friends/requests/{Guid.NewGuid()}");

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task CancelFriendRequest_ShouldReturnForbidden_WhenNotTheRequester()
    {
        // Arrange
        var requester = await _factory.CreateTestUser("requester", "requester@example.com", "Password123!");
        var receiver = await _factory.CreateTestUser("receiver", "receiver@example.com", "Password123!");
        var otherUser = await _factory.CreateTestUser("other", "other@example.com", "Password123!");

        var friendRequest = await AddFriendRequestToDatabase(requester.Id, receiver.Id);

        var token = GenerateJwtToken(otherUser, new[] { "User" });
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Act
        var response = await _client.DeleteAsync($"/v1/friends/requests/{friendRequest.Id}");

        // Assert
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task CancelFriendRequest_ShouldReturnUnauthorized_WhenNotAuthenticated()
    {
        // Arrange
        var requester = await _factory.CreateTestUser("requester", "requester@example.com", "Password123!");
        var receiver = await _factory.CreateTestUser("receiver", "receiver@example.com", "Password123!");

        var friendRequest = await AddFriendRequestToDatabase(requester.Id, receiver.Id);

        // Act
        var response = await _client.DeleteAsync($"/v1/friends/requests/{friendRequest.Id}");

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    #endregion

    #region RemoveFriend Tests

    [Fact]
    public async Task RemoveFriend_ShouldReturnNoContent_WhenValidRequest()
    {
        // Arrange
        var user1 = await _factory.CreateTestUser("user1", "user1@example.com", "Password123!");
        var user2 = await _factory.CreateTestUser("user2", "user2@example.com", "Password123!");

        // Create an accepted friend request (they are friends)
        var friendRequest = await AddFriendRequestToDatabase(
            user1.Id,
            user2.Id,
            FriendRequestStatuses.Accepted,
            DateTime.UtcNow);

        var token = GenerateJwtToken(user1, new[] { "User" });
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Act
        var response = await _client.DeleteAsync($"/v1/friends/{user2.Id}");

        // Assert
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        // Verify the friendship was removed
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<MultiplayerGameDbContext>();
        var deletedFriendship = await context.FriendRequests.FindAsync(friendRequest.Id);
        Assert.Null(deletedFriendship);
    }

    [Fact]
    public async Task RemoveFriend_ShouldReturnNotFound_WhenNotFriends()
    {
        // Arrange
        var user1 = await _factory.CreateTestUser("user1", "user1@example.com", "Password123!");
        var user2 = await _factory.CreateTestUser("user2", "user2@example.com", "Password123!");

        var token = GenerateJwtToken(user1, new[] { "User" });
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Act
        var response = await _client.DeleteAsync($"/v1/friends/{user2.Id}");

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task RemoveFriend_ShouldReturnNotFound_WhenUserDoesNotExist()
    {
        // Arrange
        var user = await _factory.CreateTestUser("user", "user@example.com", "Password123!");

        var token = GenerateJwtToken(user, new[] { "User" });
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Act
        var response = await _client.DeleteAsync($"/v1/friends/{Guid.NewGuid()}");

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task RemoveFriend_ShouldReturnBadRequest_WhenTryingToRemoveSelf()
    {
        // Arrange
        var user = await _factory.CreateTestUser("user", "user@example.com", "Password123!");

        var token = GenerateJwtToken(user, new[] { "User" });
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Act
        var response = await _client.DeleteAsync($"/v1/friends/{user.Id}");

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task RemoveFriend_ShouldReturnUnauthorized_WhenNotAuthenticated()
    {
        // Arrange
        var user1 = await _factory.CreateTestUser("user1", "user1@example.com", "Password123!");
        var user2 = await _factory.CreateTestUser("user2", "user2@example.com", "Password123!");

        await AddFriendRequestToDatabase(user1.Id, user2.Id, FriendRequestStatuses.Accepted, DateTime.UtcNow);

        // Act
        var response = await _client.DeleteAsync($"/v1/friends/{user2.Id}");

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    #endregion

    #region GetReceivedFriendRequests Tests

    [Fact]
    public async Task GetReceivedFriendRequests_ShouldReturnOkWithRequests_WhenRequestsExist()
    {
        // Arrange
        var receiver = await _factory.CreateTestUser("receiver", "receiver@example.com", "Password123!");
        var requester1 = await _factory.CreateTestUser("requester1", "requester1@example.com", "Password123!");
        var requester2 = await _factory.CreateTestUser("requester2", "requester2@example.com", "Password123!");

        await AddFriendRequestToDatabase(requester1.Id, receiver.Id);
        await AddFriendRequestToDatabase(requester2.Id, receiver.Id);

        var token = GenerateJwtToken(receiver, new[] { "User" });
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Act
        var response = await _client.GetAsync("/v1/friends/requests/received");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<PagedResult<ReadFriendRequestDto>>();
        Assert.NotNull(result);
        Assert.Equal(2, result.TotalItemsCount);
        Assert.Equal(2, result.Items.Count());
    }

    [Fact]
    public async Task GetReceivedFriendRequests_ShouldReturnEmptyList_WhenNoRequests()
    {
        // Arrange
        var user = await _factory.CreateTestUser("user", "user@example.com", "Password123!");

        var token = GenerateJwtToken(user, new[] { "User" });
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Act
        var response = await _client.GetAsync("/v1/friends/requests/received");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<PagedResult<ReadFriendRequestDto>>();
        Assert.NotNull(result);
        Assert.Equal(0, result.TotalItemsCount);
        Assert.Empty(result.Items);
    }

    [Fact]
    public async Task GetReceivedFriendRequests_ShouldReturnUnauthorized_WhenNotAuthenticated()
    {
        // Act
        var response = await _client.GetAsync("/v1/friends/requests/received");

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    #endregion

    #region GetSentFriendRequests Tests

    [Fact]
    public async Task GetSentFriendRequests_ShouldReturnOkWithRequests_WhenRequestsExist()
    {
        // Arrange
        var requester = await _factory.CreateTestUser("requester", "requester@example.com", "Password123!");
        var receiver1 = await _factory.CreateTestUser("receiver1", "receiver1@example.com", "Password123!");
        var receiver2 = await _factory.CreateTestUser("receiver2", "receiver2@example.com", "Password123!");

        await AddFriendRequestToDatabase(requester.Id, receiver1.Id);
        await AddFriendRequestToDatabase(requester.Id, receiver2.Id);

        var token = GenerateJwtToken(requester, new[] { "User" });
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Act
        var response = await _client.GetAsync("/v1/friends/requests/sent");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<PagedResult<ReadFriendRequestDto>>();
        Assert.NotNull(result);
        Assert.Equal(2, result.TotalItemsCount);
        Assert.Equal(2, result.Items.Count());
    }

    [Fact]
    public async Task GetSentFriendRequests_ShouldReturnEmptyList_WhenNoRequests()
    {
        // Arrange
        var user = await _factory.CreateTestUser("user", "user@example.com", "Password123!");

        var token = GenerateJwtToken(user, new[] { "User" });
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Act
        var response = await _client.GetAsync("/v1/friends/requests/sent");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<PagedResult<ReadFriendRequestDto>>();
        Assert.NotNull(result);
        Assert.Equal(0, result.TotalItemsCount);
        Assert.Empty(result.Items);
    }

    [Fact]
    public async Task GetSentFriendRequests_ShouldReturnUnauthorized_WhenNotAuthenticated()
    {
        // Act
        var response = await _client.GetAsync("/v1/friends/requests/sent");

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    #endregion

    #region GetFriends Tests

    [Fact]
    public async Task GetFriends_ShouldReturnOkWithFriends_WhenFriendsExist()
    {
        // Arrange
        var user = await _factory.CreateTestUser("user", "user@example.com", "Password123!");
        var friend1 = await _factory.CreateTestUser("friend1", "friend1@example.com", "Password123!");
        var friend2 = await _factory.CreateTestUser("friend2", "friend2@example.com", "Password123!");

        await AddFriendRequestToDatabase(user.Id, friend1.Id, FriendRequestStatuses.Accepted, DateTime.UtcNow);
        await AddFriendRequestToDatabase(friend2.Id, user.Id, FriendRequestStatuses.Accepted, DateTime.UtcNow);

        var token = GenerateJwtToken(user, new[] { "User" });
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Act
        var response = await _client.GetAsync("/v1/friends");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<PagedResult<ReadFriendDto>>();
        Assert.NotNull(result);
        Assert.Equal(2, result.TotalItemsCount);
        Assert.Equal(2, result.Items.Count());
    }

    [Fact]
    public async Task GetFriends_ShouldReturnEmptyList_WhenNoFriends()
    {
        // Arrange
        var user = await _factory.CreateTestUser("user", "user@example.com", "Password123!");

        var token = GenerateJwtToken(user, new[] { "User" });
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Act
        var response = await _client.GetAsync("/v1/friends");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<PagedResult<ReadFriendDto>>();
        Assert.NotNull(result);
        Assert.Equal(0, result.TotalItemsCount);
        Assert.Empty(result.Items);
    }

    [Fact]
    public async Task GetFriends_ShouldNotIncludePendingRequests()
    {
        // Arrange
        var user = await _factory.CreateTestUser("user", "user@example.com", "Password123!");
        var friend = await _factory.CreateTestUser("friend", "friend@example.com", "Password123!");
        var pendingUser = await _factory.CreateTestUser("pending", "pending@example.com", "Password123!");

        await AddFriendRequestToDatabase(user.Id, friend.Id, FriendRequestStatuses.Accepted, DateTime.UtcNow);
        await AddFriendRequestToDatabase(user.Id, pendingUser.Id, FriendRequestStatuses.Pending);

        var token = GenerateJwtToken(user, new[] { "User" });
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Act
        var response = await _client.GetAsync("/v1/friends");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<PagedResult<ReadFriendDto>>();
        Assert.NotNull(result);
        Assert.Equal(1, result.TotalItemsCount);
        Assert.Single(result.Items);
        Assert.Equal(friend.Id, result.Items.ElementAt(0).UserId);
    }

    [Fact]
    public async Task GetFriends_ShouldReturnUnauthorized_WhenNotAuthenticated()
    {
        // Act
        var response = await _client.GetAsync("/v1/friends");

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    #endregion
}

