using MultiplayerGameBackend.Application.Users.Requests;
using MultiplayerGameBackend.Application.Users.Responses;
using MultiplayerGameBackend.Domain.Entities;
using Riok.Mapperly.Abstractions;

namespace MultiplayerGameBackend.Application.Common.Mappings;

[Mapper]
public partial class UserCustomizationMapper
{
    public partial UserCustomization Map(UpdateUserAppearanceDto dto);
    public partial void UpdateFromDto(UpdateUserAppearanceDto dto, UserCustomization target);
    public partial ReadUserCustomizationDto Map(UserCustomization entity);
}

