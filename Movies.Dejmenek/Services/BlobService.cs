using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;

namespace Movies.Dejmenek.Services;

public class BlobService : IBlobService
{
    private readonly BlobServiceClient _blobServiceClient;
    private readonly ILogger<BlobService> _logger;
    private readonly string _containerName;
    public BlobService(BlobServiceClient blobServiceClient, ILogger<BlobService> logger, IConfiguration configuration)
    {
        _blobServiceClient = blobServiceClient;
        _logger = logger;

        _containerName = configuration["AzureStorage:ContainerName"];
        if (string.IsNullOrWhiteSpace(_containerName))
        {
            throw new InvalidOperationException("The Azure Storage container name is not configured. Please set 'AzureStorage:ContainerName' in the configuration.");
        }

        EnsureContainerCreated().GetAwaiter().GetResult();
    }

    private async Task EnsureContainerCreated()
    {
        BlobContainerClient containerClient = _blobServiceClient.GetBlobContainerClient(_containerName);
        await containerClient.CreateIfNotExistsAsync(PublicAccessType.Blob);
    }

    public async Task DeleteAsync(string fileUri)
    {
        BlobContainerClient containerClient = _blobServiceClient.GetBlobContainerClient(_containerName);

        try
        {
            var fileName = Path.GetFileNameWithoutExtension(new Uri(fileUri).LocalPath);
            BlobClient blobClient = containerClient.GetBlobClient(fileName);

            await blobClient.DeleteIfExistsAsync();
        }
        catch (Azure.RequestFailedException ex)
        {
            _logger.LogError(ex, "BlobService: Failed to delete file from Blob Storage. URI: {FileUri}", fileUri);
            throw new InvalidOperationException("An error occurred while deleting the file. Please try again later.");
        }
        catch (UriFormatException ex)
        {
            _logger.LogError(ex, "BlobService: Invalid file URI format. URI: {FileUri}", fileUri);
            throw new ArgumentException("The provided file URI is invalid.");
        }
    }

    public async Task<string> UploadAsync(IFormFile file)
    {
        BlobContainerClient containerClient = _blobServiceClient.GetBlobContainerClient(_containerName);

        try
        {
            var fileId = Guid.NewGuid();
            BlobClient blobClient = containerClient.GetBlobClient(fileId.ToString());

            using var stream = file.OpenReadStream();
            await blobClient.UploadAsync(
                stream,
                new BlobHttpHeaders { ContentType = file.ContentType }
            );

            return blobClient.Uri.ToString();
        }
        catch (Azure.RequestFailedException ex)
        {
            _logger.LogError(ex, "BlobService: Failed to upload file to Blob Storage. FileName: {FileName}", file.FileName);
            throw new InvalidOperationException("An error occurred while uploading the file. Please try again later.");
        }
        catch (ArgumentException ex)
        {
            _logger.LogError(ex, "BlobService: Invalid file parameters. FileName: {FileName}", file.FileName);
            throw new InvalidOperationException("The provided file is invalid. Please check the file and try again.");
        }
    }
}
