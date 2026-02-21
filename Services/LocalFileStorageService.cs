using HotelWebApplication.Services.Interfaces;

namespace HotelWebApplication.Services;

public class LocalFileStorageService : IFileStorageService
{
    private readonly IWebHostEnvironment _env;
    private readonly string _uploadFolder = "uploads";
    private const long MaxFileSize = 5 * 1024 * 1024;

    private static readonly string[] AllowedExtensions =
        [".jpg", ".jpeg", ".png", ".webp"];

    public LocalFileStorageService(IWebHostEnvironment env)
    {
        _env = env;
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

        var webRoot = _env.WebRootPath
                      ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");

        var uploadsRoot = Path.Combine(webRoot, _uploadFolder);
        Directory.CreateDirectory(uploadsRoot);

        var fileName = $"{Guid.NewGuid():N}{ext}";
        var fullPath = Path.Combine(uploadsRoot, fileName);

        await using var stream = new FileStream(fullPath, FileMode.CreateNew);
        await file.CopyToAsync(stream, ct);

        return $"/{_uploadFolder}/{fileName}";
    }

    public Task DeleteFileAsync(string relativeUrl, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(relativeUrl))
            return Task.CompletedTask;

        var webRoot = _env.WebRootPath
                      ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");

        var fullPath = Path.Combine(webRoot, relativeUrl.TrimStart('/'));

        if (File.Exists(fullPath))
            File.Delete(fullPath);

        return Task.CompletedTask;
    }
}

