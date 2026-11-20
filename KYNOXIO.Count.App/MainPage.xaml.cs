using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Media;
using Microsoft.Maui.Storage;

namespace KYNOXIO.Count.App;

/// <summary>
/// Pantalla principal de KYNOXIO Count.
/// La captura y seleccion de imagen usan MediaPicker de .NET MAUI.
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

    /// <summary>
    /// Abre la camara o la galeria y muestra la imagen elegida en vista previa.
    /// Evita abrir mas de un selector simultaneamente.
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

                photo = await MediaPicker.Default.CapturePhotoAsync();
            }
            else
            {
                photo = await MediaPicker.Default.PickPhotoAsync();
            }

            // El usuario puede cancelar el selector sin que sea un error.
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
                "No se concedió el permiso necesario para acceder a la cámara o a las fotografías.",
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