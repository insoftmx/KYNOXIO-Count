using KYNOXIO.Count.Core.Models;
using KYNOXIO.Count.Core.Repositories;
using SQLite;

namespace KYNOXIO.Count.Infrastructure.Persistence;

/// <summary>
/// Implementación SQLite local. Inicializa la tabla una sola vez y serializa
/// el primer acceso para evitar carreras al abrir la aplicación.
/// </summary>
public sealed class SqliteCountSessionRepository : ICountSessionRepository
{
    private readonly SQLiteAsyncConnection _database;
    private readonly SemaphoreSlim _initializationLock = new(1, 1);
    private bool _isInitialized;

    public SqliteCountSessionRepository(string databasePath)
    {
        if (string.IsNullOrWhiteSpace(databasePath))
            throw new ArgumentException("La ruta de SQLite es obligatoria.", nameof(databasePath));

        SQLitePCL.Batteries_V2.Init();
        _database = new SQLiteAsyncConnection(databasePath);
    }

    public async Task AddAsync(CountSession session)
    {
        ArgumentNullException.ThrowIfNull(session);
        await EnsureInitializedAsync();
        await _database.InsertAsync(ToRecord(session));
    }

    public async Task<IReadOnlyList<CountSession>> GetAllAsync()
    {
        await EnsureInitializedAsync();
        var records = await _database.Table<CountSessionRecord>()
            .OrderByDescending(item => item.CreatedAtUtc)
            .ToListAsync();

        return records.Select(ToModel).ToList();
    }

    public async Task<CountSession?> GetByIdAsync(string id)
    {
        if (string.IsNullOrWhiteSpace(id))
            return null;

        await EnsureInitializedAsync();
        var record = await _database.Table<CountSessionRecord>()
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

            await _database.CreateTableAsync<CountSessionRecord>();
            _isInitialized = true;
        }
        finally
        {
            _initializationLock.Release();
        }
    }

    private static CountSessionRecord ToRecord(CountSession session) => new()
    {
        Id = session.Id,
        Folio = session.Folio,
        CreatedAtUtc = session.CreatedAtUtc,
        ImageRelativePath = session.ImageRelativePath,
        Status = session.Status,
        DetectedCount = session.DetectedCount,
        Confidence = session.Confidence
    };

    private static CountSession ToModel(CountSessionRecord record) => new()
    {
        Id = record.Id,
        Folio = record.Folio,
        CreatedAtUtc = record.CreatedAtUtc,
        ImageRelativePath = record.ImageRelativePath,
        Status = record.Status,
        DetectedCount = record.DetectedCount,
        Confidence = record.Confidence
    };
}