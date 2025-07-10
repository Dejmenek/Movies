namespace Movies.Dejmenek.Services;

public interface IImageUploadService
{
    Task<string> UploadAsync(IFormFile? imageFile);
    Task DeleteAsync(string imageUri);
}
