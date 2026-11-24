using System.Text.Json;
using KYNOXIO.Count.Core.Models;
using KYNOXIO.Count.Core.Repositories;
using SQLite;

namespace KYNOXIO.Count.Infrastructure.Persistence;

/// <summary>
/// Persistencia SQLite de muestras de entrenamiento. Utiliza una tabla separada
/// y no consulta ni modifica CountSessions.
/// </summary>
public sealed class SqliteTrainingSampleRepository : ITrainingSampleRepository
{
    private readonly SQLiteAsyncConnection _database;
    private readonly SemaphoreSlim _initializationLock = new(1, 1);
    private bool _isInitialized;

    public SqliteTrainingSampleRepository(string databasePath)
    {
        if (string.IsNullOrWhiteSpace(databasePath))
            throw new ArgumentException("La ruta de SQLite es obligatoria.", nameof(databasePath));

        SQLitePCL.Batteries_V2.Init();
        _database = new SQLiteAsyncConnection(databasePath);
    }

    public async Task AddAsync(TrainingSample sample)
    {
        Validate(sample);
        await EnsureInitializedAsync();
        await _database.InsertAsync(ToRecord(sample));
    }

    public async Task UpdateAsync(TrainingSample sample)
    {
        Validate(sample);
        await EnsureInitializedAsync();

        var affected = await _database.UpdateAsync(ToRecord(sample));
        if (affected == 0)
            throw new InvalidOperationException("La muestra que intentas actualizar ya no existe.");
    }

    public async Task<IReadOnlyList<TrainingSample>> GetAllAsync()
    {
        await EnsureInitializedAsync();
        var records = await _database.Table<TrainingSampleRecord>()
            .OrderByDescending(item => item.CreatedAtUtc)
            .ToListAsync();

        return records.Select(ToModel).ToList();
    }

    public async Task<TrainingSample?> GetByIdAsync(string id)
    {
        if (string.IsNullOrWhiteSpace(id))
            return null;

        await EnsureInitializedAsync();
        var record = await _database.Table<TrainingSampleRecord>()
            .Where(item => item.Id == id)
            .FirstOrDefaultAsync();

        return record is null ? null : ToModel(record);
    }

    private async Task EnsureInitializedAsync()
    {
        if (_isInitialized)
            return;

        await _initializationLock.WaitAsync();
        try
        {
            if (_isInitialized)
                return;

            await _database.CreateTableAsync<TrainingSampleRecord>();
            _isInitialized = true;
        }
        finally
        {
            _initializationLock.Release();
        }
    }

    private static void Validate(TrainingSample sample)
    {
        ArgumentNullException.ThrowIfNull(sample);
        if (string.IsNullOrWhiteSpace(sample.Id) || sample.Id.Length > 32 ||
            sample.Id.Any(character => !char.IsAsciiLetterOrDigit(character) && character != '-'))
            throw new ArgumentException("El identificador de la muestra no es válido.", nameof(sample));
        if (sample.ImageRelativePath is null || sample.ImageRelativePath.Length > 260)
            throw new ArgumentException("La ruta de imagen no es válida.", nameof(sample));
        if (sample.MaterialGroup is not ("IndividualParts" or "StackedMaterials"))
            throw new ArgumentException("El grupo debe ser IndividualParts o StackedMaterials.", nameof(sample));
        if (string.IsNullOrWhiteSpace(sample.ClassName) || sample.ClassName.Trim().Length > 160)
            throw new ArgumentException("La clase/material es obligatorio y no puede superar 160 caracteres.", nameof(sample));
        if (sample.VerifiedCount < 0)
            throw new ArgumentOutOfRangeException(nameof(sample), "La cantidad verificada no puede ser negativa.");
        if (sample.CaptureConditionsJson is null || sample.CaptureConditionsJson.Length > 20000 ||
            sample.AnnotationsJson is null || sample.AnnotationsJson.Length > 200000)
            throw new ArgumentException("Los metadatos de la muestra superan el límite permitido.", nameof(sample));
        if (sample.Status is not ("Draft" or "Ready" or "Excluded"))
            throw new ArgumentException("El estado de la muestra no es válido.", nameof(sample));
        if (sample.Notes is null || sample.Notes.Length > 1000)
            throw new ArgumentException("Las notas no pueden superar 1000 caracteres.", nameof(sample));
        if (sample.Status == "Ready")
        {
            if (!sample.VerifiedCount.HasValue)
                throw new ArgumentException("Una muestra lista requiere una cantidad comprobada físicamente.", nameof(sample));

            try
            {
                using var annotations = JsonDocument.Parse(sample.AnnotationsJson);
                if (annotations.RootElement.ValueKind != JsonValueKind.Array ||
                    annotations.RootElement.GetArrayLength() == 0)
                    throw new ArgumentException("Una muestra lista requiere anotaciones geométricas de piezas o capas.", nameof(sample));
            }
            catch (JsonException ex)
            {
                throw new ArgumentException("Las anotaciones geométricas no tienen un formato JSON válido.", nameof(sample), ex);
            }
        }
    }

    private static TrainingSampleRecord ToRecord(TrainingSample sample) => new()
    {
        Id = sample.Id,
        CreatedAtUtc = sample.CreatedAtUtc,
        ImageRelativePath = sample.ImageRelativePath,
        MaterialGroup = sample.MaterialGroup,
        ClassName = sample.ClassName.Trim(),
        VerifiedCount = sample.VerifiedCount,
        CaptureConditionsJson = sample.CaptureConditionsJson,
        AnnotationsJson = sample.AnnotationsJson,
        Status = sample.Status,
        Notes = sample.Notes.Trim()
    };

    private static TrainingSample ToModel(TrainingSampleRecord record) => new()
    {
        Id = record.Id,
        CreatedAtUtc = record.CreatedAtUtc,
        ImageRelativePath = record.ImageRelativePath,
        MaterialGroup = record.MaterialGroup,
        ClassName = record.ClassName,
        VerifiedCount = record.VerifiedCount,
        CaptureConditionsJson = record.CaptureConditionsJson,
        AnnotationsJson = record.AnnotationsJson,
        Status = record.Status,
        Notes = record.Notes
    };
}