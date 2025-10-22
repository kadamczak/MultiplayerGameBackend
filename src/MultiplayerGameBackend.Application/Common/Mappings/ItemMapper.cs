using MultiplayerGameBackend.Application.Items.ReadDtos;
using MultiplayerGameBackend.Application.Items.Requests;
using MultiplayerGameBackend.Domain.Entities;
using Riok.Mapperly.Abstractions;

namespace MultiplayerGameBackend.Application.Common.Mappings;

[Mapper]
public partial class ItemMapper
{
    public partial ReadItemDto? Map(Item? item);
    public partial Item Map(CreateItemDto dto);
}