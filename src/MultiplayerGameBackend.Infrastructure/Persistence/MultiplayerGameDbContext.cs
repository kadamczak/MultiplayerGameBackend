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
    public DbSet<User> Users { get; set; }
    public DbSet<UserItem> UserItems { get; set; }
    
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        
        ConfigureItemEntities(modelBuilder);
        ConfigureUserEntities(modelBuilder);
        ConfigureUserItemEntities(modelBuilder);
    }
    
    private static void ConfigureItemEntities(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Item>(entity =>
        {
            entity.Property(e => e.Name)
                .HasMaxLength(Item.NameMaxLength);
            
            entity.HasIndex(e => e.Name)
                .IsUnique();

            entity.Property(e => e.Description)
                .HasMaxLength(Item.DescriptionMaxLength);

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
                .HasMaxLength(User.UserNameMaxLength);

            entity.HasIndex(e => e.UserName)
                .IsUnique();
            
            entity.Property(e => e.NormalizedUserName)
                .HasMaxLength(User.UserNameMaxLength);

            entity.Property(e => e.Email)
                .HasMaxLength(User.EmailMaxLength);

            entity.HasIndex(e => e.Email)
                .IsUnique();
            
            entity.Property(e => e.NormalizedEmail)
                .HasMaxLength(User.EmailMaxLength);

            entity.Property(e => e.PasswordHash)
                .HasMaxLength(User.PasswordHashMaxLength);

            entity.HasMany(u => u.UserItems)
                .WithOne(ui => ui.User)
                .HasForeignKey(ui => ui.UserId)
                .OnDelete(DeleteBehavior.Cascade);;
        });
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
}