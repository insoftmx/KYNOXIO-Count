using KYNOXIO.Count.Core.Models;
using KYNOXIO.Count.Infrastructure.Persistence;
using SQLite;

namespace KYNOXIO.Count.Tests;

/// <summary>
/// Verifica persistencia y migración de sesiones sin depender de la interfaz móvil.
/// </summary>
public sealed class SqliteCountSessionRepositoryTests
{
    [Fact]
    public async Task Repository_PersistsAndUpdatesManualCount()
    {
        var databasePath = CreateDatabasePath();

        try
        {
            var repository = new SqliteCountSessionRepository(databasePath);
            var session = CreateSession();

            await repository.AddAsync(session);
            var stored = await repository.GetByIdAsync(session.Id);

            Assert.NotNull(stored);
            Assert.Equal(session.Folio, stored!.Folio);
            Assert.Null(stored.ManualCount);

            stored.ItemDescription = "Rondana galvanizada";
            stored.ManualCount = 125;
            stored.Notes = "Conteo físico por lote";
            stored.Status = "CompletedManual";
            await repository.UpdateAsync(stored);

            var updated = await repository.GetByIdAsync(session.Id);
            Assert.NotNull(updated);
            Assert.Equal("Rondana galvanizada", updated!.ItemDescription);
            Assert.Equal(125, updated.ManualCount);
            Assert.Equal("Conteo físico por lote", updated.Notes);
            Assert.Equal("CompletedManual", updated.Status);

            var history = await repository.GetAllAsync();
            Assert.Single(history);
            Assert.Equal(125, history[0].ManualCount);
        }
        finally
        {
            DeleteDatabaseFiles(databasePath);
        }
    }

    [Fact]
    public async Task Repository_MigratesExistingDatabaseWithoutDeletingSessions()
    {
        SQLitePCL.Batteries_V2.Init();
        var databasePath = CreateDatabasePath();
        var originalDate = new DateTime(2026, 11, 23, 17, 0, 0, DateTimeKind.Utc);

        try
        {
            // Simula el esquema anterior que ya existe en los teléfonos de prueba.
            using (var legacyDatabase = new SQLiteConnection(databasePath))
            {
                legacyDatabase.CreateTable<LegacyCountSessionRecord>();
                legacyDatabase.Insert(new LegacyCountSessionRecord
                {
                    Id = "legacy-session-001",
                    Folio = "KC-20261123-110000-ABCD",
                    CreatedAtUtc = originalDate,
                    ImageRelativePath = "Images/legacy.jpg",
                    Status = "Pending",
                    DetectedCount = null,
                    Confidence = null
                });
            }

            var repository = new SqliteCountSessionRepository(databasePath);
            var migrated = await repository.GetByIdAsync("legacy-session-001");

            Assert.NotNull(migrated);
            Assert.Equal("KC-20261123-110000-ABCD", migrated!.Folio);
            Assert.Equal("Images/legacy.jpg", migrated.ImageRelativePath);
            Assert.Equal(originalDate, migrated.CreatedAtUtc);
            Assert.Null(migrated.ManualCount);
            Assert.Equal(string.Empty, migrated.ItemDescription);
            Assert.Equal("Manual", migrated.CountMethod);
        }
        finally
        {
            DeleteDatabaseFiles(databasePath);
        }
    }

    private static CountSession CreateSession() => new()
    {
        Id = Guid.NewGuid().ToString("N"),
        Folio = "KC-20261123-TEST-0001",
        CreatedAtUtc = DateTime.UtcNow,
        ImageRelativePath = "Images/test.jpg",
        Status = "Pending",
        CountMethod = "Manual"
    };

    private static string CreateDatabasePath() =>
        Path.Combine(Path.GetTempPath(), $"kynoxio-count-{Guid.NewGuid():N}.db3");

    private static void DeleteDatabaseFiles(string path)
    {
        // SQLiteAsyncConnection mantiene el archivo abierto durante la vida del proceso.
        // Los archivos temporales usan nombres únicos y el runner los elimina al finalizar.
        foreach (var file in new[] { path, path + "-shm", path + "-wal" })
        {
            try
            {
                if (File.Exists(file))
                    File.Delete(file);
            }
            catch (IOException)
            {
                // El archivo sigue bloqueado por SQLite; no afecta el aislamiento de la prueba.
            }
        }
    }

    [Table("CountSessions")]
    public sealed class LegacyCountSessionRecord
    {
        public LegacyCountSessionRecord() { }
        [PrimaryKey]
        public string Id { get; set; } = string.Empty;
        public string Folio { get; set; } = string.Empty;
        public DateTime CreatedAtUtc { get; set; }
        public string ImageRelativePath { get; set; } = string.Empty;
        public string Status { get; set; } = "Pending";
        public int? DetectedCount { get; set; }
        public double? Confidence { get; set; }
    }
}