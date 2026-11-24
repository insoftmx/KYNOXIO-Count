using KYNOXIO.Count.App.Services;
using KYNOXIO.Count.Core.Repositories;
using KYNOXIO.Count.Infrastructure.Persistence;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Maui.Storage;

namespace KYNOXIO.Count.App;

public static class MauiProgram
{
    public static IServiceProvider Services { get; private set; } = null!;

    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>()
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
            });

        // La base queda dentro del almacenamiento privado de la aplicación.
        var databasePath = Path.Combine(FileSystem.AppDataDirectory, "kynoxio-count.db3");
        builder.Services.AddSingleton<ICountSessionRepository>(
            _ => new SqliteCountSessionRepository(databasePath));
        builder.Services.AddSingleton<ITrainingSampleRepository>(
            _ => new SqliteTrainingSampleRepository(databasePath));
        builder.Services.AddSingleton<CountSessionService>();
        builder.Services.AddSingleton<BackupService>();

#if DEBUG
        builder.Logging.AddDebug();
#endif

        var app = builder.Build();
        Services = app.Services;
        return app;
    }
}