namespace MultiplayerGameBackend.Application.Interfaces;

public interface IImageService
{
    Task<string> SaveProfilePictureAsync(Stream imageStream, string fileName, CancellationToken cancellationToken = default);
    Task DeleteProfilePictureAsync(string fileName, CancellationToken cancellationToken = default);
}

