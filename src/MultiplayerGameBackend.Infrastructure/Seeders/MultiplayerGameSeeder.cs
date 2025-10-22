using Microsoft.EntityFrameworkCore;
using MultiplayerGameBackend.Domain.Entities;
using MultiplayerGameBackend.Infrastructure.Persistence;

namespace MultiplayerGameBackend.Infrastructure.Seeders;

internal class MultiplayerGameSeeder(MultiplayerGameDbContext dbContext) : IMultiplayerGameSeeder
{
    public async Task Seed()
    {
        if (dbContext.Database.GetPendingMigrations().Any())
        {
            await dbContext.Database.MigrateAsync();
        }
        
        if (await dbContext.Database.CanConnectAsync())
        {
            if (!dbContext.Items.Any())
            {
                var items = GetItems();
                dbContext.Items.AddRange(items);
                await dbContext.SaveChangesAsync();
            }
        }
    }

    private IEnumerable<Item> GetItems()
    {
        List<Item> items = [
            new()
            {
                Id = 1,
                Name = "Fire Staff",
                Description = "Burns enemies.",
            },
            new()
            {
                Id = 2,
                Name = "Ice Staff",
                Description = "Freezes enemies."
            }
        ];
        
        return items;
    }
}