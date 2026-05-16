
using CloudinaryDotNet;
using CloudinaryDotNet.Actions;

namespace HMS.API.Services
{
    public class CloudinaryUploadService : IUploadService
    {
        private readonly Cloudinary _cloudinary;

        private static readonly string[] AllowedTypes = ["image/jpeg", "image/png", "image/webp"];
        private const long MaxBytes = 5 * 1024 * 1024; // 5 MB

        public CloudinaryUploadService(IConfiguration config)
        {
            var cloudName = config["Cloudinary:CloudName"]!;
            var apiKey = config["Cloudinary:ApiKey"]!;
            var apiSecret = config["Cloudinary:ApiSecret"]!;

            var account = new Account(cloudName, apiKey, apiSecret);
            _cloudinary = new Cloudinary(account) { Api = { Secure = true } };
        }

        public async Task<string> UploadImageAsync(IFormFile file, string folder)
        {
            if (file.Length == 0)
                throw new ArgumentException("File is empty.");

            if (file.Length > MaxBytes)
                throw new ArgumentException("File exceeds the 5 MB limit.");

            if (!AllowedTypes.Contains(file.ContentType.ToLower()))
                throw new ArgumentException("Only JPEG, PNG, and WebP images are allowed.");

            await using var stream = file.OpenReadStream();

            var uploadParams = new ImageUploadParams
            {
                File = new FileDescription(file.FileName, stream),
                Folder = $"hms/{folder}",
                Transformation = new Transformation().Quality("auto").FetchFormat("auto"),
                UseFilenameAsDisplayName = true,
                UniqueFilename = true,
                Overwrite = false
            };

            var result = await _cloudinary.UploadAsync(uploadParams);

            if (result.Error != null)
                throw new InvalidOperationException($"Cloudinary upload failed: {result.Error.Message}");

            return result.SecureUrl.ToString();
        }

        public async Task DeleteImageAsync(string publicId)
        {
            var deleteParams = new DeletionParams(publicId);
            await _cloudinary.DestroyAsync(deleteParams);
        }
    }
}
