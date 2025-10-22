using Microsoft.EntityFrameworkCore;
using MultiplayerGameBackend.Domain.Entities;

namespace MultiplayerGameBackend.Application.Interfaces;

public interface IMultiplayerGameDbContext
{
    DbSet<Item> Items { get; }
    DbSet<User> Users { get; }
    DbSet<UserItem> UserItems { get; }
    
    Task<int> SaveChangesAsync(CancellationToken cancellationToken);
}