using System.IO.Compression;
using System.Text.Json;
using KYNOXIO.Count.Core.Models;
using KYNOXIO.Count.Core.Repositories;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Storage;

namespace KYNOXIO.Count.App.Services;

/// <summary>
/// Exporta sesiones y fotografías a un ZIP portable y permite restaurarlas
/// sin depender de una copia binaria de SQLite ni de la misma versión del esquema.
/// </summary>
public sealed class BackupService
{
    private const int BackupFormatVersion = 1;
    private const string ManifestName = "kynoxio-count-backup.json";
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNameCaseInsensitive = true
    };

    private readonly ICountSessionRepository _repository;
    private readonly string _imageDirectory;

    public BackupService(ICountSessionRepository repository)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _imageDirectory = Path.Combine(FileSystem.AppDataDirectory, "Images");
    }

    /// <summary>Genera el ZIP en caché y abre el diálogo nativo para compartirlo o guardarlo.</summary>
    public async Task<string> CreateAndShareBackupAsync()
    {
        var sessions = await _repository.GetAllAsync();
        var backup = new BackupManifest
        {
            FormatVersion = BackupFormatVersion,
            CreatedAtUtc = DateTime.UtcNow,
            Sessions = sessions.ToList()
        };

        Directory.CreateDirectory(FileSystem.CacheDirectory);
        var fileName = $"KYNOXIO-Count-Backup-{DateTime.Now:yyyyMMdd-HHmmss}.zip";
        var backupPath = Path.Combine(FileSystem.CacheDirectory, fileName);
        if (File.Exists(backupPath))
            File.Delete(backupPath);

        using (var archive = ZipFile.Open(backupPath, ZipArchiveMode.Create))
        {
            var manifestEntry = archive.CreateEntry(ManifestName, CompressionLevel.Optimal);
            await using (var stream = manifestEntry.Open())
                await JsonSerializer.SerializeAsync(stream, backup, JsonOptions);

            foreach (var session in sessions)
            {
                if (!IsSafeId(session.Id))
                    throw new InvalidDataException("Una sesión contiene un identificador no válido.");

                var sourcePath = GetImagePath(session.ImageRelativePath);
                if (!File.Exists(sourcePath))
                    continue;

                var extension = Path.GetExtension(sourcePath).ToLowerInvariant();
                if (!IsAllowedImageExtension(extension))
                    continue;

                var entry = archive.CreateEntry($"Images/{session.Id}{extension}", CompressionLevel.Optimal);
                await using var input = File.OpenRead(sourcePath);
                await using var output = entry.Open();
                await input.CopyToAsync(output);
            }
        }

        await Share.Default.RequestAsync(new ShareFileRequest
        {
            Title = "Respaldar KYNOXIO Count",
            File = new ShareFile(backupPath)
        });

        return backupPath;
    }

    /// <summary>
    /// Restaura sesiones por ID: agrega las nuevas y actualiza las existentes.
    /// Las rutas del ZIP nunca se usan como rutas físicas; los archivos se renombran localmente.
    /// </summary>
    public async Task<RestoreResult> RestoreAsync(FileResult backupFile)
    {
        ArgumentNullException.ThrowIfNull(backupFile);
        var tempDirectory = Path.Combine(FileSystem.CacheDirectory, $"count-restore-{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDirectory);

        try
        {
            var zipPath = Path.Combine(tempDirectory, "restore.zip");
            await using (var source = await backupFile.OpenReadAsync())
            await using (var destination = File.Create(zipPath))
                await source.CopyToAsync(destination);

            using var archive = ZipFile.OpenRead(zipPath);
            var manifestEntry = archive.GetEntry(ManifestName)
                ?? throw new InvalidDataException("El archivo no contiene un respaldo válido de KYNOXIO Count.");

            BackupManifest? manifest;
            await using (var stream = manifestEntry.Open())
                manifest = await JsonSerializer.DeserializeAsync<BackupManifest>(stream, JsonOptions);

            if (manifest is null || manifest.FormatVersion != BackupFormatVersion || manifest.Sessions is null)
                throw new InvalidDataException("La versión o estructura del respaldo no es compatible.");

            if (manifest.Sessions.Count > 100000)
                throw new InvalidDataException("El respaldo supera el límite de sesiones permitido.");

            var ids = new HashSet<string>(StringComparer.Ordinal);
            foreach (var session in manifest.Sessions)
            {
                if (session is null || !IsSafeId(session.Id) || !ids.Add(session.Id) ||
                    string.IsNullOrWhiteSpace(session.Folio) || session.Folio.Length > 80 ||
                    session.ItemDescription.Length > 160 || session.Notes.Length > 1000 ||
                    session.ManualCount < 0 || session.DetectedCount < 0)
                    throw new InvalidDataException("El respaldo contiene una sesión inválida o duplicada.");
            }

            Directory.CreateDirectory(_imageDirectory);
            var added = 0;
            var updated = 0;
            var missingImages = 0;

            foreach (var session in manifest.Sessions)
            {
                var existing = await _repository.GetByIdAsync(session.Id);
                var originalExtension = Path.GetExtension(session.ImageRelativePath).ToLowerInvariant();
                var imageEntry = IsAllowedImageExtension(originalExtension)
                    ? archive.GetEntry($"Images/{session.Id}{originalExtension}")
                    : null;

                if (imageEntry is not null)
                {
                    var destinationPath = Path.Combine(_imageDirectory, session.Id + originalExtension);
                    var temporaryImage = destinationPath + ".restore";
                    await using (var input = imageEntry.Open())
                    await using (var output = new FileStream(temporaryImage, FileMode.Create, FileAccess.Write, FileShare.None))
                        await input.CopyToAsync(output);

                    if (File.Exists(destinationPath))
                        File.Delete(destinationPath);
                    File.Move(temporaryImage, destinationPath);
                    session.ImageRelativePath = $"Images/{session.Id}{originalExtension}";
                }
                else
                {
                    missingImages++;
                    // Si el archivo no venía en el respaldo, no inventamos ni sobrescribimos una ruta.
                    if (existing is null)
                        session.ImageRelativePath = string.Empty;
                }

                if (existing is null)
                {
                    await _repository.AddAsync(session);
                    added++;
                }
                else
                {
                    await _repository.UpdateAsync(session);
                    updated++;
                }
            }

            return new RestoreResult(added, updated, missingImages);
        }
        finally
        {
            try
            {
                if (Directory.Exists(tempDirectory))
                    Directory.Delete(tempDirectory, recursive: true);
            }
            catch (IOException)
            {
                // La limpieza de caché no debe invalidar una restauración ya completada.
            }
        }
    }

    private string GetImagePath(string relativePath)
    {
        var fileName = Path.GetFileName((relativePath ?? string.Empty).Replace('\\', '/'));
        return string.IsNullOrWhiteSpace(fileName) ? string.Empty : Path.Combine(_imageDirectory, fileName);
    }

    private static bool IsSafeId(string? id) =>
        !string.IsNullOrWhiteSpace(id) &&
        id.Length <= 64 &&
        id.All(character => char.IsAsciiLetterOrDigit(character) || character == '-');

    private static bool IsAllowedImageExtension(string extension) =>
        extension is ".jpg" or ".jpeg" or ".png" or ".webp" or ".heic";

    private sealed class BackupManifest
    {
        public int FormatVersion { get; set; }
        public DateTime CreatedAtUtc { get; set; }
        public List<CountSession> Sessions { get; set; } = new();
    }
}

public sealed record RestoreResult(int AddedSessions, int UpdatedSessions, int MissingImages);
