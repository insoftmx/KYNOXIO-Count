using KYNOXIO.Count.App.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Media;
using Microsoft.Maui.Storage;

namespace KYNOXIO.Count.App;

/// <summary>
/// Pantalla principal de KYNOXIO Count.
/// Conserva el flujo de captura probado y conecta el acceso al historial local.
/// </summary>
public partial class MainPage : ContentPage
{
    private bool _isOpeningPhoto;

    public MainPage()
    {
        InitializeComponent();
    }

    private async void OnNewCountClicked(object sender, EventArgs e)
    {
        var option = await DisplayActionSheet(
            "Selecciona el tipo de captura",
            "Cancelar",
            null,
            "Tomar fotografía",
            "Elegir de galería",
            "Video en vivo");

        if (option == "Tomar fotografía")
            await OpenPhotoAsync(captureFromCamera: true);
        else if (option == "Elegir de galería")
            await OpenPhotoAsync(captureFromCamera: false);
        else if (option == "Video en vivo")
            await ShowNextStageMessageAsync("Conteo mediante video en vivo");
    }

    private async void OnPhotoClicked(object sender, EventArgs e)
    {
        var option = await DisplayActionSheet(
            "Origen de la imagen",
            "Cancelar",
            null,
            "Tomar fotografía",
            "Elegir de galería");

        if (option == "Tomar fotografía")
            await OpenPhotoAsync(captureFromCamera: true);
        else if (option == "Elegir de galería")
            await OpenPhotoAsync(captureFromCamera: false);
    }

    private async void OnLiveVideoClicked(object sender, EventArgs e)
    {
        await ShowNextStageMessageAsync("Conteo mediante video en vivo");
    }

    private async void OnHistoryClicked(object? sender, TappedEventArgs e)
    {
        var service = MauiProgram.Services.GetRequiredService<CountSessionService>();
        await Navigation.PushModalAsync(new NavigationPage(new HistoryPage(service))
        {
            BarBackgroundColor = Color.FromArgb("#111F2E"),
            BarTextColor = Colors.White
        });
    }

    private async void OnBackupClicked(object? sender, TappedEventArgs e)
    {
        var service = MauiProgram.Services.GetRequiredService<BackupService>();
        await Navigation.PushModalAsync(new NavigationPage(new BackupPage(service))
        {
            BarBackgroundColor = Color.FromArgb("#111F2E"),
            BarTextColor = Colors.White
        });
    }

    private async void OnTrainingSamplesClicked(object? sender, TappedEventArgs e)
    {
        var service = MauiProgram.Services.GetRequiredService<TrainingSampleService>();
        await Navigation.PushModalAsync(new NavigationPage(new TrainingSamplesPage(service))
        {
            BarBackgroundColor = Color.FromArgb("#111F2E"),
            BarTextColor = Colors.White
        });
    }

    /// <summary>
    /// Solicita el permiso de cámara en tiempo de ejecución antes de abrirla.
    /// Si Android lo tiene denegado, ofrece llevar al usuario a los ajustes.
    /// </summary>
    private async Task<bool> EnsureCameraPermissionAsync()
    {
        var status = await Permissions.CheckStatusAsync<Permissions.Camera>();

        if (status != PermissionStatus.Granted)
            status = await Permissions.RequestAsync<Permissions.Camera>();

        if (status == PermissionStatus.Granted)
            return true;

        var openSettings = await DisplayAlert(
            "Permiso de cámara requerido",
            "KYNOXIO Count necesita permiso para usar la cámara. Puedes habilitarlo en los ajustes de la aplicación y volver a intentarlo.",
            "Abrir ajustes",
            "Cancelar");

        if (openSettings)
            AppInfo.ShowSettingsUI();

        return false;
    }

    /// <summary>
    /// Abre la cámara o galería y muestra la fotografía en vista previa.
    /// La imagen se conserva hasta que el usuario confirma o cancela.
    /// </summary>
    private async Task OpenPhotoAsync(bool captureFromCamera)
    {
        if (_isOpeningPhoto)
            return;

        _isOpeningPhoto = true;

        try
        {
            FileResult? photo;

            if (captureFromCamera)
            {
                if (!MediaPicker.Default.IsCaptureSupported)
                {
                    await DisplayAlert(
                        "Cámara no disponible",
                        "Este dispositivo no permite capturar fotografías desde la aplicación.",
                        "Aceptar");
                    return;
                }

                if (!await EnsureCameraPermissionAsync())
                    return;

                photo = await MediaPicker.Default.CapturePhotoAsync();
            }
            else
            {
                // El selector del sistema administra el acceso a la galería.
                // No solicitamos permiso de cámara para elegir una imagen.
                photo = await MediaPicker.Default.PickPhotoAsync();
            }

            // Cancelar el selector es un flujo normal, no un error.
            if (photo is null)
                return;

            await Navigation.PushModalAsync(
                new NavigationPage(new PhotoPreviewPage(photo))
                {
                    BarBackgroundColor = Color.FromArgb("#111F2E"),
                    BarTextColor = Colors.White
                });
        }
        catch (PermissionException)
        {
            await DisplayAlert(
                "Permiso requerido",
                captureFromCamera
                    ? "Android no concedió el permiso de cámara. Revisa los permisos de KYNOXIO Count en los ajustes del dispositivo."
                    : "No se concedió el acceso necesario para seleccionar la fotografía.",
                "Aceptar");
        }
        catch (FeatureNotSupportedException)
        {
            await DisplayAlert(
                "Función no disponible",
                "El dispositivo no admite esta modalidad de captura.",
                "Aceptar");
        }
        catch (OperationCanceledException)
        {
            // Cancelación normal del usuario.
        }
        catch (Exception ex)
        {
            await DisplayAlert(
                "No se pudo abrir la imagen",
                $"Ocurrió un problema al obtener la fotografía: {ex.Message}",
                "Aceptar");
        }
        finally
        {
            _isOpeningPhoto = false;
        }
    }

    private Task ShowNextStageMessageAsync(string feature)
    {
        return DisplayAlert(
            feature,
            "Esta función se implementará en la siguiente etapa.",
            "Entendido");
    }
}
