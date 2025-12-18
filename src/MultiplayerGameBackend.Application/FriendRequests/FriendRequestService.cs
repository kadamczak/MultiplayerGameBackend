using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using MultiplayerGameBackend.Application.Common;
using MultiplayerGameBackend.Application.Common.Mappings;
using MultiplayerGameBackend.Application.FriendRequests.Requests;
using MultiplayerGameBackend.Application.FriendRequests.Responses;
using MultiplayerGameBackend.Application.FriendRequests.Specifications;
using MultiplayerGameBackend.Application.Friends.Responses;
using MultiplayerGameBackend.Application.Interfaces;
using MultiplayerGameBackend.Domain.Constants;
using MultiplayerGameBackend.Domain.Entities;
using MultiplayerGameBackend.Domain.Exceptions;

namespace MultiplayerGameBackend.Application.FriendRequests;

public class FriendRequestService(
    ILogger<FriendRequestService> logger,
    IMultiplayerGameDbContext dbContext,
    FriendRequestMapper friendRequestMapper) : IFriendRequestService
{
    public async Task<Guid> SendFriendRequest(Guid currentUserId, SendFriendRequestDto dto, CancellationToken cancellationToken)
    {
        logger.LogInformation("User {CurrentUserId} is sending friend request to {ReceiverId}", currentUserId, dto.ReceiverId);

        if (currentUserId == dto.ReceiverId)
            throw new BadRequest("You cannot send a friend request to yourself.");

        var receiver = await dbContext.Users
            .FirstOrDefaultAsync(u => u.Id == dto.ReceiverId, cancellationToken);

        if (receiver is null)
            throw new NotFoundException(nameof(User), nameof(User.Id), "ID", dto.ReceiverId.ToString());

        // Check if already friends
        var existingAcceptedRequest = await dbContext.FriendRequests
            .AnyAsync(FriendRequestSpecifications.AreFriends(currentUserId, receiver.Id), cancellationToken);

        if (existingAcceptedRequest)
            throw new ConflictException(new Dictionary<string, string[]>
            {
                { "ReceiverId", new[] { "You are already friends with this user." } }
            });

        // Check if there's already a pending request
        var existingPendingRequest = await dbContext.FriendRequests
            .FirstOrDefaultAsync(FriendRequestSpecifications.HavePendingRequest(currentUserId, dto.ReceiverId), cancellationToken);

        if (existingPendingRequest is not null)
        {
            // If the other user already sent a request to current user, automatically accept it
            if (existingPendingRequest.RequesterId == dto.ReceiverId && existingPendingRequest.ReceiverId == currentUserId)
            {
                existingPendingRequest.Status = FriendRequestStatuses.Accepted;
                existingPendingRequest.RespondedAt = DateTime.UtcNow;
                await dbContext.SaveChangesAsync(cancellationToken);
                logger.LogInformation("Automatically accepted existing friend request {RequestId}", existingPendingRequest.Id);
                return existingPendingRequest.Id;
            }

            throw new ConflictException(new Dictionary<string, string[]>
            {
                { "ReceiverId", new[] { "You have already sent a friend request to this user." } }
            });
        }

        // Check pending request limit
        var pendingRequestCount = await dbContext.FriendRequests
            .CountAsync(FriendRequestSpecifications.IsPendingRequestSentBy(currentUserId), cancellationToken);

        if (pendingRequestCount >= FriendRequest.Constraints.MaxPendingRequestsPerUser)
            throw new BadRequest($"You have reached the maximum number of pending friend requests ({FriendRequest.Constraints.MaxPendingRequestsPerUser}).");

        // Check friend limit for both users
        var friendCount = await dbContext.FriendRequests
            .CountAsync(FriendRequestSpecifications.IsFriendshipWithUser(currentUserId), cancellationToken);

        if (friendCount >= FriendRequest.Constraints.MaxFriendsPerUser)
            throw new BadRequest($"You have reached the maximum number of friends ({FriendRequest.Constraints.MaxFriendsPerUser}).");

        var friendRequest = new FriendRequest
        {
            RequesterId = currentUserId,
            ReceiverId = dto.ReceiverId,
            Status = FriendRequestStatuses.Pending,
            CreatedAt = DateTime.UtcNow
        };

        dbContext.FriendRequests.Add(friendRequest);
        await dbContext.SaveChangesAsync(cancellationToken);

        logger.LogInformation("Friend request {RequestId} created successfully", friendRequest.Id);
        return friendRequest.Id;
    }

    public async Task AcceptFriendRequest(Guid currentUserId, Guid requestId, CancellationToken cancellationToken)
    {
        logger.LogInformation("User {CurrentUserId} is accepting friend request {RequestId}", currentUserId, requestId);

        var friendRequest = await dbContext.FriendRequests
            .FirstOrDefaultAsync(fr => fr.Id == requestId, cancellationToken);

        if (friendRequest is null)
            throw new NotFoundException(nameof(FriendRequest), nameof(FriendRequest.Id), "ID", requestId.ToString());

        if (friendRequest.ReceiverId != currentUserId)
            throw new ForbidException("You can only accept friend requests sent to you.");

        if (friendRequest.Status != FriendRequestStatuses.Pending)
            throw new BadRequest("This friend request has already been responded to.");

        // Check friend count limit for receiver
        var receiverFriendCount = await dbContext.FriendRequests
            .CountAsync(FriendRequestSpecifications.IsFriendshipWithUser(currentUserId), cancellationToken);

        if (receiverFriendCount >= FriendRequest.Constraints.MaxFriendsPerUser)
            throw new BadRequest($"You have reached the maximum number of friends ({FriendRequest.Constraints.MaxFriendsPerUser}).");

        // Check friend count limit for requester
        var requesterFriendCount = await dbContext.FriendRequests
            .CountAsync(FriendRequestSpecifications.IsFriendshipWithUser(friendRequest.RequesterId), cancellationToken);

        if (requesterFriendCount >= FriendRequest.Constraints.MaxFriendsPerUser)
            throw new BadRequest($"The requester has reached the maximum number of friends ({FriendRequest.Constraints.MaxFriendsPerUser}).");

        friendRequest.Status = FriendRequestStatuses.Accepted;
        friendRequest.RespondedAt = DateTime.UtcNow;

        await dbContext.SaveChangesAsync(cancellationToken);
        logger.LogInformation("Friend request {RequestId} accepted successfully", requestId);
    }

    public async Task RejectFriendRequest(Guid currentUserId, Guid requestId, CancellationToken cancellationToken)
    {
        logger.LogInformation("User {CurrentUserId} is rejecting friend request {RequestId}", currentUserId, requestId);

        var friendRequest = await dbContext.FriendRequests
            .FirstOrDefaultAsync(fr => fr.Id == requestId, cancellationToken);

        if (friendRequest is null)
            throw new NotFoundException(nameof(FriendRequest), nameof(FriendRequest.Id), "ID", requestId.ToString());

        if (friendRequest.ReceiverId != currentUserId)
            throw new ForbidException("You can only reject friend requests sent to you.");

        if (friendRequest.Status != FriendRequestStatuses.Pending)
            throw new BadRequest("This friend request has already been responded to.");

        friendRequest.Status = FriendRequestStatuses.Rejected;
        friendRequest.RespondedAt = DateTime.UtcNow;

        await dbContext.SaveChangesAsync(cancellationToken);
        logger.LogInformation("Friend request {RequestId} rejected successfully", requestId);
    }

    public async Task CancelFriendRequest(Guid currentUserId, Guid requestId, CancellationToken cancellationToken)
    {
        logger.LogInformation("User {CurrentUserId} is canceling friend request {RequestId}", currentUserId, requestId);

        var friendRequest = await dbContext.FriendRequests
            .FirstOrDefaultAsync(fr => fr.Id == requestId, cancellationToken);

        if (friendRequest is null)
            throw new NotFoundException(nameof(FriendRequest), nameof(FriendRequest.Id), "ID", requestId.ToString());

        if (friendRequest.RequesterId != currentUserId)
            throw new ForbidException("You can only cancel friend requests that you sent.");

        if (friendRequest.Status != FriendRequestStatuses.Pending)
            throw new BadRequest("You can only cancel pending friend requests.");

        dbContext.FriendRequests.Remove(friendRequest);
        await dbContext.SaveChangesAsync(cancellationToken);
        logger.LogInformation("Friend request {RequestId} canceled successfully", requestId);
    }

    public async Task RemoveFriend(Guid currentUserId, Guid friendUserId, CancellationToken cancellationToken)
    {
        logger.LogInformation("User {CurrentUserId} is removing friend {FriendUserId}", currentUserId, friendUserId);

        if (currentUserId == friendUserId)
            throw new BadRequest("Invalid friend user ID.");

        var friendRequest = await dbContext.FriendRequests
            .FirstOrDefaultAsync(FriendRequestSpecifications.AreFriends(currentUserId, friendUserId), cancellationToken);

        if (friendRequest is null)
            throw new NotFoundException(nameof(User), nameof(User.Id), "user ID", friendUserId.ToString());

        dbContext.FriendRequests.Remove(friendRequest);
        await dbContext.SaveChangesAsync(cancellationToken);
        logger.LogInformation("Friend removed successfully");
    }

    public async Task<PagedResult<ReadFriendRequestDto>> GetReceivedFriendRequests(Guid currentUserId, GetFriendRequestsDto dto, CancellationToken cancellationToken)
    {
        logger.LogInformation("Fetching received friend requests for user {CurrentUserId}", currentUserId);
        var searchPhraseLower = dto.PagedQuery.SearchPhrase?.ToLower();
        
        // Build base query for counting (without includes for performance)
        var countQuery = dbContext.FriendRequests
            .AsNoTracking()
            .Where(FriendRequestSpecifications.IsPendingRequestReceivedBy(currentUserId))
            .ApplySearchFilter(
                searchPhraseLower,
                FriendRequestSpecifications.SearchByRequesterUsername(searchPhraseLower!));

        var totalCount = await countQuery.CountAsync(cancellationToken);
        
        // Build query for fetching data (with includes and sorting)
        var dataQuery = dbContext.FriendRequests
            .AsNoTracking()
            .Where(FriendRequestSpecifications.IsPendingRequestReceivedBy(currentUserId))
            .Include(fr => fr.Requester)
            .Include(fr => fr.Receiver)
            .ApplySearchFilter(
                searchPhraseLower,
                FriendRequestSpecifications.SearchByRequesterUsername(searchPhraseLower!))
            .ApplySorting(
                dto.PagedQuery.SortBy,
                dto.PagedQuery.SortDirection,
                FriendRequestSortingSelectors.ForReceivedRequests(),
                defaultSort: fr => fr.CreatedAt)
            .ApplyPaging(dto.PagedQuery);

        var friendRequests = await dataQuery.ToListAsync(cancellationToken);
        var mappedRequests = friendRequests.Select(fr => friendRequestMapper.MapToReadFriendRequestDto(fr)!).ToList();
        
        return new PagedResult<ReadFriendRequestDto>(mappedRequests, totalCount, dto.PagedQuery.PageSize, dto.PagedQuery.PageNumber);
    }

    public async Task<PagedResult<ReadFriendRequestDto>> GetSentFriendRequests(Guid currentUserId, GetFriendRequestsDto dto, CancellationToken cancellationToken)
    {
        logger.LogInformation("Fetching sent friend requests for user {CurrentUserId}", currentUserId);
        var searchPhraseLower = dto.PagedQuery.SearchPhrase?.ToLower();
        
        // Build base query for counting (without includes for performance)
        var countQuery = dbContext.FriendRequests
            .AsNoTracking()
            .Where(FriendRequestSpecifications.IsPendingRequestSentBy(currentUserId))
            .ApplySearchFilter(
                searchPhraseLower,
                FriendRequestSpecifications.SearchByReceiverUsername(searchPhraseLower!));

        var totalCount = await countQuery.CountAsync(cancellationToken);
        
        // Build query for fetching data (with includes and sorting)
        var dataQuery = dbContext.FriendRequests
            .AsNoTracking()
            .Where(FriendRequestSpecifications.IsPendingRequestSentBy(currentUserId))
            .Include(fr => fr.Requester)
            .Include(fr => fr.Receiver)
            .ApplySearchFilter(
                searchPhraseLower,
                FriendRequestSpecifications.SearchByReceiverUsername(searchPhraseLower!))
            .ApplySorting(
                dto.PagedQuery.SortBy,
                dto.PagedQuery.SortDirection,
                FriendRequestSortingSelectors.ForSentRequests(),
                defaultSort: fr => fr.CreatedAt)
            .ApplyPaging(dto.PagedQuery);

        var friendRequests = await dataQuery.ToListAsync(cancellationToken);
        var mappedRequests = friendRequests.Select(fr => friendRequestMapper.MapToReadFriendRequestDto(fr)!).ToList();
        
        return new PagedResult<ReadFriendRequestDto>(mappedRequests, totalCount, dto.PagedQuery.PageSize, dto.PagedQuery.PageNumber);
    }

    public async Task<PagedResult<ReadFriendDto>> GetFriends(Guid currentUserId, PagedQuery query, CancellationToken cancellationToken)
    {
        logger.LogInformation("Fetching friends for user {CurrentUserId}", currentUserId);
        var searchPhraseLower = query.SearchPhrase?.ToLower();
        
        var baseQuery = dbContext.FriendRequests
            .AsNoTracking()
            .Where(FriendRequestSpecifications.IsFriendshipWithUser(currentUserId))
            .Include(fr => fr.Requester)
            .Include(fr => fr.Receiver)
            .ApplySearchFilter(
                searchPhraseLower,
                fr => (fr.RequesterId == currentUserId && fr.Receiver.UserName!.ToLower().Contains(searchPhraseLower!)) ||
                      (fr.ReceiverId == currentUserId && fr.Requester.UserName!.ToLower().Contains(searchPhraseLower!)))
            .ApplySorting(
                query.SortBy,
                query.SortDirection,
                FriendRequestSortingSelectors.ForFriends(currentUserId),
                defaultSort: fr => fr.RequesterId == currentUserId ? fr.Receiver.UserName! : fr.Requester.UserName!);

        var totalCount = await baseQuery.CountAsync(cancellationToken);

        var friendRequests = await baseQuery
            .ApplyPaging(query)
            .ToListAsync(cancellationToken);

        var mappedFriends = friendRequests.Select(fr => friendRequestMapper.MapToReadFriendDto(fr, currentUserId)!).ToList();
        return new PagedResult<ReadFriendDto>(mappedFriends, totalCount, query.PageSize, query.PageNumber);
    }
}