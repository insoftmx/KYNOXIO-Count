using System.Text.Json;
using KYNOXIO.Count.Core.Models;
using KYNOXIO.Count.Core.Repositories;
using Microsoft.Maui.Storage;

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

    private static string GetSafeExtension(string? fileName)
    {
        var extension = Path.GetExtension(fileName);
        return new[] { ".jpg", ".jpeg", ".png", ".webp", ".heic" }
            .Contains(extension, StringComparer.OrdinalIgnoreCase)
            ? extension.ToLowerInvariant()
            : ".jpg";
    }
}