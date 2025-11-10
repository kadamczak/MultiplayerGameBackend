using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Infrastructure;
using MultiplayerGameBackend.Domain.Entities;

namespace MultiplayerGameBackend.Application.Interfaces;

public interface IMultiplayerGameDbContext
{
    DbSet<Item> Items { get; }
    DbSet<User> Users { get; }
    DbSet<UserItem> UserItems { get; }
    DbSet<RefreshToken> RefreshTokens { get; }
    DbSet<InGameMerchant> InGameMerchants { get; }
    DbSet<MerchantItemOffer> MerchantItemOffers { get; }
    
    // DatabaseFacade Database { get; }
    // EntityEntry<TEntity> Entry<TEntity>(TEntity entity) where TEntity : class;
    
    Task<int> SaveChangesAsync(CancellationToken cancellationToken);
}