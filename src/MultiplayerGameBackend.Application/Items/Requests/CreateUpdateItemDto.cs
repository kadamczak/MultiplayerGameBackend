using System.ComponentModel.DataAnnotations;
using MultiplayerGameBackend.Domain.Entities;

namespace MultiplayerGameBackend.Application.Items.Requests;

public class CreateUpdateItemDto
{
    public required string Name { get; set; }
    public required string Description { get; set; }
    public required string Type { get; set; }
    public required string ThumbnailUrl { get; set; }
}