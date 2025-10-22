using AutoMapper;
using MultiplayerGameBackend.Application.Items.ReadDtos;
using MultiplayerGameBackend.Domain.Entities;

namespace MultiplayerGameBackend.Application.Common;

public class MappingProfile : Profile
{
    public MappingProfile()
    {
        CreateMap<Item, ItemReadDto>().ReverseMap();
    }
}