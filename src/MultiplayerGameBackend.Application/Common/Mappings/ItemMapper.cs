using MultiplayerGameBackend.Application.Items.ReadDtos;
using MultiplayerGameBackend.Domain.Entities;
using Riok.Mapperly.Abstractions;

namespace MultiplayerGameBackend.Application.Common.Mappings;

[Mapper]
public partial class ItemMapper
{
    public partial ItemReadDto? Map(Item? item);
}