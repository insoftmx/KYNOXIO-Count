using KYNOXIO.Count.App.Services;
using Microsoft.Maui.Storage;

namespace KYNOXIO.Count.App;

/// <summary>Pantalla para crear respaldos y recuperar sesiones desde un archivo ZIP.</summary>
public sealed class BackupPage : ContentPage
{
    private readonly BackupService _backupService;
    private readonly Button _exportButton;
    private readonly Button _restoreButton;

    public BackupPage(BackupService backupService)
    {
        _backupService = backupService;
        Title = "Respaldo y recuperación";
        BackgroundColor = Color.FromArgb("#0B1420");

        ToolbarItems.Add(new ToolbarItem
        {
            Text = "Cerrar",
            Command = new Command(async () => await Navigation.PopModalAsync())
        });

        _exportButton = new Button
        {
            Text = "CREAR Y COMPARTIR RESPALDO",
            BackgroundColor = Color.FromArgb("#087E9B"),
            TextColor = Colors.White,
            CornerRadius = 14,
            HeightRequest = 54
        };
        _exportButton.Clicked += OnExportClicked;

        _restoreButton = new Button
        {
            Text = "RESTAURAR RESPALDO",
            BackgroundColor = Color.FromArgb("#183B50"),
            TextColor = Color.FromArgb("#22D3EE"),
            CornerRadius = 14,
            HeightRequest = 54
        };
        _restoreButton.Clicked += OnRestoreClicked;

        Content = new ScrollView
        {
            Content = new VerticalStackLayout
            {
                Padding = 22,
                Spacing = 18,
                Children =
                {
                    new Label
                    {
                        Text = "PROTEGE TU INFORMACIÓN",
                        FontSize = 12,
                        FontAttributes = FontAttributes.Bold,
                        CharacterSpacing = 1.5,
                        TextColor = Color.FromArgb("#22D3EE")
                    },
                    new Label
                    {
                        Text = "Respaldo de conteos",
                        FontSize = 26,
                        FontAttributes = FontAttributes.Bold,
                        TextColor = Colors.White
                    },
                    new Label
                    {
                        Text = "El respaldo incluye tus conteos operativos y el banco de imágenes de entrenamiento, con sus fotografías y metadatos disponibles. Se genera un ZIP portable que puedes guardar fuera del teléfono.",
                        FontSize = 14,
                        TextColor = Color.FromArgb("#A6B7C5")
                    },
                    _exportButton,
                    new BoxView { HeightRequest = 1, Color = Color.FromArgb("#263B4D") },
                    new Label
                    {
                        Text = "La restauración agrega sesiones que no existen y actualiza las que tengan el mismo identificador. No elimina sesiones locales que no estén en el archivo.",
                        FontSize = 13,
                        TextColor = Color.FromArgb("#A6B7C5")
                    },
                    _restoreButton,
                    new Label
                    {
                        Text = "Importante: conserva una copia del ZIP en OneDrive, Google Drive o una computadora. El respaldo no se carga automáticamente a la nube.",
                        FontSize = 12,
                        TextColor = Color.FromArgb("#FBBF24")
                    }
                }
            }
        };
    }

    private async void OnExportClicked(object? sender, EventArgs e)
    {
        if (!_exportButton.IsEnabled)
            return;

        try
        {
            _exportButton.IsEnabled = false;
            var path = await _backupService.CreateAndShareBackupAsync();
            await DisplayAlert(
                "Respaldo preparado",
                $"Se generó el archivo de respaldo. Utiliza el menú de compartir para guardarlo en una ubicación segura.\n\n{Path.GetFileName(path)}",
                "Aceptar");
        }
        catch (Exception ex)
        {
            await DisplayAlert("No se pudo crear el respaldo", ex.Message, "Aceptar");
        }
        finally
        {
            _exportButton.IsEnabled = true;
        }
    }

    private async void OnRestoreClicked(object? sender, EventArgs e)
    {
        if (!_restoreButton.IsEnabled)
            return;

        try
        {
            var file = await FilePicker.Default.PickAsync(new PickOptions
            {
                PickerTitle = "Selecciona un respaldo de KYNOXIO Count",
                FileTypes = new FilePickerFileType(new Dictionary<DevicePlatform, IEnumerable<string>>
                {
                    [DevicePlatform.Android] = new[] { "application/zip", "application/octet-stream" }
                })
            });

            if (file is null)
                return;

            var confirmed = await DisplayAlert(
                "Confirmar restauración",
                "Las sesiones del archivo se agregarán o actualizarán por identificador. Las sesiones locales que no estén en el respaldo se conservarán. ¿Deseas continuar?",
                "Restaurar",
                "Cancelar");

            if (!confirmed)
                return;

            _restoreButton.IsEnabled = false;
            var result = await _backupService.RestoreAsync(file);
            await DisplayAlert(
                "Restauración terminada",
                $"Sesiones nuevas: {result.AddedSessions}\nSesiones actualizadas: {result.UpdatedSessions}\nFotografías de conteos ausentes: {result.MissingImages}\nImágenes de entrenamiento ausentes: {result.MissingTrainingImages}",
                "Aceptar");
        }
        catch (Exception ex)
        {
            await DisplayAlert("No se pudo restaurar el respaldo", ex.Message, "Aceptar");
        }
        finally
        {
            _restoreButton.IsEnabled = true;
        }
    }
}