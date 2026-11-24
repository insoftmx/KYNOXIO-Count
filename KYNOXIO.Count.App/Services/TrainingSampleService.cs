using System.IO.Compression;
using System.Text.Json;
using KYNOXIO.Count.Core.Models;
using KYNOXIO.Count.Core.Repositories;
using Microsoft.Maui.Storage;
using Microsoft.Maui.ApplicationModel;

namespace KYNOXIO.Count.App.Services;

/// <summary>Administra fotografías y metadatos del banco de entrenamiento sin tocar sesiones.</summary>
public sealed class TrainingSampleService
{
    private readonly ITrainingSampleRepository _repository;
    private readonly string _imageDirectory;

    public TrainingSampleService(ITrainingSampleRepository repository)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _imageDirectory = Path.Combine(FileSystem.AppDataDirectory, "TrainingImages");
    }

    public async Task<TrainingSample> CreateDraftAsync(
        FileResult photo,
        string materialGroup,
        string className,
        int? verifiedCount,
        string lighting,
        string cameraDistance,
        string cameraAngle,
        string occlusion,
        string notes)
    {
        ArgumentNullException.ThrowIfNull(photo);

        if (materialGroup is not ("IndividualParts" or "StackedMaterials"))
            throw new ArgumentException("Selecciona un grupo de material válido.", nameof(materialGroup));
        if (string.IsNullOrWhiteSpace(className) || className.Trim().Length > 160)
            throw new ArgumentException("Escribe el nombre del material (máximo 160 caracteres).", nameof(className));
        if (verifiedCount < 0)
            throw new ArgumentOutOfRangeException(nameof(verifiedCount), "La cantidad verificada no puede ser negativa.");
        if (notes.Length > 1000)
            throw new ArgumentException("Las notas no pueden superar 1000 caracteres.", nameof(notes));

        Directory.CreateDirectory(_imageDirectory);
        var id = Guid.NewGuid().ToString("N");
        var extension = GetSafeExtension(photo.FileName);
        var destination = Path.Combine(_imageDirectory, id + extension);

        try
        {
            await using (var source = await photo.OpenReadAsync())
            await using (var output = new FileStream(destination, FileMode.CreateNew, FileAccess.Write, FileShare.None))
                await source.CopyToAsync(output);

            var sample = new TrainingSample
            {
                Id = id,
                CreatedAtUtc = DateTime.UtcNow,
                ImageRelativePath = $"TrainingImages/{id}{extension}",
                MaterialGroup = materialGroup,
                ClassName = className.Trim(),
                VerifiedCount = verifiedCount,
                CaptureConditionsJson = JsonSerializer.Serialize(new
                {
                    lighting = lighting.Trim(),
                    cameraDistance = cameraDistance.Trim(),
                    cameraAngle = cameraAngle.Trim(),
                    occlusion = occlusion.Trim()
                }),
                AnnotationsJson = "[]",
                Status = "Draft",
                Notes = notes.Trim()
            };

            await _repository.AddAsync(sample);
            return sample;
        }
        catch
        {
            if (File.Exists(destination))
                File.Delete(destination);
            throw;
        }
    }

    public async Task<IReadOnlyList<TrainingSample>> GetAllAsync() =>
        await _repository.GetAllAsync();

    public string GetImageAbsolutePath(string relativePath)
    {
        var fileName = Path.GetFileName((relativePath ?? string.Empty).Replace('\\', '/'));
        return Path.Combine(_imageDirectory, fileName);
    }

    /// <summary>Exporta todas las muestras y sus fotografías en un paquete ZIP portable.</summary>
    public async Task<string> ExportAndShareDatasetAsync()
    {
        var samples = await _repository.GetAllAsync();
        Directory.CreateDirectory(FileSystem.CacheDirectory);
        var fileName = $"KYNOXIO-Count-TrainingBank-{DateTime.Now:yyyyMMdd-HHmmss}.zip";
        var zipPath = Path.Combine(FileSystem.CacheDirectory, fileName);
        if (File.Exists(zipPath))
            File.Delete(zipPath);

        var manifest = new TrainingDatasetManifest
        {
            FormatVersion = 1,
            CreatedAtUtc = DateTime.UtcNow,
            Samples = samples.ToList()
        };

        using (var archive = ZipFile.Open(zipPath, ZipArchiveMode.Create))
        {
            var manifestEntry = archive.CreateEntry("kynoxio-training-dataset.json", CompressionLevel.Optimal);
            await using (var stream = manifestEntry.Open())
                await JsonSerializer.SerializeAsync(stream, manifest, new JsonSerializerOptions { WriteIndented = true });

            foreach (var sample in samples)
            {
                if (string.IsNullOrWhiteSpace(sample.Id) ||
                    sample.Id.Length > 32 ||
                    sample.Id.Any(character => !char.IsAsciiLetterOrDigit(character) && character != '-'))
                    throw new InvalidDataException("El banco contiene una muestra con identificador no válido.");

                var sourcePath = GetImageAbsolutePath(sample.ImageRelativePath);
                if (!File.Exists(sourcePath))
                    continue;

                var extension = Path.GetExtension(sourcePath).ToLowerInvariant();
                if (extension is not (".jpg" or ".jpeg" or ".png" or ".webp" or ".heic"))
                    continue;

                var entry = archive.CreateEntry($"images/{sample.Id}{extension}", CompressionLevel.Optimal);
                await using var input = File.OpenRead(sourcePath);
                await using var output = entry.Open();
                await input.CopyToAsync(output);
            }
        }

        await Share.Default.RequestAsync(new ShareFileRequest
        {
            Title = "Exportar banco de imágenes KYNOXIO Count",
            File = new ShareFile(zipPath)
        });

        return zipPath;
    }

    private sealed class TrainingDatasetManifest
    {
        public int FormatVersion { get; set; }
        public DateTime CreatedAtUtc { get; set; }
        public List<TrainingSample> Samples { get; set; } = new();
    }

    private static string GetSafeExtension(string? fileName)
    {
        var extension = Path.GetExtension(fileName);
        return new[] { ".jpg", ".jpeg", ".png", ".webp", ".heic" }
            .Contains(extension, StringComparer.OrdinalIgnoreCase)
            ? extension.ToLowerInvariant()
            : ".jpg";
    }
}