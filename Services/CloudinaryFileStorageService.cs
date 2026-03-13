using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using HotelWebApplication.Services.Interfaces;
using Microsoft.Extensions.Configuration;

namespace HotelWebApplication.Services;

public class CloudinaryFileStorageService : IFileStorageService
{
    private readonly Cloudinary _cloudinary;
    private const long MaxFileSize = 5 * 1024 * 1024;
    private static readonly string[] AllowedExtensions = [".jpg", ".jpeg", ".png", ".webp"];

    public CloudinaryFileStorageService(IConfiguration config)
    {
        var account = new Account(
            config["Cloudinary:CloudName"],
            config["Cloudinary:ApiKey"],
            config["Cloudinary:ApiSecret"]
        );
        _cloudinary = new Cloudinary(account) { Api = { Secure = true } };
    }

    public async Task<string> SaveFileAsync(IFormFile file, CancellationToken ct = default)
    {
        if (file == null || file.Length == 0)
            throw new ArgumentException("File is empty");

        if (file.Length > MaxFileSize)
            throw new InvalidOperationException("File too large");

        var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (!AllowedExtensions.Contains(ext))
            throw new InvalidOperationException("Invalid file type");

        await using var stream = file.OpenReadStream();

        var uploadParams = new ImageUploadParams
        {
            File = new FileDescription(file.FileName, stream),
            Folder = "hotel/rooms",
            PublicId = Guid.NewGuid().ToString("N"),
            Overwrite = false
        };

        var result = await _cloudinary.UploadAsync(uploadParams, ct);

        if (result.Error != null)
            throw new InvalidOperationException($"Cloudinary upload error: {result.Error.Message}");

        // Возвращаем полный HTTPS URL вместо относительного пути
        return result.SecureUrl.ToString();
    }

    public async Task DeleteFileAsync(string fileUrl, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(fileUrl)) return;

        // Извлекаем publicId из URL: "hotel/rooms/abc123"
        var publicId = ExtractPublicId(fileUrl);
        if (publicId == null) return;

        var deleteParams = new DeletionParams(publicId);
        await _cloudinary.DestroyAsync(deleteParams);
    }

    private static string? ExtractPublicId(string url)
    {
        // URL вида: https://res.cloudinary.com/{cloud}/image/upload/v123/hotel/rooms/abc123.jpg
        try
        {
            var uri = new Uri(url);
            var segments = uri.AbsolutePath.Split('/');
            // Найти индекс "upload", взять всё после него без расширения
            var uploadIndex = Array.IndexOf(segments, "upload");
            if (uploadIndex < 0 || uploadIndex + 2 >= segments.Length) return null;

            // Пропустить версию (v1234567890)
            var startIndex = segments[uploadIndex + 1].StartsWith("v") ? uploadIndex + 2 : uploadIndex + 1;
            var pathParts = segments[startIndex..];
            var fullPath = string.Join("/", pathParts);
            return Path.ChangeExtension(fullPath, null); // убрать расширение
        }
        catch
        {
            return null;
        }
    }
}