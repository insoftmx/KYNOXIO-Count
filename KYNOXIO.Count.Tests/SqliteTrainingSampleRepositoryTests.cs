using KYNOXIO.Count.Core.Models;
using KYNOXIO.Count.Infrastructure.Persistence;

namespace KYNOXIO.Count.Tests;

/// <summary>Comprueba que las muestras de entrenamiento se persisten aisladas de las sesiones.</summary>
public sealed class SqliteTrainingSampleRepositoryTests
{
    [Fact]
    public async Task Repository_PersistsVerifiedSampleWithoutChangingOperationalSessions()
    {
        SQLitePCL.Batteries_V2.Init();
        var databasePath = Path.Combine(Path.GetTempPath(), $"kynoxio-training-{Guid.NewGuid():N}.db3");

        try
        {
            var sessionRepository = new SqliteCountSessionRepository(databasePath);
            var trainingRepository = new SqliteTrainingSampleRepository(databasePath);
            var session = new CountSession
            {
                Id = Guid.NewGuid().ToString("N"),
                Folio = "KC-TRAINING-ISOLATION",
                CreatedAtUtc = DateTime.UtcNow,
                ImageRelativePath = "Images/operational.jpg",
                Status = "Pending",
                CountMethod = "Manual"
            };
            await sessionRepository.AddAsync(session);

            var sample = new TrainingSample
            {
                Id = Guid.NewGuid().ToString("N"),
                CreatedAtUtc = DateTime.UtcNow,
                ImageRelativePath = "Training/individual-sample.jpg",
                MaterialGroup = "IndividualParts",
                ClassName = "Rondana galvanizada",
                VerifiedCount = 125,
                CaptureConditionsJson = "{\"lighting\":\"industrial\"}",
                AnnotationsJson = "[]",
                Status = "Ready",
                Notes = "Cantidad comprobada físicamente"
            };

            await trainingRepository.AddAsync(sample);

            var storedSample = await trainingRepository.GetByIdAsync(sample.Id);
            var storedSessions = await sessionRepository.GetAllAsync();

            Assert.NotNull(storedSample);
            Assert.Equal(125, storedSample!.VerifiedCount);
            Assert.Equal("IndividualParts", storedSample.MaterialGroup);
            Assert.Equal("Ready", storedSample.Status);
            Assert.Single(storedSessions);
            Assert.Equal(session.Id, storedSessions[0].Id);
        }
        finally
        {
            DeleteDatabaseFiles(databasePath);
        }
    }

    [Fact]
    public async Task Repository_RejectsReadySampleWithoutPhysicallyVerifiedCount()
    {
        SQLitePCL.Batteries_V2.Init();
        var databasePath = Path.Combine(Path.GetTempPath(), $"kynoxio-training-{Guid.NewGuid():N}.db3");

        try
        {
            var repository = new SqliteTrainingSampleRepository(databasePath);
            var sample = new TrainingSample
            {
                Id = Guid.NewGuid().ToString("N"),
                CreatedAtUtc = DateTime.UtcNow,
                ImageRelativePath = "Training/stacked-sample.jpg",
                MaterialGroup = "StackedMaterials",
                ClassName = "Hojas de cartón",
                VerifiedCount = null,
                Status = "Ready"
            };

            await Assert.ThrowsAsync<ArgumentException>(() => repository.AddAsync(sample));
        }
        finally
        {
            DeleteDatabaseFiles(databasePath);
        }
    }

    private static void DeleteDatabaseFiles(string path)
    {
        foreach (var file in new[] { path, path + "-shm", path + "-wal" })
        {
            try
            {
                if (File.Exists(file))
                    File.Delete(file);
            }
            catch (IOException)
            {
                // SQLite puede conservar el archivo abierto hasta finalizar el proceso de pruebas.
            }
        }
    }
}