namespace MultiplayerGameBackend.Application.Common;

/// <summary>
/// Constants for localization resource keys.
/// These keys correspond to entries in SharedResources.resx files.
/// </summary>
public static class LocalizationKeys
{
    public static class Errors
    {
        // General
        public const string SomethingWentWrong = "Error.SomethingWentWrong";
        public const string OneOrMoreConflicts = "Error.OneOrMoreConflicts";
        public const string OneOrMoreErrors = "Error.OneOrMoreErrors";
        
        // Authentication/Authorization
        public const string UserMustBeAuthenticated = "Error.UserMustBeAuthenticated";
        public const string InvalidCredentials = "Error.InvalidCredentials";
        public const string InvalidToken = "Error.InvalidToken";
        public const string RefreshTokenExpired = "Error.RefreshTokenExpired";
        public const string UserAlreadyExists = "Error.UserAlreadyExists";
        public const string EmailAlreadyConfirmed = "Error.EmailAlreadyConfirmed";
        public const string EmailNotConfirmed = "Error.EmailNotConfirmed";
        public const string TwoFactorNotEnabled = "Error.TwoFactorNotEnabled";
        public const string PasswordDoesNotMeetRequirements = "Error.PasswordDoesNotMeetRequirements";
        
        // Friend Requests
        public const string CannotSendFriendRequestToYourself = "Error.CannotSendFriendRequestToYourself";
        public const string AlreadyFriends = "Error.AlreadyFriends";
        public const string PendingFriendRequestExists = "Error.PendingFriendRequestExists";
        public const string MaxPendingRequestsReached = "Error.MaxPendingRequestsReached";
        public const string MaxFriendsReached = "Error.MaxFriendsReached";
        public const string RequesterMaxFriendsReached = "Error.RequesterMaxFriendsReached";
        public const string FriendRequestAlreadyResponded = "Error.FriendRequestAlreadyResponded";
        public const string CanOnlyAcceptRequestsSentToYou = "Error.CanOnlyAcceptRequestsSentToYou";
        public const string CanOnlyRejectRequestsSentToYou = "Error.CanOnlyRejectRequestsSentToYou";
        public const string CanOnlyCancelPendingRequests = "Error.CanOnlyCancelPendingRequests";
        public const string InvalidFriendUserId = "Error.InvalidFriendUserId";
        
        // Items
        public const string NoFileUploaded = "Error.NoFileUploaded";
        
        // User Item Offers
        public const string CannotBuyOwnOffer = "Error.CannotBuyOwnOffer";
        public const string InsufficientBalance = "Error.InsufficientBalance";
        public const string CannotUpdateSoldOffer = "Error.CannotUpdateSoldOffer";
        public const string CannotCancelSoldOffer = "Error.CannotCancelSoldOffer";
        
        // Merchant Item Offers
        public const string CannotAffordMerchantOffer = "Error.CannotAffordMerchantOffer";
        
        // Not Found
        public const string NotFound = "Error.NotFound";
        public const string UserNotFound = "Error.UserNotFound";
        public const string ReceiverNotFound = "Error.ReceiverNotFound";
    }
    
    public static class Validation
    {
        public const string Required = "Validation.Required";
        public const string InvalidEmail = "Validation.InvalidEmail";
        public const string StringLength = "Validation.StringLength";
        public const string MinLength = "Validation.MinLength";
        public const string MaxLength = "Validation.MaxLength";
        public const string Range = "Validation.Range";
        public const string InvalidValue = "Validation.InvalidValue";
    }
}

