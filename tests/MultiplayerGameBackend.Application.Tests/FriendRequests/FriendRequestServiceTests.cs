using Microsoft.Extensions.Logging;
using MultiplayerGameBackend.Application.FriendRequests;
using MultiplayerGameBackend.Application.FriendRequests.Requests;
using MultiplayerGameBackend.Application.Tests.TestHelpers;
using MultiplayerGameBackend.Domain.Constants;
using MultiplayerGameBackend.Domain.Exceptions;
using MultiplayerGameBackend.Tests.Shared.Helpers;
using NSubstitute;

namespace MultiplayerGameBackend.Application.Tests.FriendRequests;

[Collection("Database")]
public class FriendRequestServiceTests : IClassFixture<DatabaseFixture>, IAsyncLifetime
{
    private readonly DatabaseFixture _fixture;
    private readonly ILogger<FriendRequestService> _logger;

    public FriendRequestServiceTests(DatabaseFixture fixture)
    {
        _fixture = fixture;
        _logger = Substitute.For<ILogger<FriendRequestService>>();
    }

    public async Task InitializeAsync() => await _fixture.InitializeAsync();
    public async Task DisposeAsync() => await _fixture.CleanDatabase();

    #region SendFriendRequest Tests

    [Fact]
    public async Task SendFriendRequest_ShouldCreateRequest_WhenValid()
    {
        // Arrange
        await using var context = _fixture.CreateDbContext();
        var friendService = new FriendRequestService(_logger, context);
        var userManager = IdentityHelper.CreateUserManager(context);
        
        var requester = await DatabaseHelper.CreateAndSaveUser(userManager, "requester", "requester@test.com");
        var receiver = await DatabaseHelper.CreateAndSaveUser(userManager, "receiver", "receiver@test.com");

        var dto = new SendFriendRequestDto { ReceiverId = receiver.Id };

        // Act
        var requestId = await friendService.SendFriendRequest(requester.Id, dto, CancellationToken.None);

        // Assert
        var request = await context.FriendRequests.FindAsync(requestId);
        Assert.NotNull(request);
        Assert.Equal(requester.Id, request.RequesterId);
        Assert.Equal(receiver.Id, request.ReceiverId);
        Assert.Equal(FriendRequestStatuses.Pending, request.Status);
        Assert.Null(request.RespondedAt);
    }

    [Fact]
    public async Task SendFriendRequest_ShouldThrowBadRequest_WhenSendingToSelf()
    {
        // Arrange
        await using var context = _fixture.CreateDbContext();
        var friendService = new FriendRequestService(_logger, context);
        var userManager = IdentityHelper.CreateUserManager(context);
        
        var user = await DatabaseHelper.CreateAndSaveUser(userManager, "user", "user@test.com");
        var dto = new SendFriendRequestDto { ReceiverId = user.Id };

        // Act & Assert
        await Assert.ThrowsAsync<BadRequest>(
            () => friendService.SendFriendRequest(user.Id, dto, CancellationToken.None));
    }

    [Fact]
    public async Task SendFriendRequest_ShouldThrowNotFoundException_WhenReceiverDoesNotExist()
    {
        // Arrange
        await using var context = _fixture.CreateDbContext();
        var friendService = new FriendRequestService(_logger, context);
        var userManager = IdentityHelper.CreateUserManager(context);
        
        var requester = await DatabaseHelper.CreateAndSaveUser(userManager, "requester", "requester@test.com");
        var dto = new SendFriendRequestDto { ReceiverId = Guid.NewGuid() };

        // Act & Assert
        await Assert.ThrowsAsync<NotFoundException>(
            () => friendService.SendFriendRequest(requester.Id, dto, CancellationToken.None));
    }

    [Fact]
    public async Task SendFriendRequest_ShouldThrowConflict_WhenAlreadyFriends()
    {
        // Arrange
        await using var context = _fixture.CreateDbContext();
        var friendService = new FriendRequestService(_logger, context);
        var userManager = IdentityHelper.CreateUserManager(context);
        
        var user1 = await DatabaseHelper.CreateAndSaveUser(userManager, "user1", "user1@test.com");
        var user2 = await DatabaseHelper.CreateAndSaveUser(userManager, "user2", "user2@test.com");
        await DatabaseHelper.CreateAndSaveFriendRequest(context, user1.Id, user2.Id, FriendRequestStatuses.Accepted);

        var dto = new SendFriendRequestDto { ReceiverId = user2.Id };

        // Act & Assert
        await Assert.ThrowsAsync<ConflictException>(
            () => friendService.SendFriendRequest(user1.Id, dto, CancellationToken.None));
    }

    [Fact]
    public async Task SendFriendRequest_ShouldThrowConflict_WhenPendingRequestExists()
    {
        // Arrange
        await using var context = _fixture.CreateDbContext();
        var friendService = new FriendRequestService(_logger, context);
        var userManager = IdentityHelper.CreateUserManager(context);
        
        var user1 = await DatabaseHelper.CreateAndSaveUser(userManager, "user1", "user1@test.com");
        var user2 = await DatabaseHelper.CreateAndSaveUser(userManager, "user2", "user2@test.com");
        await DatabaseHelper.CreateAndSaveFriendRequest(context, user1.Id, user2.Id);

        var dto = new SendFriendRequestDto { ReceiverId = user2.Id };

        // Act & Assert
        await Assert.ThrowsAsync<ConflictException>(
            () => friendService.SendFriendRequest(user1.Id, dto, CancellationToken.None));
    }

    [Fact]
    public async Task SendFriendRequest_ShouldAutoAccept_WhenReceiverAlreadySentRequest()
    {
        // Arrange
        await using var context = _fixture.CreateDbContext();
        var friendService = new FriendRequestService(_logger, context);
        var userManager = IdentityHelper.CreateUserManager(context);
        
        var user1 = await DatabaseHelper.CreateAndSaveUser(userManager, "user1", "user1@test.com");
        var user2 = await DatabaseHelper.CreateAndSaveUser(userManager, "user2", "user2@test.com");
        var existingRequest = await DatabaseHelper.CreateAndSaveFriendRequest(context, user2.Id, user1.Id);

        var dto = new SendFriendRequestDto { ReceiverId = user2.Id };

        // Act
        var requestId = await friendService.SendFriendRequest(user1.Id, dto, CancellationToken.None);

        // Assert
        Assert.Equal(existingRequest.Id, requestId);
        var request = await context.FriendRequests.FindAsync(requestId);
        Assert.NotNull(request);
        Assert.Equal(FriendRequestStatuses.Accepted, request.Status);
        Assert.NotNull(request.RespondedAt);
    }

    #endregion

    #region AcceptFriendRequest Tests

    [Fact]
    public async Task AcceptFriendRequest_ShouldAcceptRequest_WhenValid()
    {
        // Arrange
        await using var context = _fixture.CreateDbContext();
        var friendService = new FriendRequestService(_logger, context);
        var userManager = IdentityHelper.CreateUserManager(context);
        
        var requester = await DatabaseHelper.CreateAndSaveUser(userManager, "requester", "requester@test.com");
        var receiver = await DatabaseHelper.CreateAndSaveUser(userManager, "receiver", "receiver@test.com");
        var request = await DatabaseHelper.CreateAndSaveFriendRequest(context, requester.Id, receiver.Id);

        // Act
        await friendService.AcceptFriendRequest(receiver.Id, request.Id, CancellationToken.None);

        // Assert
        var updatedRequest = await context.FriendRequests.FindAsync(request.Id);
        Assert.NotNull(updatedRequest);
        Assert.Equal(FriendRequestStatuses.Accepted, updatedRequest.Status);
        Assert.NotNull(updatedRequest.RespondedAt);
    }

    [Fact]
    public async Task AcceptFriendRequest_ShouldThrowNotFoundException_WhenRequestDoesNotExist()
    {
        // Arrange
        await using var context = _fixture.CreateDbContext();
        var friendService = new FriendRequestService(_logger, context);
        var userManager = IdentityHelper.CreateUserManager(context);
        
        var user = await DatabaseHelper.CreateAndSaveUser(userManager, "user", "user@test.com");

        // Act & Assert
        await Assert.ThrowsAsync<NotFoundException>(
            () => friendService.AcceptFriendRequest(user.Id, Guid.NewGuid(), CancellationToken.None));
    }

    [Fact]
    public async Task AcceptFriendRequest_ShouldThrowForbidden_WhenNotReceiver()
    {
        // Arrange
        await using var context = _fixture.CreateDbContext();
        var friendService = new FriendRequestService(_logger, context);
        var userManager = IdentityHelper.CreateUserManager(context);
        
        var requester = await DatabaseHelper.CreateAndSaveUser(userManager, "requester", "requester@test.com");
        var receiver = await DatabaseHelper.CreateAndSaveUser(userManager, "receiver", "receiver@test.com");
        var otherUser = await DatabaseHelper.CreateAndSaveUser(userManager, "other", "other@test.com");
        var request = await DatabaseHelper.CreateAndSaveFriendRequest(context, requester.Id, receiver.Id);

        // Act & Assert
        await Assert.ThrowsAsync<ForbidException>(
            () => friendService.AcceptFriendRequest(otherUser.Id, request.Id, CancellationToken.None));
    }

    [Fact]
    public async Task AcceptFriendRequest_ShouldThrowBadRequest_WhenAlreadyResponded()
    {
        // Arrange
        await using var context = _fixture.CreateDbContext();
        var friendService = new FriendRequestService(_logger, context);
        var userManager = IdentityHelper.CreateUserManager(context);
        
        var requester = await DatabaseHelper.CreateAndSaveUser(userManager, "requester", "requester@test.com");
        var receiver = await DatabaseHelper.CreateAndSaveUser(userManager, "receiver", "receiver@test.com");
        var request = await DatabaseHelper.CreateAndSaveFriendRequest(
            context, requester.Id, receiver.Id, FriendRequestStatuses.Accepted, DateTime.UtcNow);

        // Act & Assert
        await Assert.ThrowsAsync<BadRequest>(
            () => friendService.AcceptFriendRequest(receiver.Id, request.Id, CancellationToken.None));
    }

    #endregion

    #region RejectFriendRequest Tests

    [Fact]
    public async Task RejectFriendRequest_ShouldRejectRequest_WhenValid()
    {
        // Arrange
        await using var context = _fixture.CreateDbContext();
        var friendService = new FriendRequestService(_logger, context);
        var userManager = IdentityHelper.CreateUserManager(context);
        
        var requester = await DatabaseHelper.CreateAndSaveUser(userManager, "requester", "requester@test.com");
        var receiver = await DatabaseHelper.CreateAndSaveUser(userManager, "receiver", "receiver@test.com");
        var request = await DatabaseHelper.CreateAndSaveFriendRequest(context, requester.Id, receiver.Id);

        // Act
        await friendService.RejectFriendRequest(receiver.Id, request.Id, CancellationToken.None);

        // Assert
        var updatedRequest = await context.FriendRequests.FindAsync(request.Id);
        Assert.NotNull(updatedRequest);
        Assert.Equal(FriendRequestStatuses.Rejected, updatedRequest.Status);
        Assert.NotNull(updatedRequest.RespondedAt);
    }

    [Fact]
    public async Task RejectFriendRequest_ShouldThrowNotFoundException_WhenRequestDoesNotExist()
    {
        // Arrange
        await using var context = _fixture.CreateDbContext();
        var friendService = new FriendRequestService(_logger, context);
        var userManager = IdentityHelper.CreateUserManager(context);
        
        var user = await DatabaseHelper.CreateAndSaveUser(userManager, "user", "user@test.com");

        // Act & Assert
        await Assert.ThrowsAsync<NotFoundException>(
            () => friendService.RejectFriendRequest(user.Id, Guid.NewGuid(), CancellationToken.None));
    }

    [Fact]
    public async Task RejectFriendRequest_ShouldThrowForbidden_WhenNotReceiver()
    {
        // Arrange
        await using var context = _fixture.CreateDbContext();
        var friendService = new FriendRequestService(_logger, context);
        var userManager = IdentityHelper.CreateUserManager(context);
        
        var requester = await DatabaseHelper.CreateAndSaveUser(userManager, "requester", "requester@test.com");
        var receiver = await DatabaseHelper.CreateAndSaveUser(userManager, "receiver", "receiver@test.com");
        var otherUser = await DatabaseHelper.CreateAndSaveUser(userManager, "other", "other@test.com");
        var request = await DatabaseHelper.CreateAndSaveFriendRequest(context, requester.Id, receiver.Id);

        // Act & Assert
        await Assert.ThrowsAsync<ForbidException>(
            () => friendService.RejectFriendRequest(otherUser.Id, request.Id, CancellationToken.None));
    }

    #endregion

    #region CancelFriendRequest Tests

    [Fact]
    public async Task CancelFriendRequest_ShouldDeleteRequest_WhenValid()
    {
        // Arrange
        await using var context = _fixture.CreateDbContext();
        var friendService = new FriendRequestService(_logger, context);
        var userManager = IdentityHelper.CreateUserManager(context);
        
        var requester = await DatabaseHelper.CreateAndSaveUser(userManager, "requester", "requester@test.com");
        var receiver = await DatabaseHelper.CreateAndSaveUser(userManager, "receiver", "receiver@test.com");
        var request = await DatabaseHelper.CreateAndSaveFriendRequest(context, requester.Id, receiver.Id);

        // Act
        await friendService.CancelFriendRequest(requester.Id, request.Id, CancellationToken.None);

        // Assert
        var deletedRequest = await context.FriendRequests.FindAsync(request.Id);
        Assert.Null(deletedRequest);
    }

    [Fact]
    public async Task CancelFriendRequest_ShouldThrowNotFoundException_WhenRequestDoesNotExist()
    {
        // Arrange
        await using var context = _fixture.CreateDbContext();
        var friendService = new FriendRequestService(_logger, context);
        var userManager = IdentityHelper.CreateUserManager(context);
        
        var user = await DatabaseHelper.CreateAndSaveUser(userManager, "user", "user@test.com");

        // Act & Assert
        await Assert.ThrowsAsync<NotFoundException>(
            () => friendService.CancelFriendRequest(user.Id, Guid.NewGuid(), CancellationToken.None));
    }

    [Fact]
    public async Task CancelFriendRequest_ShouldThrowForbidden_WhenNotRequester()
    {
        // Arrange
        await using var context = _fixture.CreateDbContext();
        var friendService = new FriendRequestService(_logger, context);
        var userManager = IdentityHelper.CreateUserManager(context);
        
        var requester = await DatabaseHelper.CreateAndSaveUser(userManager, "requester", "requester@test.com");
        var receiver = await DatabaseHelper.CreateAndSaveUser(userManager, "receiver", "receiver@test.com");
        var otherUser = await DatabaseHelper.CreateAndSaveUser(userManager, "other", "other@test.com");
        var request = await DatabaseHelper.CreateAndSaveFriendRequest(context, requester.Id, receiver.Id);

        // Act & Assert
        await Assert.ThrowsAsync<ForbidException>(
            () => friendService.CancelFriendRequest(otherUser.Id, request.Id, CancellationToken.None));
    }

    [Fact]
    public async Task CancelFriendRequest_ShouldThrowBadRequest_WhenAlreadyResponded()
    {
        // Arrange
        await using var context = _fixture.CreateDbContext();
        var friendService = new FriendRequestService(_logger, context);
        var userManager = IdentityHelper.CreateUserManager(context);
        
        var requester = await DatabaseHelper.CreateAndSaveUser(userManager, "requester", "requester@test.com");
        var receiver = await DatabaseHelper.CreateAndSaveUser(userManager, "receiver", "receiver@test.com");
        var request = await DatabaseHelper.CreateAndSaveFriendRequest(
            context, requester.Id, receiver.Id, FriendRequestStatuses.Accepted, DateTime.UtcNow);

        // Act & Assert
        await Assert.ThrowsAsync<BadRequest>(
            () => friendService.CancelFriendRequest(requester.Id, request.Id, CancellationToken.None));
    }

    #endregion

    #region RemoveFriend Tests

    [Fact]
    public async Task RemoveFriend_ShouldDeleteFriendship_WhenValid()
    {
        // Arrange
        await using var context = _fixture.CreateDbContext();
        var friendService = new FriendRequestService(_logger, context);
        var userManager = IdentityHelper.CreateUserManager(context);
        
        var user1 = await DatabaseHelper.CreateAndSaveUser(userManager, "user1", "user1@test.com");
        var user2 = await DatabaseHelper.CreateAndSaveUser(userManager, "user2", "user2@test.com");
        var request = await DatabaseHelper.CreateAndSaveFriendRequest(
            context, user1.Id, user2.Id, FriendRequestStatuses.Accepted, DateTime.UtcNow);

        // Act
        await friendService.RemoveFriend(user1.Id, user2.Id, CancellationToken.None);

        // Assert
        var deletedRequest = await context.FriendRequests.FindAsync(request.Id);
        Assert.Null(deletedRequest);
    }

    [Fact]
    public async Task RemoveFriend_ShouldWork_WhenCalledByEitherFriend()
    {
        // Arrange
        await using var context = _fixture.CreateDbContext();
        var friendService = new FriendRequestService(_logger, context);
        var userManager = IdentityHelper.CreateUserManager(context);
        
        var user1 = await DatabaseHelper.CreateAndSaveUser(userManager, "user1", "user1@test.com");
        var user2 = await DatabaseHelper.CreateAndSaveUser(userManager, "user2", "user2@test.com");
        var request = await DatabaseHelper.CreateAndSaveFriendRequest(
            context, user1.Id, user2.Id, FriendRequestStatuses.Accepted, DateTime.UtcNow);

        // Act - user2 removes user1 (reverse direction)
        await friendService.RemoveFriend(user2.Id, user1.Id, CancellationToken.None);

        // Assert
        var deletedRequest = await context.FriendRequests.FindAsync(request.Id);
        Assert.Null(deletedRequest);
    }

    [Fact]
    public async Task RemoveFriend_ShouldThrowBadRequest_WhenRemovingSelf()
    {
        // Arrange
        await using var context = _fixture.CreateDbContext();
        var friendService = new FriendRequestService(_logger, context);
        var userManager = IdentityHelper.CreateUserManager(context);
        
        var user = await DatabaseHelper.CreateAndSaveUser(userManager, "user", "user@test.com");

        // Act & Assert
        await Assert.ThrowsAsync<BadRequest>(
            () => friendService.RemoveFriend(user.Id, user.Id, CancellationToken.None));
    }

    [Fact]
    public async Task RemoveFriend_ShouldThrowNotFoundException_WhenNotFriends()
    {
        // Arrange
        await using var context = _fixture.CreateDbContext();
        var friendService = new FriendRequestService(_logger, context);
        var userManager = IdentityHelper.CreateUserManager(context);
        
        var user1 = await DatabaseHelper.CreateAndSaveUser(userManager, "user1", "user1@test.com");
        var user2 = await DatabaseHelper.CreateAndSaveUser(userManager, "user2", "user2@test.com");

        // Act & Assert
        await Assert.ThrowsAsync<NotFoundException>(
            () => friendService.RemoveFriend(user1.Id, user2.Id, CancellationToken.None));
    }

    #endregion

    #region GetReceivedFriendRequests Tests

    [Fact]
    public async Task GetReceivedFriendRequests_ShouldReturnOnlyPendingReceivedRequests()
    {
        // Arrange
        await using var context = _fixture.CreateDbContext();
        var friendService = new FriendRequestService(_logger, context);
        var userManager = IdentityHelper.CreateUserManager(context);
        
        var user = await DatabaseHelper.CreateAndSaveUser(userManager, "user", "user@test.com");
        var requester1 = await DatabaseHelper.CreateAndSaveUser(userManager, "req1", "req1@test.com");
        var requester2 = await DatabaseHelper.CreateAndSaveUser(userManager, "req2", "req2@test.com");
        var requester3 = await DatabaseHelper.CreateAndSaveUser(userManager, "req3", "req3@test.com");

        await DatabaseHelper.CreateAndSaveFriendRequest(context, requester1.Id, user.Id); // Pending - should be returned
        await DatabaseHelper.CreateAndSaveFriendRequest(context, requester2.Id, user.Id, FriendRequestStatuses.Accepted); // Accepted - should NOT be returned
        await DatabaseHelper.CreateAndSaveFriendRequest(context, user.Id, requester3.Id); // Sent by user - should NOT be returned

        var query = new Application.Common.PagedQuery { PageNumber = 1, PageSize = 10 };

        // Act
        var result = await friendService.GetReceivedFriendRequests(user.Id, query, CancellationToken.None);

        // Assert
        Assert.Single(result.Items);
        Assert.Equal(requester1.Id, result.Items.ElementAt(0).RequesterId);
    }

    [Fact]
    public async Task GetReceivedFriendRequests_ShouldFilterBySearchPhrase()
    {
        // Arrange
        await using var context = _fixture.CreateDbContext();
        var friendService = new FriendRequestService(_logger, context);
        var userManager = IdentityHelper.CreateUserManager(context);
        
        var user = await DatabaseHelper.CreateAndSaveUser(userManager, "user", "user@test.com");
        var requester1 = await DatabaseHelper.CreateAndSaveUser(userManager, "alice", "alice@test.com");
        var requester2 = await DatabaseHelper.CreateAndSaveUser(userManager, "bob", "bob@test.com");

        await DatabaseHelper.CreateAndSaveFriendRequest(context, requester1.Id, user.Id);
        await DatabaseHelper.CreateAndSaveFriendRequest(context, requester2.Id, user.Id);

        var query = new Application.Common.PagedQuery { PageNumber = 1, PageSize = 10, SearchPhrase = "alice" };

        // Act
        var result = await friendService.GetReceivedFriendRequests(user.Id, query, CancellationToken.None);

        // Assert
        Assert.Single(result.Items);
        Assert.Equal("alice", result.Items.ElementAt(0).RequesterUsername);
    }

    #endregion

    #region GetSentFriendRequests Tests

    [Fact]
    public async Task GetSentFriendRequests_ShouldReturnOnlyPendingSentRequests()
    {
        // Arrange
        await using var context = _fixture.CreateDbContext();
        var friendService = new FriendRequestService(_logger, context);
        var userManager = IdentityHelper.CreateUserManager(context);
        
        var user = await DatabaseHelper.CreateAndSaveUser(userManager, "user", "user@test.com");
        var receiver1 = await DatabaseHelper.CreateAndSaveUser(userManager, "rec1", "rec1@test.com");
        var receiver2 = await DatabaseHelper.CreateAndSaveUser(userManager, "rec2", "rec2@test.com");
        var receiver3 = await DatabaseHelper.CreateAndSaveUser(userManager, "rec3", "rec3@test.com");

        await DatabaseHelper.CreateAndSaveFriendRequest(context, user.Id, receiver1.Id); // Pending - should be returned
        await DatabaseHelper.CreateAndSaveFriendRequest(context, user.Id, receiver2.Id, FriendRequestStatuses.Rejected); // Rejected - should NOT be returned
        await DatabaseHelper.CreateAndSaveFriendRequest(context, receiver3.Id, user.Id); // Received by user - should NOT be returned

        var query = new Application.Common.PagedQuery { PageNumber = 1, PageSize = 10 };

        // Act
        var result = await friendService.GetSentFriendRequests(user.Id, query, CancellationToken.None);

        // Assert
        Assert.Single(result.Items);
        Assert.Equal(receiver1.Id, result.Items.ElementAt(0).ReceiverId);
    }

    #endregion

    #region GetFriends Tests

    [Fact]
    public async Task GetFriends_ShouldReturnOnlyAcceptedFriendships()
    {
        // Arrange
        await using var context = _fixture.CreateDbContext();
        var friendService = new FriendRequestService(_logger, context);
        var userManager = IdentityHelper.CreateUserManager(context);
        
        var user = await DatabaseHelper.CreateAndSaveUser(userManager, "user", "user@test.com");
        var friend1 = await DatabaseHelper.CreateAndSaveUser(userManager, "friend1", "friend1@test.com");
        var friend2 = await DatabaseHelper.CreateAndSaveUser(userManager, "friend2", "friend2@test.com");
        var notFriend = await DatabaseHelper.CreateAndSaveUser(userManager, "notfriend", "notfriend@test.com");

        await DatabaseHelper.CreateAndSaveFriendRequest(context, user.Id, friend1.Id, FriendRequestStatuses.Accepted, DateTime.UtcNow);
        await DatabaseHelper.CreateAndSaveFriendRequest(context, friend2.Id, user.Id, FriendRequestStatuses.Accepted, DateTime.UtcNow);
        await DatabaseHelper.CreateAndSaveFriendRequest(context, user.Id, notFriend.Id); // Pending - should NOT be returned

        var query = new Application.Common.PagedQuery { PageNumber = 1, PageSize = 10 };

        // Act
        var result = await friendService.GetFriends(user.Id, query, CancellationToken.None);

        // Assert
        Assert.Equal(2, result.Items.Count());
        Assert.Contains(result.Items, f => f.UserId == friend1.Id);
        Assert.Contains(result.Items, f => f.UserId == friend2.Id);
    }

    [Fact]
    public async Task GetFriends_ShouldFilterBySearchPhrase()
    {
        // Arrange
        await using var context = _fixture.CreateDbContext();
        var friendService = new FriendRequestService(_logger, context);
        var userManager = IdentityHelper.CreateUserManager(context);
        
        var user = await DatabaseHelper.CreateAndSaveUser(userManager, "user", "user@test.com");
        var friend1 = await DatabaseHelper.CreateAndSaveUser(userManager, "alice", "alice@test.com");
        var friend2 = await DatabaseHelper.CreateAndSaveUser(userManager, "bob", "bob@test.com");

        await DatabaseHelper.CreateAndSaveFriendRequest(context, user.Id, friend1.Id, FriendRequestStatuses.Accepted, DateTime.UtcNow);
        await DatabaseHelper.CreateAndSaveFriendRequest(context, user.Id, friend2.Id, FriendRequestStatuses.Accepted, DateTime.UtcNow);

        var query = new Application.Common.PagedQuery { PageNumber = 1, PageSize = 10, SearchPhrase = "alice" };

        // Act
        var result = await friendService.GetFriends(user.Id, query, CancellationToken.None);

        // Assert
        Assert.Single(result.Items);
        Assert.Equal("alice", result.Items.ElementAt(0).Username);
    }

    [Fact]
    public async Task GetFriends_ShouldSortByUsername_WhenSpecified()
    {
        // Arrange
        await using var context = _fixture.CreateDbContext();
        var friendService = new FriendRequestService(_logger, context);
        var userManager = IdentityHelper.CreateUserManager(context);
        
        var user = await DatabaseHelper.CreateAndSaveUser(userManager, "user", "user@test.com");
        var friend1 = await DatabaseHelper.CreateAndSaveUser(userManager, "charlie", "charlie@test.com");
        var friend2 = await DatabaseHelper.CreateAndSaveUser(userManager, "alice", "alice@test.com");
        var friend3 = await DatabaseHelper.CreateAndSaveUser(userManager, "bob", "bob@test.com");

        await DatabaseHelper.CreateAndSaveFriendRequest(context, user.Id, friend1.Id, FriendRequestStatuses.Accepted, DateTime.UtcNow);
        await DatabaseHelper.CreateAndSaveFriendRequest(context, user.Id, friend2.Id, FriendRequestStatuses.Accepted, DateTime.UtcNow);
        await DatabaseHelper.CreateAndSaveFriendRequest(context, user.Id, friend3.Id, FriendRequestStatuses.Accepted, DateTime.UtcNow);

        var query = new Application.Common.PagedQuery 
        { 
            PageNumber = 1, 
            PageSize = 10, 
            SortBy = "Username", 
            SortDirection = SortDirection.Ascending 
        };

        // Act
        var result = await friendService.GetFriends(user.Id, query, CancellationToken.None);

        // Assert
        Assert.Equal(3, result.Items.Count());
        Assert.Equal("alice", result.Items.ElementAt(0).Username);
        Assert.Equal("bob", result.Items.ElementAt(1).Username);
        Assert.Equal("charlie", result.Items.ElementAt(2).Username);
    }

    [Fact]
    public async Task GetFriends_ShouldPaginate_Correctly()
    {
        // Arrange
        await using var context = _fixture.CreateDbContext();
        var friendService = new FriendRequestService(_logger, context);
        var userManager = IdentityHelper.CreateUserManager(context);
        
        var user = await DatabaseHelper.CreateAndSaveUser(userManager, "user", "user@test.com");
        
        for (int i = 1; i <= 5; i++)
        {
            var friend = await DatabaseHelper.CreateAndSaveUser(userManager, $"friend{i}", $"friend{i}@test.com");
            await DatabaseHelper.CreateAndSaveFriendRequest(context, user.Id, friend.Id, FriendRequestStatuses.Accepted, DateTime.UtcNow);
        }

        var query = new Application.Common.PagedQuery { PageNumber = 2, PageSize = 2 };

        // Act
        var result = await friendService.GetFriends(user.Id, query, CancellationToken.None);

        // Assert
        Assert.Equal(2, result.Items.Count());
        Assert.Equal(5, result.TotalItemsCount);
        Assert.Equal(3, result.TotalPages);
    }

    #endregion
}

