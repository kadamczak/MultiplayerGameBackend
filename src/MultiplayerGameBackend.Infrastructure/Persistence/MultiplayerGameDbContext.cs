using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using MultiplayerGameBackend.Application.Interfaces;
using MultiplayerGameBackend.Domain.Entities;

namespace MultiplayerGameBackend.Infrastructure.Persistence;

public class MultiplayerGameDbContext
    : IdentityDbContext<User, IdentityRole<Guid>, Guid>, IMultiplayerGameDbContext
{
    public MultiplayerGameDbContext(DbContextOptions<MultiplayerGameDbContext> options)
        : base(options)
    {
    }
    
    public DbSet<Item> Items { get; set; }
    public new DbSet<User> Users { get; set; }
    public DbSet<UserItem> UserItems { get; set; }
    public DbSet<RefreshToken> RefreshTokens { get; set; }
    
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        
        ConfigureItemEntities(modelBuilder);
        ConfigureUserEntities(modelBuilder);
        ConfigureUserItemEntities(modelBuilder);
        ConfigureRefreshTokenEntities(modelBuilder);
    }

    private static void ConfigureItemEntities(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Item>(entity =>
        {
            entity.HasIndex(e => e.Name)
                .IsUnique();
            
            entity.HasMany(i => i.UserItems)
                .WithOne(ui => ui.Item)
                .HasForeignKey(ui => ui.ItemId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
    
    private static void ConfigureUserEntities(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<User>(entity =>
        {
            entity.Property(e => e.UserName)
                .IsRequired()
                .HasMaxLength(User.Constraints.UserNameMaxLength);

            entity.HasIndex(e => e.UserName)
                .IsUnique();
            
            entity.Property(e => e.NormalizedUserName)
                .IsRequired()
                .HasMaxLength(User.Constraints.UserNameMaxLength);

            entity.Property(e => e.Email)
                .IsRequired()
                .HasMaxLength(User.Constraints.EmailMaxLength);

            entity.HasIndex(e => e.Email)
                .IsUnique();
            
            entity.Property(e => e.NormalizedEmail)
                .IsRequired()
                .HasMaxLength(User.Constraints.EmailMaxLength);

            entity.Property(e => e.PasswordHash)
                .IsRequired()
                .HasMaxLength(User.Constraints.PasswordHashMaxLength);

            entity.HasMany(u => u.UserItems)
                .WithOne(ui => ui.User)
                .HasForeignKey(ui => ui.UserId)
                .OnDelete(DeleteBehavior.Cascade);;
            
            entity.HasMany(u => u.RefreshTokens)
                .WithOne(ui => ui.User)
                .HasForeignKey(ui => ui.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
    
    private static void ConfigureUserItemEntities(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<UserItem>(entity =>
        {
            entity.HasKey(e => e.Id);

            entity.HasOne(ui => ui.User)
                .WithMany(u => u.UserItems)
                .HasForeignKey(ui => ui.UserId);

            entity.HasOne(e => e.Item)
                .WithMany(e => e.UserItems)
                .HasForeignKey(e => e.ItemId);
        });
    }
    
    private void ConfigureRefreshTokenEntities(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<RefreshToken>(entity =>
        {
            entity.HasKey(e => e.Id);
            
            entity.HasIndex(e => e.TokenHash)
                .IsUnique();
        });
    }
}