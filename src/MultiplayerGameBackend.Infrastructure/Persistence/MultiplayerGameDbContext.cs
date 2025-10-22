using Microsoft.EntityFrameworkCore;
using MultiplayerGameBackend.Application.Interfaces;
using MultiplayerGameBackend.Domain.Entities;

namespace MultiplayerGameBackend.Infrastructure.Persistence;

public class MultiplayerGameDbContext(DbContextOptions<MultiplayerGameDbContext> options)
    : DbContext(options), IMultiplayerGameDbContext
{
    public DbSet<Item> Items { get; set; }
    public DbSet<User> Users { get; set; }
    public DbSet<UserItem> UserItems { get; set; }
    
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        
        ConfigureItemEntities(modelBuilder);
        ConfigureUserEntities(modelBuilder);
        ConfigureUserItemEntities(modelBuilder);
    }

    private static void ConfigureUserItemEntities(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<UserItem>(entity =>
        {
            entity.HasKey(e => e.Id);

            entity.Property(e => e.ObtainedAt)
                .IsRequired();

            entity.HasOne(ui => ui.User)
                .WithMany(u => u.UserItems)
                .HasForeignKey(ui => ui.UserId);

            entity.HasOne(e => e.Item)
                .WithMany(e => e.UserItems)
                .HasForeignKey(e => e.ItemId);
        });
    }

    private static void ConfigureUserEntities(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<User>(entity =>
        {
            entity.Property(e => e.UserName)
                .HasMaxLength(30);

            entity.HasIndex(e => e.UserName)
                .IsUnique();

            entity.Property(e => e.Email)
                .HasMaxLength(100);

            entity.HasIndex(e => e.Email)
                .IsUnique();

            entity.Property(e => e.PasswordHash)
                .HasMaxLength(512);

            entity.HasMany(u => u.UserItems)
                .WithOne(ui => ui.User)
                .HasForeignKey(ui => ui.UserId)
                .OnDelete(DeleteBehavior.Cascade);;
        });
    }

    private static void ConfigureItemEntities(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Item>(entity =>
        {
            entity.Property(e => e.Name)
                .HasMaxLength(50);
            
            entity.HasIndex(e => e.Name)
                .IsUnique();

            entity.Property(e => e.Description)
                .HasMaxLength(256);

            entity.HasMany(i => i.UserItems)
                .WithOne(ui => ui.Item)
                .HasForeignKey(ui => ui.ItemId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
}