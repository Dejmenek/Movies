using Azure;

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
        catch (RequestFailedException ex)
        {
            _logger.LogWarning(ex, "Blob deletion failed due to a request error. URI: {ImageUri}", imageUri);
        }
        catch (UriFormatException ex)
        {
            _logger.LogWarning(ex, "Blob deletion failed due to an invalid URI format: {ImageUri}", imageUri);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error while deleting blob. URI: {ImageUri}", imageUri);
        }
    }

    public async Task<string?> TryUploadImageAsync(IFormFile imageFile)
    {
        try
        {
            return await _blobService.UploadAsync(imageFile);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogError(ex, "Blob upload failed.");
        }
        catch (ArgumentException ex)
        {
            _logger.LogError(ex, "Invalid image file.");
        }

        return null;
    }
}
