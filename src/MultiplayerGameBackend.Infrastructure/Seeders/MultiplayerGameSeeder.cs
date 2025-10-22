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
        }
    }

    private static IEnumerable<IdentityRole<Guid>> GetRoles() => [
        new IdentityRole<Guid>(UserRoles.User) { NormalizedName = UserRoles.User.ToUpper() },
        new IdentityRole<Guid>(UserRoles.Admin) { NormalizedName = UserRoles.Admin.ToUpper() }
    ];

    private static IEnumerable<Item> GetItems() => [
        new Item() { Id = 1, Name = "Fire Staff", Description = "Burns enemies."},
        new Item() { Id = 2, Name = "Ice Staff", Description = "Freezes enemies." }
    ];
}