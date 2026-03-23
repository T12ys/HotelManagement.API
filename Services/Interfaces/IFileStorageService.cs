namespace HotelWebApplication.Services.Interfaces;

public interface IFileStorageService
{
    // returns a relative URL, e.g. "/uploads/abc.jpg"
    Task<string> SaveFileAsync(IFormFile file, CancellationToken ct = default);
    Task DeleteFileAsync(string relativeUrl, CancellationToken ct = default);
}
