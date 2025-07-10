using Azure;
using Movies.Dejmenek.Exceptions;

namespace Movies.Dejmenek.Services;

public class ImageUploadService : IImageUploadService
{
    private readonly IBlobService _blobService;
    private readonly ILogger<ImageUploadService> _logger;
    public ImageUploadService(IBlobService blobService, ILogger<ImageUploadService> logger)
    {
        _blobService = blobService;
        _logger = logger;
    }

    public async Task DeleteAsync(string imageUri)
    {
        if (string.IsNullOrWhiteSpace(imageUri))
            return;

        try
        {
            await _blobService.DeleteAsync(imageUri);
        }
        catch (Exception ex) when (ex is RequestFailedException || ex is UriFormatException)
        {
            throw new ImageDeleteException("Failed to delete image.", ex);
        }
    }

    public async Task<string> UploadAsync(IFormFile? imageFile)
    {
        if (imageFile == null)
            throw new ArgumentException("Image file cannot be null.");

        try
        {
            return await _blobService.UploadAsync(imageFile);
        }
        catch (Exception ex) when (ex is RequestFailedException || ex is ArgumentException)
        {
            throw new ImageUploadException("Failed to upload image.", ex);
        }
    }
}
