using KYNOXIO.Count.Core.Models;
using KYNOXIO.Count.Core.Repositories;
using Microsoft.Maui.Storage;

namespace KYNOXIO.Count.App.Services;

/// <summary>
/// Coordina el guardado de imágenes y la persistencia de sesiones y conteos manuales.
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
                Confidence = null,
                ItemDescription = string.Empty,
                ManualCount = null,
                Notes = string.Empty,
                CountMethod = "Manual"
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

    /// <summary>
    /// Registra el resultado contado y verificado por una persona.
    /// No modifica ni simula resultados de inteligencia artificial.
    /// </summary>
    public async Task<CountSession> SaveManualCountAsync(
        string sessionId,
        string itemDescription,
        int quantity,
        string? notes)
    {
        if (string.IsNullOrWhiteSpace(sessionId))
            throw new ArgumentException("La sesión es obligatoria.", nameof(sessionId));
        if (string.IsNullOrWhiteSpace(itemDescription))
            throw new ArgumentException("Describe el material o componente.", nameof(itemDescription));
        if (itemDescription.Trim().Length > 160)
            throw new ArgumentException("La descripción no puede superar 160 caracteres.", nameof(itemDescription));
        if (quantity < 0)
            throw new ArgumentOutOfRangeException(nameof(quantity), "La cantidad no puede ser negativa.");
        if ((notes?.Length ?? 0) > 1000)
            throw new ArgumentException("Las notas no pueden superar 1000 caracteres.", nameof(notes));

        var session = await _repository.GetByIdAsync(sessionId)
            ?? throw new InvalidOperationException("La sesión ya no existe.");

        session.ItemDescription = itemDescription.Trim();
        session.ManualCount = quantity;
        session.Notes = notes?.Trim() ?? string.Empty;
        session.CountMethod = "Manual";
        session.Status = "CompletedManual";

        await _repository.UpdateAsync(session);
        return session;
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