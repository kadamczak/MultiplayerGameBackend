using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using MultiplayerGameBackend.Domain.Constants;
using MultiplayerGameBackend.Domain.Entities;
using MultiplayerGameBackend.Infrastructure.Persistence;

namespace MultiplayerGameBackend.Infrastructure.Seeders;

internal class MultiplayerGameSeeder(MultiplayerGameDbContext dbContext) : IMultiplayerGameSeeder
{
    public async Task Seed()
    {
        if (dbContext.Database.GetPendingMigrations().Any())
            await dbContext.Database.MigrateAsync();
        
        if (await dbContext.Database.CanConnectAsync())
        {
            if(!dbContext.Roles.Any())
            {
                var roles = GetRoles();
                dbContext.Roles.AddRange(roles);
                await dbContext.SaveChangesAsync();
            }
            
            if (!dbContext.Items.Any())
            {
                var items = GetItems();
                dbContext.Items.AddRange(items);
                await dbContext.SaveChangesAsync();
            }

            if (!dbContext.InGameMerchants.Any())
            {
                var merchants = GetMerchants();
                dbContext.InGameMerchants.AddRange(merchants);
                await dbContext.SaveChangesAsync();
            }
        }
    }

    private static IEnumerable<IdentityRole<Guid>> GetRoles() => [
        new IdentityRole<Guid>(UserRoles.User) { NormalizedName = UserRoles.User.ToUpper() },
        new IdentityRole<Guid>(UserRoles.Admin) { NormalizedName = UserRoles.Admin.ToUpper() }
    ];

    private static IEnumerable<Item> GetItems() => [
        new Item() { Id = 1, Name = "Mage Hat", Description = "Stylish!", Type = ItemTypes.EquippableOnHead },
        new Item() { Id = 2, Name = "Headphones", Description = "High-fidelity!", Type = ItemTypes.EquippableOnHead},
        new Item() { Id = 3, Name = "Tome of Magic", Description = "Smart!", Type = ItemTypes.Consumable},
        new Item() { Id = 4, Name = "Leg Armor", Description = "Fierce!", Type = ItemTypes.EquippableOnBody},
        new Item() { Id = 5, Name = "Helmet", Description = "Sturdy!", Type = ItemTypes.EquippableOnHead},
    ];
    
    private static IEnumerable<InGameMerchant> GetMerchants() => [
        new InGameMerchant()
        {
            Id = 1,
            ItemOffers = [
                new MerchantItemOffer() { ItemId = 1, Price = 100 },
                new MerchantItemOffer() { ItemId = 3, Price = 150 },
                new MerchantItemOffer() { ItemId = 4, Price = 170 },
                new MerchantItemOffer() { ItemId = 5, Price = 280 },
            ]
        }
    ];
}