using KYNOXIO.Count.Core.Models;
using KYNOXIO.Count.Core.Repositories;
using SQLite;

namespace KYNOXIO.Count.Infrastructure.Persistence;

/// <summary>
/// Implementación SQLite local. Las migraciones agregan columnas sin borrar
/// los conteos que ya existan en los dispositivos de prueba.
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

    public async Task UpdateAsync(CountSession session)
    {
        ArgumentNullException.ThrowIfNull(session);
        await EnsureInitializedAsync();

        var affected = await _database.UpdateAsync(ToRecord(session));
        if (affected == 0)
            throw new InvalidOperationException("La sesión que intentas actualizar ya no existe.");
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

            // Migración aditiva: mantiene las sesiones guardadas antes de esta versión.
            var columns = await _database.QueryAsync<SqliteColumnInfo>(
                "PRAGMA table_info('CountSessions')");
            await AddColumnIfMissingAsync(columns, "ItemDescription",
                "ALTER TABLE CountSessions ADD COLUMN ItemDescription TEXT NOT NULL DEFAULT ''");
            await AddColumnIfMissingAsync(columns, "ManualCount",
                "ALTER TABLE CountSessions ADD COLUMN ManualCount INTEGER NULL");
            await AddColumnIfMissingAsync(columns, "Notes",
                "ALTER TABLE CountSessions ADD COLUMN Notes TEXT NOT NULL DEFAULT ''");
            await AddColumnIfMissingAsync(columns, "CountMethod",
                "ALTER TABLE CountSessions ADD COLUMN CountMethod TEXT NOT NULL DEFAULT 'Manual'");

            _isInitialized = true;
        }
        finally
        {
            _initializationLock.Release();
        }
    }

    private async Task AddColumnIfMissingAsync(
        IReadOnlyCollection<SqliteColumnInfo> columns,
        string columnName,
        string sql)
    {
        if (columns.Any(column =>
            string.Equals(column.Name, columnName, StringComparison.OrdinalIgnoreCase)))
            return;

        await _database.ExecuteAsync(sql);
    }

    private static CountSessionRecord ToRecord(CountSession session) => new()
    {
        Id = session.Id,
        Folio = session.Folio,
        CreatedAtUtc = session.CreatedAtUtc,
        ImageRelativePath = session.ImageRelativePath,
        Status = session.Status,
        DetectedCount = session.DetectedCount,
        Confidence = session.Confidence,
        ItemDescription = session.ItemDescription,
        ManualCount = session.ManualCount,
        Notes = session.Notes,
        CountMethod = session.CountMethod
    };

    private static CountSession ToModel(CountSessionRecord record) => new()
    {
        Id = record.Id,
        Folio = record.Folio,
        CreatedAtUtc = record.CreatedAtUtc,
        ImageRelativePath = record.ImageRelativePath,
        Status = record.Status,
        DetectedCount = record.DetectedCount,
        Confidence = record.Confidence,
        ItemDescription = record.ItemDescription,
        ManualCount = record.ManualCount,
        Notes = record.Notes,
        CountMethod = record.CountMethod
    };

    // SQLite PRAGMA devuelve el nombre de la columna en la propiedad "name".
    private sealed class SqliteColumnInfo
    {
        [Column("name")]
        public string Name { get; set; } = string.Empty;
    }
}