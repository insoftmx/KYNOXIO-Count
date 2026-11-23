using KYNOXIO.Count.Core.Models;
using KYNOXIO.Count.Core.Repositories;
using Microsoft.Maui.Storage;

namespace KYNOXIO.Count.App.Services;

/// <summary>
/// Coordina el guardado de la imagen original y el registro SQLite.
/// Si la escritura de la base falla, elimina la imagen para no dejar archivos huérfanos.
/// </summary>
public sealed class CountSessionService
{
    private readonly ICountSessionRepository _repository;
    private readonly string _imageDirectory;

    public CountSessionService(ICountSessionRepository repository)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _imageDirectory = Path.Combine(FileSystem.AppDataDirectory, "Images");
    }

    public async Task<CountSession> CreateFromPhotoAsync(FileResult photo)
    {
        ArgumentNullException.ThrowIfNull(photo);

        Directory.CreateDirectory(_imageDirectory);

        var id = Guid.NewGuid().ToString("N");
        var extension = GetSafeExtension(photo.FileName);
        var fileName = id + extension;
        var destinationPath = Path.Combine(_imageDirectory, fileName);
        var relativePath = $"Images/{fileName}";

        try
        {
            await using (var source = await photo.OpenReadAsync())
            await using (var destination = new FileStream(
                destinationPath, FileMode.CreateNew, FileAccess.Write, FileShare.None))
            {
                await source.CopyToAsync(destination);
            }

            var createdAt = DateTime.UtcNow;
            var session = new CountSession
            {
                Id = id,
                Folio = $"KC-{createdAt.ToLocalTime():yyyyMMdd-HHmmss}-{id[..4].ToUpperInvariant()}",
                CreatedAtUtc = createdAt,
                ImageRelativePath = relativePath,
                Status = "Pending",
                DetectedCount = null,
                Confidence = null
            };

            await _repository.AddAsync(session);
            return session;
        }
        catch
        {
            // El identificador es único; cualquier archivo parcial pertenece a este intento.
            if (File.Exists(destinationPath))
                File.Delete(destinationPath);

            throw;
        }
    }

    public string GetImageAbsolutePath(string relativePath)
    {
        var fileName = Path.GetFileName(relativePath.Replace('\\', '/'));
        return Path.Combine(_imageDirectory, fileName);
    }

    public Task<IReadOnlyList<CountSession>> GetHistoryAsync() => _repository.GetAllAsync();

    public Task<CountSession?> GetSessionAsync(string id) => _repository.GetByIdAsync(id);

    private static string GetSafeExtension(string? fileName)
    {
        var extension = Path.GetExtension(fileName);
        var allowed = new[] { ".jpg", ".jpeg", ".png", ".webp", ".heic" };

        return allowed.Contains(extension, StringComparer.OrdinalIgnoreCase)
            ? extension.ToLowerInvariant()
            : ".jpg";
    }
}