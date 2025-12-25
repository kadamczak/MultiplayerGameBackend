using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using MultiplayerGameBackend.Application.Common;
using MultiplayerGameBackend.Application.Common.Mappings;
using MultiplayerGameBackend.Application.FriendRequests.Requests;
using MultiplayerGameBackend.Application.FriendRequests.Responses;
using MultiplayerGameBackend.Application.FriendRequests.Specifications;
using MultiplayerGameBackend.Application.Interfaces;
using MultiplayerGameBackend.Domain.Constants;
using MultiplayerGameBackend.Domain.Entities;
using MultiplayerGameBackend.Domain.Exceptions;

namespace MultiplayerGameBackend.Application.FriendRequests;

public class FriendRequestService(
    ILogger<FriendRequestService> logger,
    IMultiplayerGameDbContext dbContext,
    FriendRequestMapper friendRequestMapper,
    ILocalizationService localizationService) : IFriendRequestService
{
    public async Task<Guid> SendFriendRequest(Guid userId, SendFriendRequestDto dto, CancellationToken cancellationToken)
    {
        logger.LogInformation("User {CurrentUserId} is sending friend request to {ReceiverId}", userId, dto.ReceiverId);

        if (userId == dto.ReceiverId)
            throw new BadRequest(localizationService.GetString(LocalizationKeys.Errors.CannotSendFriendRequestToYourself));

        var receiver = await dbContext.Users
            .FirstOrDefaultAsync(u => u.Id == dto.ReceiverId, cancellationToken);

        if (receiver is null)
            throw new NotFoundException(nameof(User), nameof(User.Id), "ID", dto.ReceiverId.ToString());

        // Check if already friends
        var existingAcceptedRequest = await dbContext.FriendRequests
            .AnyAsync(FriendRequestSpecifications.AreFriends(userId, receiver.Id), cancellationToken);

        if (existingAcceptedRequest)
            throw new ConflictException(new Dictionary<string, string[]>
            {
                { "ReceiverId", new[] { localizationService.GetString(LocalizationKeys.Errors.AlreadyFriends) } }
            });

        // Check if there's already a pending request
        var existingPendingRequest = await dbContext.FriendRequests
            .FirstOrDefaultAsync(FriendRequestSpecifications.HavePendingRequest(userId, dto.ReceiverId), cancellationToken);

        if (existingPendingRequest is not null)
        {
            // If the other user already sent a request to current user, automatically accept it
            if (existingPendingRequest.RequesterId == dto.ReceiverId && existingPendingRequest.ReceiverId == userId)
            {
                existingPendingRequest.Status = FriendRequestStatuses.Accepted;
                existingPendingRequest.RespondedAt = DateTime.UtcNow;
                await dbContext.SaveChangesAsync(cancellationToken);
                logger.LogInformation("Automatically accepted existing friend request {RequestId}", existingPendingRequest.Id);
                return existingPendingRequest.Id;
            }

            throw new ConflictException(new Dictionary<string, string[]>
            {
                { "ReceiverId", new[] { localizationService.GetString(LocalizationKeys.Errors.PendingFriendRequestExists) } }
            });
        }

        // Check pending request limit
        var pendingRequestCount = await dbContext.FriendRequests
            .CountAsync(FriendRequestSpecifications.IsPendingRequestSentBy(userId), cancellationToken);

        if (pendingRequestCount >= FriendRequest.Constraints.MaxPendingRequestsPerUser)
            throw new BadRequest(localizationService.GetString(LocalizationKeys.Errors.MaxPendingRequestsReached, FriendRequest.Constraints.MaxPendingRequestsPerUser));

        // Check friend limit for both users
        var friendCount = await dbContext.FriendRequests
            .CountAsync(FriendRequestSpecifications.IsFriendshipWithUser(userId), cancellationToken);

        if (friendCount >= FriendRequest.Constraints.MaxFriendsPerUser)
            throw new BadRequest(localizationService.GetString(LocalizationKeys.Errors.MaxFriendsReached, FriendRequest.Constraints.MaxFriendsPerUser));

        var friendRequest = new FriendRequest
        {
            RequesterId = userId,
            ReceiverId = dto.ReceiverId,
            Status = FriendRequestStatuses.Pending,
            CreatedAt = DateTime.UtcNow
        };

        dbContext.FriendRequests.Add(friendRequest);
        await dbContext.SaveChangesAsync(cancellationToken);

        logger.LogInformation("Friend request {RequestId} created successfully", friendRequest.Id);
        return friendRequest.Id;
    }

    public async Task AcceptFriendRequest(Guid userId, Guid requestId, CancellationToken cancellationToken)
    {
        logger.LogInformation("User {CurrentUserId} is accepting friend request {RequestId}", userId, requestId);

        var friendRequest = await dbContext.FriendRequests
            .FirstOrDefaultAsync(fr => fr.Id == requestId, cancellationToken);

        if (friendRequest is null)
            throw new NotFoundException(nameof(FriendRequest), nameof(FriendRequest.Id), "ID", requestId.ToString());

        if (friendRequest.ReceiverId != userId)
            throw new ForbidException(localizationService.GetString(LocalizationKeys.Errors.CanOnlyAcceptRequestsSentToYou));

        if (friendRequest.Status != FriendRequestStatuses.Pending)
            throw new BadRequest(localizationService.GetString(LocalizationKeys.Errors.FriendRequestAlreadyResponded));

        // Check friend count limit for receiver
        var receiverFriendCount = await dbContext.FriendRequests
            .CountAsync(FriendRequestSpecifications.IsFriendshipWithUser(userId), cancellationToken);

        if (receiverFriendCount >= FriendRequest.Constraints.MaxFriendsPerUser)
            throw new BadRequest(localizationService.GetString(LocalizationKeys.Errors.MaxFriendsReached, FriendRequest.Constraints.MaxFriendsPerUser));

        // Check friend count limit for requester
        var requesterFriendCount = await dbContext.FriendRequests
            .CountAsync(FriendRequestSpecifications.IsFriendshipWithUser(friendRequest.RequesterId), cancellationToken);

        if (requesterFriendCount >= FriendRequest.Constraints.MaxFriendsPerUser)
            throw new BadRequest(localizationService.GetString(LocalizationKeys.Errors.RequesterMaxFriendsReached, FriendRequest.Constraints.MaxFriendsPerUser));

        friendRequest.Status = FriendRequestStatuses.Accepted;
        friendRequest.RespondedAt = DateTime.UtcNow;

        await dbContext.SaveChangesAsync(cancellationToken);
        logger.LogInformation("Friend request {RequestId} accepted successfully", requestId);
    }

    public async Task RejectFriendRequest(Guid userId, Guid requestId, CancellationToken cancellationToken)
    {
        logger.LogInformation("User {CurrentUserId} is rejecting friend request {RequestId}", userId, requestId);

        var friendRequest = await dbContext.FriendRequests
            .FirstOrDefaultAsync(fr => fr.Id == requestId, cancellationToken);

        if (friendRequest is null)
            throw new NotFoundException(nameof(FriendRequest), nameof(FriendRequest.Id), "ID", requestId.ToString());

        if (friendRequest.ReceiverId != userId)
            throw new ForbidException(localizationService.GetString(LocalizationKeys.Errors.CanOnlyRejectRequestsSentToYou));

        if (friendRequest.Status != FriendRequestStatuses.Pending)
            throw new BadRequest(localizationService.GetString(LocalizationKeys.Errors.FriendRequestAlreadyResponded));

        dbContext.FriendRequests.Remove(friendRequest);
        await dbContext.SaveChangesAsync(cancellationToken);
        logger.LogInformation("Friend request {RequestId} rejected and deleted successfully", requestId);
    }

    public async Task CancelFriendRequest(Guid userId, Guid requestId, CancellationToken cancellationToken)
    {
        logger.LogInformation("User {CurrentUserId} is canceling friend request {RequestId}", userId, requestId);

        var friendRequest = await dbContext.FriendRequests
            .FirstOrDefaultAsync(fr => fr.Id == requestId, cancellationToken);

        if (friendRequest is null)
            throw new NotFoundException(nameof(FriendRequest), nameof(FriendRequest.Id), "ID", requestId.ToString());

        if (friendRequest.RequesterId != userId)
            throw new ForbidException("You can only cancel friend requests that you sent.");

        if (friendRequest.Status != FriendRequestStatuses.Pending)
            throw new BadRequest(localizationService.GetString(LocalizationKeys.Errors.CanOnlyCancelPendingRequests));

        dbContext.FriendRequests.Remove(friendRequest);
        await dbContext.SaveChangesAsync(cancellationToken);
        logger.LogInformation("Friend request {RequestId} canceled successfully", requestId);
    }

    public async Task RemoveFriend(Guid userId, Guid friendUserId, CancellationToken cancellationToken)
    {
        logger.LogInformation("User {CurrentUserId} is removing friend {FriendUserId}", userId, friendUserId);

        if (userId == friendUserId)
            throw new BadRequest(localizationService.GetString(LocalizationKeys.Errors.InvalidFriendUserId));

        var friendRequest = await dbContext.FriendRequests
            .FirstOrDefaultAsync(FriendRequestSpecifications.AreFriends(userId, friendUserId), cancellationToken);

        if (friendRequest is null)
            throw new NotFoundException(nameof(User), nameof(User.Id), "user ID", friendUserId.ToString());

        dbContext.FriendRequests.Remove(friendRequest);
        await dbContext.SaveChangesAsync(cancellationToken);
        logger.LogInformation("Friend removed successfully");
    }

    public async Task<PagedResult<ReadFriendRequestDto>> GetReceivedFriendRequests(Guid userId, GetFriendRequestsDto dto, CancellationToken cancellationToken)
    {
        logger.LogInformation("Fetching received friend requests for user {CurrentUserId}", userId);
        var searchPhraseLower = dto.PagedQuery.SearchPhrase?.ToLower();
        
        // Build base query for counting (without includes for performance)
        var countQuery = dbContext.FriendRequests
            .AsNoTracking()
            .Where(FriendRequestSpecifications.IsPendingRequestReceivedBy(userId))
            .ApplySearchFilter(
                searchPhraseLower,
                FriendRequestSpecifications.SearchByRequesterUsername(searchPhraseLower!));

        var totalCount = await countQuery.CountAsync(cancellationToken);
        
        // Build query for fetching data (with includes and sorting)
        var dataQuery = dbContext.FriendRequests
            .AsNoTracking()
            .Where(FriendRequestSpecifications.IsPendingRequestReceivedBy(userId))
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

    public async Task<PagedResult<ReadFriendRequestDto>> GetSentFriendRequests(Guid userId, GetFriendRequestsDto dto, CancellationToken cancellationToken)
    {
        logger.LogInformation("Fetching sent friend requests for user {CurrentUserId}", userId);
        var searchPhraseLower = dto.PagedQuery.SearchPhrase?.ToLower();
        
        // Build base query for counting (without includes for performance)
        var countQuery = dbContext.FriendRequests
            .AsNoTracking()
            .Where(FriendRequestSpecifications.IsPendingRequestSentBy(userId))
            .ApplySearchFilter(
                searchPhraseLower,
                FriendRequestSpecifications.SearchByReceiverUsername(searchPhraseLower!));

        var totalCount = await countQuery.CountAsync(cancellationToken);
        
        // Build query for fetching data (with includes and sorting)
        var dataQuery = dbContext.FriendRequests
            .AsNoTracking()
            .Where(FriendRequestSpecifications.IsPendingRequestSentBy(userId))
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

    public async Task<PagedResult<ReadFriendDto>> GetFriends(Guid currentUserId, GetFriendsDto dto, CancellationToken cancellationToken)
    {
        logger.LogInformation("Fetching friends for user {CurrentUserId}", currentUserId);
        var searchPhraseLower = dto.PagedQuery.SearchPhrase?.ToLower();
        
        // Build base query for counting (without includes for performance)
        var countQuery = dbContext.FriendRequests
            .AsNoTracking()
            .Where(FriendRequestSpecifications.IsFriendshipWithUser(currentUserId))
            .ApplySearchFilter(
                searchPhraseLower,
                FriendRequestSpecifications.SearchByOtherUserName(searchPhraseLower, currentUserId));

        var totalCount = await countQuery.CountAsync(cancellationToken);
        
        // Build query for fetching data (with includes and sorting)
        var dataQuery = dbContext.FriendRequests
            .AsNoTracking()
            .Where(FriendRequestSpecifications.IsFriendshipWithUser(currentUserId))
            .Include(fr => fr.Requester)
            .Include(fr => fr.Receiver)
            .ApplySearchFilter(
                searchPhraseLower,
                FriendRequestSpecifications.SearchByOtherUserName(searchPhraseLower, currentUserId))
            .ApplySorting(
                dto.PagedQuery.SortBy,
                dto.PagedQuery.SortDirection,
                FriendRequestSortingSelectors.ForFriends(currentUserId),
                defaultSort: FriendRequestSpecifications.GetOtherUserName(currentUserId))
            .ApplyPaging(dto.PagedQuery);

        var friendRequests = await dataQuery.ToListAsync(cancellationToken);
        var mappedFriends = friendRequests.Select(fr => friendRequestMapper.MapToReadFriendDto(fr, currentUserId)!).ToList();
        
        return new PagedResult<ReadFriendDto>(mappedFriends, totalCount, dto.PagedQuery.PageSize, dto.PagedQuery.PageNumber);
    }
}