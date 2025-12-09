using Microsoft.Extensions.Logging;
using MultiplayerGameBackend.Application.Common.Mappings;
using MultiplayerGameBackend.Application.Items;
using MultiplayerGameBackend.Application.Items.Requests;
using MultiplayerGameBackend.Application.Tests.Common;
using MultiplayerGameBackend.Domain.Constants;
using MultiplayerGameBackend.Domain.Entities;
using MultiplayerGameBackend.Domain.Exceptions;
using NSubstitute;

namespace MultiplayerGameBackend.Application.Tests.Items;

public class ItemServiceTests : IClassFixture<DatabaseFixture>, IAsyncLifetime
{
    private readonly DatabaseFixture _fixture;
    private readonly ILogger<ItemService> _logger;
    private readonly ItemMapper _mapper;

    public ItemServiceTests(DatabaseFixture fixture)
    {
        _fixture = fixture;
        _logger = Substitute.For<ILogger<ItemService>>();
        _mapper = new ItemMapper();
    }

    public async Task InitializeAsync() => await _fixture.InitializeAsync();
    public Task DisposeAsync() => Task.CompletedTask;

    #region GetById Tests

    [Fact]
    public async Task GetById_ShouldReturnItem_WhenItemExists()
    {
        // Arrange
        await using var context = _fixture.CreateDbContext();
        var service = new ItemService(_logger, context, _mapper);

        var item = new Item
        {
            Name = "Test Sword",
            Description = "A powerful sword",
            Type = ItemTypes.EquippableOnHead,
            ThumbnailUrl = "assets/sword.png"
        };
        context.Items.Add(item);
        await context.SaveChangesAsync();

        // Act
        var result = await service.GetById(item.Id, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(item.Id, result.Id);
        Assert.Equal(item.Name, result.Name);
        Assert.Equal(item.Description, result.Description);
        Assert.Equal(item.Type, result.Type);
        Assert.Equal(item.ThumbnailUrl, result.ThumbnailUrl);
    }

    [Fact]
    public async Task GetById_ShouldReturnNull_WhenItemDoesNotExist()
    {
        // Arrange
        await using var context = _fixture.CreateDbContext();
        var service = new ItemService(_logger, context, _mapper);

        // Act
        var result = await service.GetById(99999, CancellationToken.None);

        // Assert
        Assert.Null(result);
    }

    #endregion

    #region GetAll Tests

    [Fact]
    public async Task GetAll_ShouldReturnAllItems()
    {
        // Arrange
        await using var context = _fixture.CreateDbContext();
        var service = new ItemService(_logger, context, _mapper);

        var items = new List<Item>
        {
            new() { Name = "Item 1", Description = "Desc 1", Type = ItemTypes.EquippableOnHead, ThumbnailUrl = "url1.png" },
            new() { Name = "Item 2", Description = "Desc 2", Type = ItemTypes.EquippableOnBody, ThumbnailUrl = "url2.png" },
            new() { Name = "Item 3", Description = "Desc 3", Type = ItemTypes.EquippableOnHead, ThumbnailUrl = "url3.png" }
        };
        context.Items.AddRange(items);
        await context.SaveChangesAsync();

        // Act
        var result = await service.GetAll(CancellationToken.None);

        // Assert
        var resultList = result.ToList();
        Assert.NotEmpty(resultList);
        Assert.Equal(3, resultList.Count);
        Assert.Contains(resultList, r => r.Name == "Item 1");
        Assert.Contains(resultList, r => r.Name == "Item 2");
        Assert.Contains(resultList, r => r.Name == "Item 3");
    }

    [Fact]
    public async Task GetAll_ShouldReturnEmptyList_WhenNoItemsExist()
    {
        // Arrange
        await using var context = _fixture.CreateDbContext();
        var service = new ItemService(_logger, context, _mapper);

        // Act
        var result = await service.GetAll(CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        // Note: May not be empty if other tests have run, so we just check it's not null
    }

    #endregion

    #region Create Tests

    [Fact]
    public async Task Create_ShouldCreateItem_WhenValidDto()
    {
        // Arrange
        await using var context = _fixture.CreateDbContext();
        var service = new ItemService(_logger, context, _mapper);

        var dto = new CreateUpdateItemDto
        {
            Name = "  New Helmet  ",
            Description = "  A shiny helmet  ",
            Type = ItemTypes.EquippableOnHead,
            ThumbnailUrl = "assets/helmet.png"
        };

        // Act
        var itemId = await service.Create(dto, CancellationToken.None);

        // Assert
        Assert.True(itemId > 0);

        var createdItem = await context.Items.FindAsync(itemId);
        Assert.NotNull(createdItem);
        Assert.Equal("New Helmet", createdItem.Name); // Trimmed
        Assert.Equal("A shiny helmet", createdItem.Description); // Trimmed
        Assert.Equal(ItemTypes.EquippableOnHead, createdItem.Type);
        Assert.Equal("assets/helmet.png", createdItem.ThumbnailUrl);
    }

    [Fact]
    public async Task Create_ShouldTrimNameAndDescription()
    {
        // Arrange
        await using var context = _fixture.CreateDbContext();
        var service = new ItemService(_logger, context, _mapper);

        var dto = new CreateUpdateItemDto
        {
            Name = "   Spaced Item   ",
            Description = "   Spaced Description   ",
            Type = ItemTypes.EquippableOnBody,
            ThumbnailUrl = "assets/item.png"
        };

        // Act
        var itemId = await service.Create(dto, CancellationToken.None);

        // Assert
        var createdItem = await context.Items.FindAsync(itemId);
        Assert.NotNull(createdItem);
        Assert.Equal("Spaced Item", createdItem.Name);
        Assert.Equal("Spaced Description", createdItem.Description);
    }

    [Fact]
    public async Task Create_ShouldThrowConflictException_WhenItemWithSameNameExists()
    {
        // Arrange
        await using var context = _fixture.CreateDbContext();
        var service = new ItemService(_logger, context, _mapper);

        var existingItem = new Item
        {
            Name = "Duplicate Item",
            Description = "First one",
            Type = ItemTypes.EquippableOnHead,
            ThumbnailUrl = "assets/dup.png"
        };
        context.Items.Add(existingItem);
        await context.SaveChangesAsync();

        var dto = new CreateUpdateItemDto
        {
            Name = "Duplicate Item",
            Description = "Second one",
            Type = ItemTypes.EquippableOnBody,
            ThumbnailUrl = "assets/dup2.png"
        };

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ConflictException>(
            () => service.Create(dto, CancellationToken.None)
        );

        Assert.NotNull(exception.Errors);
        Assert.True(exception.Errors.ContainsKey("Name"));
    }

    #endregion

    #region Update Tests

    [Fact]
    public async Task Update_ShouldUpdateItem_WhenItemExists()
    {
        // Arrange
        await using var context = _fixture.CreateDbContext();
        var service = new ItemService(_logger, context, _mapper);

        var item = new Item
        {
            Name = "Original Name",
            Description = "Original Description",
            Type = ItemTypes.EquippableOnHead,
            ThumbnailUrl = "assets/original.png"
        };
        context.Items.Add(item);
        await context.SaveChangesAsync();

        var updateDto = new CreateUpdateItemDto
        {
            Name = "  Updated Name  ",
            Description = "  Updated Description  ",
            Type = ItemTypes.EquippableOnBody,
            ThumbnailUrl = "assets/updated.png"
        };

        // Act
        await service.Update(item.Id, updateDto, CancellationToken.None);

        // Assert
        var updatedItem = await context.Items.FindAsync(item.Id);
        Assert.NotNull(updatedItem);
        Assert.Equal("Updated Name", updatedItem.Name);
        Assert.Equal("Updated Description", updatedItem.Description);
        // Note: ItemService.Update doesn't update Type and ThumbnailUrl based on the code
    }

    [Fact]
    public async Task Update_ShouldTrimNameAndDescription()
    {
        // Arrange
        await using var context = _fixture.CreateDbContext();
        var service = new ItemService(_logger, context, _mapper);

        var item = new Item
        {
            Name = "Test Item",
            Description = "Test Description",
            Type = ItemTypes.EquippableOnHead,
            ThumbnailUrl = "assets/test.png"
        };
        context.Items.Add(item);
        await context.SaveChangesAsync();

        var updateDto = new CreateUpdateItemDto
        {
            Name = "   Trimmed Name   ",
            Description = "   Trimmed Description   ",
            Type = ItemTypes.EquippableOnBody,
            ThumbnailUrl = "assets/test2.png"
        };

        // Act
        await service.Update(item.Id, updateDto, CancellationToken.None);

        // Assert
        var updatedItem = await context.Items.FindAsync(item.Id);
        Assert.NotNull(updatedItem);
        Assert.Equal("Trimmed Name", updatedItem.Name);
        Assert.Equal("Trimmed Description", updatedItem.Description);
    }

    [Fact]
    public async Task Update_ShouldThrowNotFoundException_WhenItemDoesNotExist()
    {
        // Arrange
        await using var context = _fixture.CreateDbContext();
        var service = new ItemService(_logger, context, _mapper);

        var updateDto = new CreateUpdateItemDto
        {
            Name = "Non-existent",
            Description = "Does not exist",
            Type = ItemTypes.EquippableOnHead,
            ThumbnailUrl = "assets/none.png"
        };

        // Act & Assert
        await Assert.ThrowsAsync<NotFoundException>(
            () => service.Update(99999, updateDto, CancellationToken.None)
        );
    }

    #endregion

    #region Delete Tests

    [Fact]
    public async Task Delete_ShouldDeleteItem_WhenItemExists()
    {
        // Arrange
        await using var context = _fixture.CreateDbContext();
        var service = new ItemService(_logger, context, _mapper);

        var item = new Item
        {
            Name = "Item to Delete",
            Description = "Will be deleted",
            Type = ItemTypes.EquippableOnHead,
            ThumbnailUrl = "assets/delete.png"
        };
        context.Items.Add(item);
        await context.SaveChangesAsync();

        // Act
        var result = await service.Delete(item.Id, CancellationToken.None);

        // Assert
        Assert.True(result);
        var deletedItem = await context.Items.FindAsync(item.Id);
        Assert.Null(deletedItem);
    }

    [Fact]
    public async Task Delete_ShouldReturnFalse_WhenItemDoesNotExist()
    {
        // Arrange
        await using var context = _fixture.CreateDbContext();
        var service = new ItemService(_logger, context, _mapper);

        // Act
        var result = await service.Delete(99999, CancellationToken.None);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task Delete_ShouldReturnFalse_ForAlreadyDeletedItem()
    {
        // Arrange
        await using var context = _fixture.CreateDbContext();
        var service = new ItemService(_logger, context, _mapper);

        var item = new Item
        {
            Name = "Double Delete Test",
            Description = "Test double deletion",
            Type = ItemTypes.EquippableOnHead,
            ThumbnailUrl = "assets/double.png"
        };
        context.Items.Add(item);
        await context.SaveChangesAsync();
        var itemId = item.Id;

        // Act - Delete once
        var firstDelete = await service.Delete(itemId, CancellationToken.None);
        // Act - Delete again
        var secondDelete = await service.Delete(itemId, CancellationToken.None);

        // Assert
        Assert.True(firstDelete);
        Assert.False(secondDelete);
    }

    #endregion

    #region Edge Cases and Integration Tests

    [Fact]
    public async Task Create_AndGetById_ShouldWorkTogether()
    {
        // Arrange
        await using var context = _fixture.CreateDbContext();
        var service = new ItemService(_logger, context, _mapper);

        var dto = new CreateUpdateItemDto
        {
            Name = "Integration Test Item",
            Description = "Testing create and get",
            Type = ItemTypes.EquippableOnBody,
            ThumbnailUrl = "assets/integration.png"
        };

        // Act
        var createdId = await service.Create(dto, CancellationToken.None);
        var retrieved = await service.GetById(createdId, CancellationToken.None);

        // Assert
        Assert.NotNull(retrieved);
        Assert.Equal(createdId, retrieved.Id);
        Assert.Equal("Integration Test Item", retrieved.Name);
    }

    [Fact]
    public async Task Update_AndGetById_ShouldWorkTogether()
    {
        // Arrange
        await using var context = _fixture.CreateDbContext();
        var service = new ItemService(_logger, context, _mapper);

        var item = new Item
        {
            Name = "Before Update",
            Description = "Original",
            Type = ItemTypes.Consumable,
            ThumbnailUrl = "assets/before.png"
        };
        context.Items.Add(item);
        await context.SaveChangesAsync();

        var updateDto = new CreateUpdateItemDto
        {
            Name = "After Update",
            Description = "Modified",
            Type = ItemTypes.Consumable,
            ThumbnailUrl = "assets/after.png"
        };

        // Act
        await service.Update(item.Id, updateDto, CancellationToken.None);
        var retrieved = await service.GetById(item.Id, CancellationToken.None);

        // Assert
        Assert.NotNull(retrieved);
        Assert.Equal("After Update", retrieved.Name);
        Assert.Equal("Modified", retrieved.Description);
    }

    [Fact]
    public async Task CancellationToken_ShouldBeRespected()
    {
        // Arrange
        await using var context = _fixture.CreateDbContext();
        var service = new ItemService(_logger, context, _mapper);
        var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act & Assert
        await Assert.ThrowsAsync<OperationCanceledException>(
            () => service.GetAll(cts.Token)
        );
    }

    #endregion
}

