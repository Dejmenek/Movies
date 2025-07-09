namespace Movies.Dejmenek.Services;

public interface IImageUploadService
{
    Task<string?> TryUploadImageAsync(IFormFile imageFile);
    Task DeleteAsync(string imageUri);
}
