using Microsoft.EntityFrameworkCore;
using MultiplayerGameBackend.Domain.Entities;

namespace MultiplayerGameBackend.Infrastructure.Persistence;

public class MultiplayerGameDbContext(DbContextOptions<MultiplayerGameDbContext> options)
    : DbContext(options)
{
    public DbSet<Item> Items { get; set; }
    
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        
        modelBuilder.Entity<Item>(entity =>
        {
            entity.Property(e => e.Name)
                .HasMaxLength(50);

            entity.Property(e => e.Description)
                .HasMaxLength(255);
        });
    }
}