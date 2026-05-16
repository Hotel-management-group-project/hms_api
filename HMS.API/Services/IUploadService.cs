
namespace HMS.API.Services
{
    public interface IUploadService
    {
        Task<string> UploadImageAsync(IFormFile file, string folder);
        Task DeleteImageAsync(string publicId);
    }
}
