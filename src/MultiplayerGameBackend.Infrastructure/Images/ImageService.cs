using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Logging;
using MultiplayerGameBackend.Application.Interfaces;
using MultiplayerGameBackend.Domain.Entities;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.Processing;

namespace MultiplayerGameBackend.Infrastructure.Images;

public class ImageService(IWebHostEnvironment webHostEnvironment, ILogger<ImageService> logger) : IImageService
{
    private const string ProfilePicturesFolder = "profile-pictures";

    public async Task<string> SaveProfilePictureAsync(Stream imageStream, string fileName, CancellationToken cancellationToken = default)
    {
        // Ensure the profile pictures directory exists
        var profilePicturesPath = Path.Combine(webHostEnvironment.WebRootPath, ProfilePicturesFolder);
        Directory.CreateDirectory(profilePicturesPath);

        // Generate a unique filename
        var uniqueFileName = $"{Guid.NewGuid()}{Path.GetExtension(fileName)}";
        var filePath = Path.Combine(profilePicturesPath, uniqueFileName);

        try
        {
            using var image = await Image.LoadAsync(imageStream, cancellationToken);
            
            // Resize if the image is larger than the max dimensions
            if (image.Width > User.Constraints.ProfilePictureCompressedMaxWidth || 
                image.Height > User.Constraints.ProfilePictureCompressedMaxHeight)
            {
                image.Mutate(x => x.Resize(new ResizeOptions
                {
                    Size = new Size(User.Constraints.ProfilePictureCompressedMaxWidth, 
                                    User.Constraints.ProfilePictureCompressedMaxHeight),
                    Mode = ResizeMode.Max
                }));
            }

            // Save as JPEG with compression
            var encoder = new JpegEncoder { Quality = 85 };
            await image.SaveAsync(filePath, encoder, cancellationToken);
            logger.LogInformation("Profile picture saved: {FileName}", uniqueFileName);

            // Return the relative URL path
            return $"/{ProfilePicturesFolder}/{uniqueFileName}";
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error saving profile picture");
            if (File.Exists(filePath))
                File.Delete(filePath);
            
            throw;
        }
    }

    public Task DeleteProfilePictureAsync(string fileName, CancellationToken cancellationToken = default)
    {
        try
        {
            var fileNameOnly = Path.GetFileName(fileName);
            var filePath = Path.Combine(webHostEnvironment.WebRootPath, ProfilePicturesFolder, fileNameOnly);

            if (File.Exists(filePath))
            {
                File.Delete(filePath);
                logger.LogInformation("Profile picture deleted: {FileName}", fileNameOnly);
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error deleting profile picture: {FileName}", fileName);
        }

        return Task.CompletedTask;
    }
}

