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
    public DbSet<UserItemOffer> UserItemOffers { get; set; }
    public DbSet<UserCustomization> UserCustomizations { get; set; }
    public DbSet<InGameMerchant> InGameMerchants { get; set; }
    public DbSet<MerchantItemOffer> MerchantItemOffers { get; set; }
    public DbSet<RefreshToken> RefreshTokens { get; set; }
    
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        
        ConfigureItemEntities(modelBuilder);
        ConfigureUserEntities(modelBuilder);
        ConfigureUserItemEntities(modelBuilder);
        ConfigureUserItemOfferEntities(modelBuilder);
        ConfigureUserCustomizationEntities(modelBuilder);
        ConfigureInGameMerchantEntities(modelBuilder);
        ConfigureMerchantItemOfferEntities(modelBuilder);
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
            
            entity.HasMany(i => i.MerchantItemOffers)
                .WithOne(o => o.Item)
                .HasForeignKey(o => o.ItemId)
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

            entity.Property(e => e.Balance)
                .HasDefaultValue(User.Constraints.StartingBalance);

            entity.HasMany(u => u.UserItems)
                .WithOne(ui => ui.User)
                .HasForeignKey(ui => ui.UserId)
                .OnDelete(DeleteBehavior.Cascade);
            
            entity.HasMany(u => u.RefreshTokens)
                .WithOne(ui => ui.User)
                .HasForeignKey(ui => ui.UserId)
                .OnDelete(DeleteBehavior.Cascade);
            
            entity.HasOne(u => u.Customization)
                .WithOne(uc => uc.User)
                .HasForeignKey<UserCustomization>(uc => uc.UserId)
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
    
    private static void ConfigureUserItemOfferEntities(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<UserItemOffer>(entity =>
        {
            entity.HasKey(e => e.Id);
            
            entity.HasOne(o => o.UserItem)
                .WithOne(ui => ui.Offer)
                .HasForeignKey<UserItemOffer>(o => o.UserItemId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
    
    private static void ConfigureUserCustomizationEntities(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<UserCustomization>(entity =>
        {
            entity.HasKey(e => e.Id);
        });
    }
    
    private static void ConfigureInGameMerchantEntities(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<InGameMerchant>(entity =>
        {
            entity.HasKey(e => e.Id);
            
            entity.HasMany(m => m.ItemOffers)
                .WithOne(o => o.Merchant)
                .HasForeignKey(o => o.MerchantId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
    
    private static void ConfigureMerchantItemOfferEntities(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<MerchantItemOffer>(entity =>
        {
            entity.HasKey(e => e.Id);

            entity.HasOne(o => o.Item)
                .WithMany(i => i.MerchantItemOffers)
                .HasForeignKey(o => o.ItemId);

            entity.HasOne(o => o.Merchant)
                .WithMany(m => m.ItemOffers)
                .HasForeignKey(o => o.MerchantId);
        });
    }
    
    private static void ConfigureRefreshTokenEntities(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<RefreshToken>(entity =>
        {
            entity.HasKey(e => e.Id);
            
            entity.HasIndex(e => e.TokenHash)
                .IsUnique();
        });
    }
}