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
        public const string PasswordIncorrect = "Error.PasswordIncorrect";
        
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
        public const string CanOnlyCancelOwnRequests = "Error.CanOnlyCancelOwnRequests";
        public const string InvalidFriendUserId = "Error.InvalidFriendUserId";
        
        // Items
        public const string NoFileUploaded = "Error.NoFileUploaded";
        public const string CannotEquipItemsNotOwned = "Error.CannotEquipItemsNotOwned";
        
        // User Item Offers
        public const string CannotBuyOwnOffer = "Error.CannotBuyOwnOffer";
        public const string InsufficientBalance = "Error.InsufficientBalance";
        public const string CannotUpdateSoldOffer = "Error.CannotUpdateSoldOffer";
        public const string CannotCancelSoldOffer = "Error.CannotCancelSoldOffer";
        public const string DoNotOwnItem = "Error.DoNotOwnItem";
        public const string DoNotOwnOffer = "Error.DoNotOwnOffer";
        
        // Merchant Item Offers
        public const string CannotAffordMerchantOffer = "Error.CannotAffordMerchantOffer";
        
        // Not Found
        public const string NotFound = "Error.NotFound";
        public const string UserNotFound = "Error.UserNotFound";
        public const string ReceiverNotFound = "Error.ReceiverNotFound";
        public const string ItemNotFound = "Error.ItemNotFound";
        public const string UserItemNotFound = "Error.UserItemNotFound";
        public const string UserItemOfferNotFound = "Error.UserItemOfferNotFound";
        public const string MerchantItemOfferNotFound = "Error.MerchantItemOfferNotFound";
        public const string MerchantNotFound = "Error.MerchantNotFound";
        public const string FriendRequestNotFound = "Error.FriendRequestNotFound";
        public const string RoleNotFound = "Error.RoleNotFound";
        public const string UserCustomizationNotFound = "Error.UserCustomizationNotFound";
        public const string ProfilePictureNotSet = "Error.ProfilePictureNotSet";
        
        // Conflicts
        public const string UsernameAlreadyExists = "Error.UsernameAlreadyExists";
        public const string EmailAlreadyExists = "Error.EmailAlreadyExists";
        public const string UserItemOfferAlreadyExists = "Error.UserItemOfferAlreadyExists";
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
        public const string SortByMustBeOneOf = "Validation.SortByMustBeOneOf";
        public const string PageSizeMustBeOneOf = "Validation.PageSizeMustBeOneOf";
        public const string SearchPhraseTooLong = "Validation.SearchPhraseTooLong";
        public const string PasswordsDoNotMatch = "Validation.PasswordsDoNotMatch";
        public const string InvalidPageNumber = "Validation.InvalidPageNumber";
        public const string CurrentPasswordRequired = "Validation.CurrentPasswordRequired";
        public const string NewPasswordRequired = "Validation.NewPasswordRequired";
        public const string PasswordLength = "Validation.PasswordLength";
        public const string PasswordMustContainUppercase = "Validation.PasswordMustContainUppercase";
        public const string PasswordMustContainLowercase = "Validation.PasswordMustContainLowercase";
        public const string PasswordMustContainDigit = "Validation.PasswordMustContainDigit";
        public const string UsernameRequired = "Validation.UsernameRequired";
        public const string UsernameLength = "Validation.UsernameLength";
        public const string EmailRequired = "Validation.EmailRequired";
        public const string PasswordRequired = "Validation.PasswordRequired";
        public const string ConfirmPasswordRequired = "Validation.ConfirmPasswordRequired";
        public const string RoleNameRequired = "Validation.RoleNameRequired";
        public const string NameRequired = "Validation.NameRequired";
        public const string NameLength = "Validation.NameLength";
        public const string DescriptionRequired = "Validation.DescriptionRequired";
        public const string DescriptionLength = "Validation.DescriptionLength";
        public const string TypeRequired = "Validation.TypeRequired";
        public const string SortDirectionRequired = "Validation.SortDirectionRequired";
    }
    
    public static class Entities
    {
        public const string User = "Entity.User";
        public const string Item = "Entity.Item";
        public const string UserItem = "Entity.UserItem";
        public const string UserItemOffer = "Entity.UserItemOffer";
        public const string MerchantItemOffer = "Entity.MerchantItemOffer";
        public const string InGameMerchant = "Entity.InGameMerchant";
        public const string FriendRequest = "Entity.FriendRequest";
        public const string Role = "Entity.Role";
    }
}

