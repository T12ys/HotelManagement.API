namespace HotelWebApplication.Services.Interfaces;

public interface IFileStorageService
{
    // возвращает относительный URL, напр. "/uploads/abc.jpg"
    Task<string> SaveFileAsync(IFormFile file, CancellationToken ct = default);
    Task DeleteFileAsync(string relativeUrl, CancellationToken ct = default);
}
