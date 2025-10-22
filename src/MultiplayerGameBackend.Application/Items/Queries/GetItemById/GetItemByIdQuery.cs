using MediatR;
using MultiplayerGameBackend.Application.Items.ReadDtos;

namespace MultiplayerGameBackend.Application.Items.Queries.GetItemById;

public class GetItemByIdQuery(int id) : IRequest<ItemReadDto>
{
    public int Id { get; } = id;
}