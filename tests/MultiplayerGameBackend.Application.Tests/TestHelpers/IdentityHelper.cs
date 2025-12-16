using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MultiplayerGameBackend.Domain.Entities;
using MultiplayerGameBackend.Infrastructure.Persistence;
using NSubstitute;

namespace MultiplayerGameBackend.Application.Tests.TestHelpers;

/// <summary>
/// Helper class for creating Identity-related objects for testing
/// </summary>
public static class IdentityHelper
{
    public static UserManager<User> CreateUserManager(MultiplayerGameDbContext context)
    {
        var userStore = new UserStore<User, IdentityRole<Guid>, MultiplayerGameDbContext, Guid>(context);
        var options = Substitute.For<IOptions<IdentityOptions>>();
        options.Value.Returns(new IdentityOptions());
        var passwordHasher = new PasswordHasher<User>();
        var userValidators = new List<IUserValidator<User>>();
        var passwordValidators = new List<IPasswordValidator<User>>();
        var keyNormalizer = new UpperInvariantLookupNormalizer();
        var errors = new IdentityErrorDescriber();
        var services = Substitute.For<IServiceProvider>();
        var logger = Substitute.For<ILogger<UserManager<User>>>();

        return new UserManager<User>(userStore, options, passwordHasher, userValidators, 
            passwordValidators, keyNormalizer, errors, services, logger);
    }

    /// <summary>
    /// Creates a UserManager with token provider support for IdentityService tests
    /// </summary>
    public static UserManager<User> CreateUserManagerWithTokenProvider(MultiplayerGameDbContext context)
    {
        var userStore = new UserStore<User, IdentityRole<Guid>, MultiplayerGameDbContext, Guid>(context);
        
        var identityOptions = new IdentityOptions();
        identityOptions.Tokens.ProviderMap["Default"] = new TokenProviderDescriptor(typeof(DataProtectorTokenProvider<User>));
        identityOptions.Tokens.EmailConfirmationTokenProvider = "Default";
        identityOptions.Tokens.PasswordResetTokenProvider = "Default";
        
        var options = Substitute.For<IOptions<IdentityOptions>>();
        options.Value.Returns(identityOptions);
        
        var passwordHasher = new PasswordHasher<User>();
        var userValidators = new List<IUserValidator<User>>();
        var passwordValidators = new List<IPasswordValidator<User>>();
        var keyNormalizer = new UpperInvariantLookupNormalizer();
        var errors = new IdentityErrorDescriber();
        
        // Setup service provider to provide token provider
        var services = Substitute.For<IServiceProvider>();
        var loggerFactory = Substitute.For<ILoggerFactory>();
        var tokenProvider = new DataProtectorTokenProvider<User>(
            new EphemeralDataProtectionProvider(loggerFactory),
            Substitute.For<IOptions<DataProtectionTokenProviderOptions>>(),
            Substitute.For<ILogger<DataProtectorTokenProvider<User>>>()
        );
        services.GetService(typeof(DataProtectorTokenProvider<User>)).Returns(tokenProvider);
        
        var logger = Substitute.For<ILogger<UserManager<User>>>();

        return new UserManager<User>(userStore, options, passwordHasher, userValidators, 
            passwordValidators, keyNormalizer, errors, services, logger);
    }

    public static RoleManager<IdentityRole<Guid>> CreateRoleManager(MultiplayerGameDbContext context)
    {
        var roleStore = new RoleStore<IdentityRole<Guid>, MultiplayerGameDbContext, Guid>(context);
        var roleValidators = new List<IRoleValidator<IdentityRole<Guid>>>();
        var keyNormalizer = new UpperInvariantLookupNormalizer();
        var errors = new IdentityErrorDescriber();
        var logger = Substitute.For<ILogger<RoleManager<IdentityRole<Guid>>>>();

        return new RoleManager<IdentityRole<Guid>>(roleStore, roleValidators, keyNormalizer, errors, logger);
    }
}

