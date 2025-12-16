using Microsoft.AspNetCore.Identity;
using MultiplayerGameBackend.Domain.Constants;
using MultiplayerGameBackend.Domain.Entities;
using MultiplayerGameBackend.Infrastructure.Persistence;
using MultiplayerGameBackend.Tests.Shared.Factories;

namespace MultiplayerGameBackend.Tests.Shared.Helpers;

/// <summary>
/// Helper class for creating and saving test entities to the database.
/// Use this for integration tests that need to persist data.
/// </summary>
public static class DatabaseHelper
{
    #region Item Helpers

    /// <summary>
    /// Creates and saves an item to the database
    /// </summary>
    public static async Task<Item> CreateAndSaveItem(
        MultiplayerGameDbContext context,
        string name = "Test Item",
        string type = ItemTypes.Consumable,
        string? description = null,
        string? thumbnailUrl = null)
    {
        var item = TestEntityFactory.CreateItem(name, type, description, thumbnailUrl);
        context.Items.Add(item);
        await context.SaveChangesAsync();
        return item;
    }

    /// <summary>
    /// Creates and saves head and body items to the database
    /// </summary>
    public static async Task<(Item headItem, Item bodyItem)> CreateAndSaveHeadAndBodyItems(
        MultiplayerGameDbContext context)
    {
        var (headItem, bodyItem) = TestEntityFactory.CreateHeadAndBodyItems();
        context.Items.AddRange(headItem, bodyItem);
        await context.SaveChangesAsync();
        return (headItem, bodyItem);
    }

    #endregion

    #region User Helpers

    /// <summary>
    /// Creates and saves a user with UserManager
    /// </summary>
    public static async Task<User> CreateAndSaveUser(
        UserManager<User> userManager,
        string userName = "testuser",
        string email = "test@example.com",
        string password = "Password123!",
        int balance = 0)
    {
        var user = TestEntityFactory.CreateUser(userName, email, balance: balance);
        await userManager.CreateAsync(user, password);
        return user;
    }

    /// <summary>
    /// Creates and saves a user with a specific role
    /// </summary>
    public static async Task<User> CreateAndSaveUserWithRole(
        UserManager<User> userManager,
        RoleManager<IdentityRole<Guid>> roleManager,
        string userName,
        string email,
        string password,
        string roleName,
        int balance = 0)
    {
        // Ensure role exists
        if (!await roleManager.RoleExistsAsync(roleName))
        {
            var role = TestEntityFactory.CreateRole(roleName);
            await roleManager.CreateAsync(role);
        }

        var user = await CreateAndSaveUser(userManager, userName, email, password, balance);
        await userManager.AddToRoleAsync(user, roleName);
        return user;
    }

    /// <summary>
    /// Updates a user's balance in the database
    /// </summary>
    public static async Task UpdateUserBalance(
        MultiplayerGameDbContext context,
        Guid userId,
        int balance)
    {
        var user = await context.Users.FindAsync(userId);
        if (user != null)
        {
            user.Balance = balance;
            await context.SaveChangesAsync();
        }
    }

    #endregion

    #region UserItem Helpers

    /// <summary>
    /// Creates and saves a user item to the database
    /// </summary>
    public static async Task<UserItem> CreateAndSaveUserItem(
        MultiplayerGameDbContext context,
        Guid userId,
        int itemId)
    {
        var userItem = TestEntityFactory.CreateUserItem(userId, itemId);
        context.UserItems.Add(userItem);
        await context.SaveChangesAsync();
        return userItem;
    }

    #endregion

    #region UserCustomization Helpers

    /// <summary>
    /// Creates and saves a user customization to the database
    /// </summary>
    public static async Task<UserCustomization> CreateAndSaveUserCustomization(
        MultiplayerGameDbContext context,
        Guid userId,
        string? headColor = null,
        string? bodyColor = null,
        string? tailColor = null,
        string? eyeColor = null,
        string? wingColor = null,
        string? hornColor = null,
        string? markingsColor = null)
    {
        var customization = TestEntityFactory.CreateUserCustomization(
            userId, headColor, bodyColor, tailColor, eyeColor, wingColor, hornColor, markingsColor);
        context.UserCustomizations.Add(customization);
        await context.SaveChangesAsync();
        return customization;
    }

    #endregion

    #region Merchant Helpers

    /// <summary>
    /// Creates and saves a merchant to the database
    /// </summary>
    public static async Task<InGameMerchant> CreateAndSaveMerchant(
        MultiplayerGameDbContext context)
    {
        var merchant = TestEntityFactory.CreateMerchant();
        context.InGameMerchants.Add(merchant);
        await context.SaveChangesAsync();
        return merchant;
    }

    /// <summary>
    /// Creates and saves a merchant item offer to the database
    /// </summary>
    public static async Task<MerchantItemOffer> CreateAndSaveMerchantItemOffer(
        MultiplayerGameDbContext context,
        int merchantId,
        int itemId,
        int price)
    {
        var offer = TestEntityFactory.CreateMerchantItemOffer(merchantId, itemId, price);
        context.MerchantItemOffers.Add(offer);
        await context.SaveChangesAsync();
        return offer;
    }

    /// <summary>
    /// Creates and saves a merchant with an item and offer to the database
    /// </summary>
    public static async Task<(InGameMerchant merchant, Item item, MerchantItemOffer offer)> CreateAndSaveMerchantWithOffer(
        MultiplayerGameDbContext context,
        int price,
        string itemName = "Test Item",
        string itemType = ItemTypes.Consumable)
    {
        var merchant = await CreateAndSaveMerchant(context);
        var item = await CreateAndSaveItem(context, itemName, itemType);
        var offer = await CreateAndSaveMerchantItemOffer(context, merchant.Id, item.Id, price);
        return (merchant, item, offer);
    }

    #endregion

    #region UserItemOffer Helpers

    /// <summary>
    /// Creates and saves a user item offer to the database
    /// </summary>
    public static async Task<UserItemOffer> CreateAndSaveUserItemOffer(
        MultiplayerGameDbContext context,
        Guid sellerId,
        Guid userItemId,
        int price,
        Guid? buyerId = null,
        DateTime? boughtAt = null)
    {
        var offer = TestEntityFactory.CreateUserItemOffer(sellerId, userItemId, price, buyerId, boughtAt);
        context.UserItemOffers.Add(offer);
        await context.SaveChangesAsync();
        return offer;
    }

    #endregion

    #region Role Helpers

    /// <summary>
    /// Creates and saves a role to the database
    /// </summary>
    public static async Task<IdentityRole<Guid>> CreateAndSaveRole(
        RoleManager<IdentityRole<Guid>> roleManager,
        string roleName)
    {
        var role = TestEntityFactory.CreateRole(roleName);
        await roleManager.CreateAsync(role);
        return role;
    }

    #endregion
}

