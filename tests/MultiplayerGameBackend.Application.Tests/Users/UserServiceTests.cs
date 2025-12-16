using Microsoft.Extensions.Logging;
using MultiplayerGameBackend.Application.Common.Mappings;
using MultiplayerGameBackend.Application.Interfaces;
using MultiplayerGameBackend.Application.Tests.TestHelpers;
using MultiplayerGameBackend.Application.Users;
using MultiplayerGameBackend.Application.Users.Requests;
using MultiplayerGameBackend.Domain.Constants;
using MultiplayerGameBackend.Domain.Exceptions;
using NSubstitute;

namespace MultiplayerGameBackend.Application.Tests.Users;

public class UserServiceTests : IClassFixture<DatabaseFixture>, IAsyncLifetime
{
    private readonly DatabaseFixture _fixture;
    private readonly ILogger<UserService> _logger;
    private readonly IImageService _imageService;
    private readonly UserCustomizationMapper _customizationMapper;

    public UserServiceTests(DatabaseFixture fixture)
    {
        _fixture = fixture;
        _logger = Substitute.For<ILogger<UserService>>();
        _imageService = Substitute.For<IImageService>();
        _customizationMapper = new UserCustomizationMapper();
    }

    public Task InitializeAsync() => Task.CompletedTask;

    public async Task DisposeAsync() => await _fixture.CleanDatabase();


    #region AssignUserRole Tests

    [Fact]
    public async Task AssignUserRole_ShouldAssignRole_WhenUserAndRoleExist()
    {
        // Arrange
        await using var context = _fixture.CreateDbContext();
        var userManager = IdentityHelper.CreateUserManager(context);
        var roleManager = IdentityHelper.CreateRoleManager(context);
        var service = new UserService(_logger, userManager, roleManager, context, _customizationMapper, _imageService);

        var userId = Guid.NewGuid();
        var user = TestEntityFactory.CreateUser("testuser", "test@example.com", userId);
        await userManager.CreateAsync(user);

        var role = TestEntityFactory.CreateRole("Admin");
        await roleManager.CreateAsync(role);

        var dto = new ModifyUserRoleDto { RoleName = "Admin" };

        // Act
        await service.AssignUserRole(userId, dto, CancellationToken.None);

        // Assert
        var isInRole = await userManager.IsInRoleAsync(user, "Admin");
        Assert.True(isInRole);
    }

    [Fact]
    public async Task AssignUserRole_ShouldThrowNotFoundException_WhenUserDoesNotExist()
    {
        // Arrange
        await using var context = _fixture.CreateDbContext();
        var userManager = IdentityHelper.CreateUserManager(context);
        var roleManager = IdentityHelper.CreateRoleManager(context);
        var service = new UserService(_logger, userManager, roleManager, context, _customizationMapper, _imageService);

        var role = TestEntityFactory.CreateRole("Admin");
        await roleManager.CreateAsync(role);

        var dto = new ModifyUserRoleDto { RoleName = "Admin" };

        // Act & Assert
        await Assert.ThrowsAsync<NotFoundException>(
            () => service.AssignUserRole(Guid.NewGuid(), dto, CancellationToken.None)
        );
    }

    [Fact]
    public async Task AssignUserRole_ShouldThrowNotFoundException_WhenRoleDoesNotExist()
    {
        // Arrange
        await using var context = _fixture.CreateDbContext();
        var userManager = IdentityHelper.CreateUserManager(context);
        var roleManager = IdentityHelper.CreateRoleManager(context);
        var service = new UserService(_logger, userManager, roleManager, context, _customizationMapper, _imageService);

        var userId = Guid.NewGuid();
        var user = TestEntityFactory.CreateUser("testuser", "test@example.com", userId);
        await userManager.CreateAsync(user);

        var dto = new ModifyUserRoleDto { RoleName = "NonExistentRole" };

        // Act & Assert
        await Assert.ThrowsAsync<NotFoundException>(
            () => service.AssignUserRole(userId, dto, CancellationToken.None)
        );
    }

    #endregion

    #region UnassignUserRole Tests

    [Fact]
    public async Task UnassignUserRole_ShouldRemoveRole_WhenUserHasRole()
    {
        // Arrange
        await using var context = _fixture.CreateDbContext();
        var userManager = IdentityHelper.CreateUserManager(context);
        var roleManager = IdentityHelper.CreateRoleManager(context);
        var service = new UserService(_logger, userManager, roleManager, context, _customizationMapper, _imageService);

        var userId = Guid.NewGuid();
        var user = TestEntityFactory.CreateUser("testuser", "test@example.com", userId);
        await userManager.CreateAsync(user);

        var role = TestEntityFactory.CreateRole("Admin");
        await roleManager.CreateAsync(role);
        await userManager.AddToRoleAsync(user, "Admin");

        var dto = new ModifyUserRoleDto { RoleName = "Admin" };

        // Act
        await service.UnassignUserRole(userId, dto, CancellationToken.None);

        // Assert
        var isInRole = await userManager.IsInRoleAsync(user, "Admin");
        Assert.False(isInRole);
    }

    [Fact]
    public async Task UnassignUserRole_ShouldThrowNotFoundException_WhenUserDoesNotExist()
    {
        // Arrange
        await using var context = _fixture.CreateDbContext();
        var userManager = IdentityHelper.CreateUserManager(context);
        var roleManager = IdentityHelper.CreateRoleManager(context);
        var service = new UserService(_logger, userManager, roleManager, context, _customizationMapper, _imageService);

        var role = TestEntityFactory.CreateRole("Admin");
        await roleManager.CreateAsync(role);

        var dto = new ModifyUserRoleDto { RoleName = "Admin" };

        // Act & Assert
        await Assert.ThrowsAsync<NotFoundException>(
            () => service.UnassignUserRole(Guid.NewGuid(), dto, CancellationToken.None)
        );
    }

    #endregion

    #region GetCurrentUserGameInfo Tests

    [Fact]
    public async Task GetCurrentUserGameInfo_ShouldReturnUserInfo_WithoutOptionalData()
    {
        // Arrange
        await using var context = _fixture.CreateDbContext();
        var userManager = IdentityHelper.CreateUserManager(context);
        var roleManager = IdentityHelper.CreateRoleManager(context);
        var service = new UserService(_logger, userManager, roleManager, context, _customizationMapper, _imageService);

        var userId = Guid.NewGuid();
        var user = TestEntityFactory.CreateUser("gamer123", "gamer@example.com", userId, balance: 500);
        await userManager.CreateAsync(user);

        // Act
        var result = await service.GetCurrentUserGameInfo(userId, includeCustomization: false, includeUserItems: false, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(userId, result.Id);
        Assert.Equal("gamer123", result.UserName);
        Assert.Equal(500, result.Balance);
        Assert.Null(result.Customization);
        Assert.Null(result.UserItems);
    }

    [Fact]
    public async Task GetCurrentUserGameInfo_ShouldReturnUserInfo_WithCustomization()
    {
        // Arrange
        await using var context = _fixture.CreateDbContext();
        var userManager = IdentityHelper.CreateUserManager(context);
        var roleManager = IdentityHelper.CreateRoleManager(context);
        var service = new UserService(_logger, userManager, roleManager, context, _customizationMapper, _imageService);

        var userId = Guid.NewGuid();
        var user = TestEntityFactory.CreateUser("gamer123", "gamer@example.com", userId, balance: 500);
        await userManager.CreateAsync(user);

        var customization = TestEntityFactory.CreateUserCustomization(userId);
        context.UserCustomizations.Add(customization);
        await context.SaveChangesAsync();

        // Act
        var result = await service.GetCurrentUserGameInfo(userId, includeCustomization: true, includeUserItems: false, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.NotNull(result.Customization);
        Assert.Equal("#FF0000", result.Customization.HeadColor);
        Assert.Equal("#00FF00", result.Customization.BodyColor);
        Assert.Null(result.UserItems);
    }

    [Fact]
    public async Task GetCurrentUserGameInfo_ShouldReturnUserInfo_WithUserItems()
    {
        // Arrange
        await using var context = _fixture.CreateDbContext();
        var userManager = IdentityHelper.CreateUserManager(context);
        var roleManager = IdentityHelper.CreateRoleManager(context);
        var service = new UserService(_logger, userManager, roleManager, context, _customizationMapper, _imageService);

        var userId = Guid.NewGuid();
        var user = TestEntityFactory.CreateUser("gamer123", "gamer@example.com", userId, balance: 500);
        await userManager.CreateAsync(user);

        var item = TestEntityFactory.CreateItem("Cool Helmet", ItemTypes.EquippableOnHead, "A very cool helmet", "assets/helmet.png");
        context.Items.Add(item);
        await context.SaveChangesAsync();

        var userItem = TestEntityFactory.CreateUserItem(userId, item.Id);
        context.UserItems.Add(userItem);
        await context.SaveChangesAsync();

        // Act
        var result = await service.GetCurrentUserGameInfo(userId, includeCustomization: false, includeUserItems: true, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.NotNull(result.UserItems);
        Assert.Single(result.UserItems);
        Assert.Equal("Cool Helmet", result.UserItems[0].Item.Name);
        Assert.Null(result.Customization);
    }

    [Fact]
    public async Task GetCurrentUserGameInfo_ShouldThrowNotFoundException_WhenUserDoesNotExist()
    {
        // Arrange
        await using var context = _fixture.CreateDbContext();
        var userManager = IdentityHelper.CreateUserManager(context);
        var roleManager = IdentityHelper.CreateRoleManager(context);
        var service = new UserService(_logger, userManager, roleManager, context, _customizationMapper, _imageService);

        // Act & Assert
        await Assert.ThrowsAsync<NotFoundException>(
            () => service.GetCurrentUserGameInfo(Guid.NewGuid(), includeCustomization: false, includeUserItems: false, CancellationToken.None)
        );
    }

    #endregion

    #region UpdateUserAppearance Tests

    [Fact]
    public async Task UpdateUserAppearance_ShouldCreateNewCustomization_WhenNoneExists()
    {
        // Arrange
        await using var context = _fixture.CreateDbContext();
        var userManager = IdentityHelper.CreateUserManager(context);
        var roleManager = IdentityHelper.CreateRoleManager(context);
        var service = new UserService(_logger, userManager, roleManager, context, _customizationMapper, _imageService);

        var userId = Guid.NewGuid();
        var user = TestEntityFactory.CreateUser("gamer123", "gamer@example.com", userId);
        await userManager.CreateAsync(user);

        var dto = new UpdateUserAppearanceDto
        {
            HeadColor = "#FF0000",
            BodyColor = "#00FF00",
            TailColor = "#0000FF",
            EyeColor = "#FFFF00",
            WingColor = "#FF00FF",
            HornColor = "#00FFFF",
            MarkingsColor = "#FFFFFF"
        };

        // Act
        await service.UpdateUserAppearance(userId, dto, CancellationToken.None);

        // Assert
        var customization = context.UserCustomizations.FirstOrDefault(c => c.UserId == userId);
        Assert.NotNull(customization);
        Assert.Equal("#FF0000", customization.HeadColor);
        Assert.Equal("#00FF00", customization.BodyColor);
    }

    [Fact]
    public async Task UpdateUserAppearance_ShouldUpdateExistingCustomization_WhenExists()
    {
        // Arrange
        await using var context = _fixture.CreateDbContext();
        var userManager = IdentityHelper.CreateUserManager(context);
        var roleManager = IdentityHelper.CreateRoleManager(context);
        var service = new UserService(_logger, userManager, roleManager, context, _customizationMapper, _imageService);

        var userId = Guid.NewGuid();
        var user = TestEntityFactory.CreateUser("gamer123", "gamer@example.com", userId);
        await userManager.CreateAsync(user);

        var existingCustomization = TestEntityFactory.CreateUserCustomization(
            userId,
            headColor: "#000000",
            bodyColor: "#000000",
            tailColor: "#000000",
            eyeColor: "#000000",
            wingColor: "#000000",
            hornColor: "#000000",
            markingsColor: "#000000"
        );
        context.UserCustomizations.Add(existingCustomization);
        await context.SaveChangesAsync();

        var dto = new UpdateUserAppearanceDto
        {
            HeadColor = "#FFFFFF",
            BodyColor = "#FFFFFF",
            TailColor = "#FFFFFF",
            EyeColor = "#FFFFFF",
            WingColor = "#FFFFFF",
            HornColor = "#FFFFFF",
            MarkingsColor = "#FFFFFF"
        };

        // Act
        await service.UpdateUserAppearance(userId, dto, CancellationToken.None);

        // Assert
        var customization = context.UserCustomizations.FirstOrDefault(c => c.UserId == userId);
        Assert.NotNull(customization);
        Assert.Equal("#FFFFFF", customization.HeadColor);
        Assert.Equal("#FFFFFF", customization.BodyColor);
    }


    #endregion

    #region UploadProfilePicture Tests

    [Fact]
    public async Task UploadProfilePicture_ShouldUploadAndReturnUrl_WhenValid()
    {
        // Arrange
        await using var context = _fixture.CreateDbContext();
        var userManager = IdentityHelper.CreateUserManager(context);
        var roleManager = IdentityHelper.CreateRoleManager(context);
        var service = new UserService(_logger, userManager, roleManager, context, _customizationMapper, _imageService);

        var userId = Guid.NewGuid();
        var user = TestEntityFactory.CreateUser("gamer123", "gamer@example.com", userId);
        await userManager.CreateAsync(user);

        var imageStream = new MemoryStream();
        var expectedUrl = "/uploads/profiles/test.jpg";
        _imageService.SaveProfilePictureAsync(imageStream, "test.jpg", Arg.Any<CancellationToken>())
            .Returns(expectedUrl);

        // Act
        var result = await service.UploadProfilePicture(userId, imageStream, "test.jpg", CancellationToken.None);

        // Assert
        Assert.Equal(expectedUrl, result);
        var updatedUser = await userManager.FindByIdAsync(userId.ToString());
        Assert.Equal(expectedUrl, updatedUser!.ProfilePictureUrl);
    }

    [Fact]
    public async Task UploadProfilePicture_ShouldDeleteOldPicture_WhenUserAlreadyHasOne()
    {
        // Arrange
        await using var context = _fixture.CreateDbContext();
        var userManager = IdentityHelper.CreateUserManager(context);
        var roleManager = IdentityHelper.CreateRoleManager(context);
        var service = new UserService(_logger, userManager, roleManager, context, _customizationMapper, _imageService);

        var userId = Guid.NewGuid();
        var oldUrl = "/uploads/profiles/old.jpg";
        var user = TestEntityFactory.CreateUser("gamer123", "gamer@example.com", userId);
        user.ProfilePictureUrl = oldUrl;
        await userManager.CreateAsync(user);

        var imageStream = new MemoryStream();
        var newUrl = "/uploads/profiles/new.jpg";
        _imageService.SaveProfilePictureAsync(imageStream, "new.jpg", Arg.Any<CancellationToken>())
            .Returns(newUrl);

        // Act
        await service.UploadProfilePicture(userId, imageStream, "new.jpg", CancellationToken.None);

        // Assert
        await _imageService.Received(1).DeleteProfilePictureAsync(oldUrl, Arg.Any<CancellationToken>());
        var updatedUser = await userManager.FindByIdAsync(userId.ToString());
        Assert.Equal(newUrl, updatedUser!.ProfilePictureUrl);
    }


    #endregion

    #region DeleteProfilePicture Tests

    [Fact]
    public async Task DeleteProfilePicture_ShouldDeletePicture_WhenExists()
    {
        // Arrange
        await using var context = _fixture.CreateDbContext();
        var userManager = IdentityHelper.CreateUserManager(context);
        var roleManager = IdentityHelper.CreateRoleManager(context);
        var service = new UserService(_logger, userManager, roleManager, context, _customizationMapper, _imageService);

        var userId = Guid.NewGuid();
        var pictureUrl = "/uploads/profiles/test.jpg";
        var user = TestEntityFactory.CreateUser("gamer123", "gamer@example.com", userId);
        user.ProfilePictureUrl = pictureUrl;
        await userManager.CreateAsync(user);

        // Act
        await service.DeleteProfilePicture(userId, CancellationToken.None);

        // Assert
        await _imageService.Received(1).DeleteProfilePictureAsync(pictureUrl, Arg.Any<CancellationToken>());
        var updatedUser = await userManager.FindByIdAsync(userId.ToString());
        Assert.Null(updatedUser!.ProfilePictureUrl);
    }

    [Fact]
    public async Task DeleteProfilePicture_ShouldThrowNotFoundException_WhenNoPictureExists()
    {
        // Arrange
        await using var context = _fixture.CreateDbContext();
        var userManager = IdentityHelper.CreateUserManager(context);
        var roleManager = IdentityHelper.CreateRoleManager(context);
        var service = new UserService(_logger, userManager, roleManager, context, _customizationMapper, _imageService);

        var userId = Guid.NewGuid();
        var user = TestEntityFactory.CreateUser("gamer123", "gamer@example.com", userId);
        user.ProfilePictureUrl = null;
        await userManager.CreateAsync(user);

        // Act & Assert
        await Assert.ThrowsAsync<NotFoundException>(
            () => service.DeleteProfilePicture(userId, CancellationToken.None)
        );
    }

    #endregion
}

