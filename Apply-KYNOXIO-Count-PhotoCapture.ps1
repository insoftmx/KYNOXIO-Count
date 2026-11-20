# KYNOXIO Count - Captura fotografica y seleccion desde galeria
# Ejecutar desde la raiz del repositorio KYNOXIO-Count.
$ErrorActionPreference = "Stop"

$root = (Get-Location).Path
$required = @(
    "KYNOXIO.Count.App\KYNOXIO.Count.App.csproj",
    "KYNOXIO.Count.App\MainPage.xaml",
    "KYNOXIO.Count.App\Platforms\Android\AndroidManifest.xml"
)
foreach ($file in $required) {
    if (-not (Test-Path (Join-Path $root $file))) {
        throw "No se encontro '$file'. Ejecuta este script desde la raiz de KYNOXIO-Count."
    }
}

function Write-Utf8File([string]$RelativePath, [string]$Content) {
    $fullPath = Join-Path $root $RelativePath
    $directory = Split-Path $fullPath -Parent
    if (-not (Test-Path $directory)) {
        New-Item -ItemType Directory -Path $directory -Force | Out-Null
    }
    [System.IO.File]::WriteAllText(
        $fullPath,
        $Content.TrimStart("`r", "`n"),
        [System.Text.UTF8Encoding]::new($false)
    )
}

$mainPage = Join-Path $root "KYNOXIO.Count.App\MainPage.xaml"
$mainXaml = [System.IO.File]::ReadAllText($mainPage)
if ($mainXaml -notmatch 'Clicked="OnPhotoClicked"') {
    throw "No se reconoce el dashboard esperado. No se modifico MainPage.xaml.cs."
}

Write-Utf8File "KYNOXIO.Count.App\MainPage.xaml.cs" @'
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
'@

Write-Utf8File "KYNOXIO.Count.App\PhotoPreviewPage.cs" @'
using Microsoft.Maui.Storage;

namespace KYNOXIO.Count.App;

/// <summary>
/// Vista previa de la fotografia antes de enviarla al futuro motor de conteo.
/// Conserva el FileResult mientras esta pantalla permanece abierta.
/// </summary>
public sealed class PhotoPreviewPage : ContentPage
{
    private readonly FileResult _photo;

    public PhotoPreviewPage(FileResult photo)
    {
        _photo = photo ?? throw new ArgumentNullException(nameof(photo));

        Title = "Vista previa";
        BackgroundColor = Color.FromArgb("#0B1420");

        var heading = new Label
        {
            Text = "REVISA TU IMAGEN",
            FontSize = 12,
            FontAttributes = FontAttributes.Bold,
            CharacterSpacing = 1.5,
            TextColor = Color.FromArgb("#22D3EE")
        };

        var description = new Label
        {
            Text = "Confirma que los materiales o componentes se vean con claridad antes de continuar.",
            FontSize = 13,
            TextColor = Color.FromArgb("#A6B7C5")
        };

        var image = new Image
        {
            Source = ImageSource.FromFile(_photo.FullPath),
            Aspect = Aspect.AspectFit,
            HorizontalOptions = LayoutOptions.Fill,
            VerticalOptions = LayoutOptions.Fill
        };

        var imageFrame = new Border
        {
            BackgroundColor = Color.FromArgb("#111F2E"),
            Stroke = Color.FromArgb("#263B4D"),
            StrokeThickness = 1,
            StrokeShape = new Microsoft.Maui.Controls.Shapes.RoundRectangle
            {
                CornerRadius = 16
            },
            Padding = 8,
            Content = image
        };

        var fileName = new Label
        {
            Text = _photo.FileName,
            FontSize = 11,
            TextColor = Color.FromArgb("#718596"),
            LineBreakMode = LineBreakMode.MiddleTruncation
        };

        var useButton = new Button
        {
            Text = "USAR ESTA IMAGEN",
            BackgroundColor = Color.FromArgb("#087CA7"),
            TextColor = Colors.White,
            FontAttributes = FontAttributes.Bold,
            CornerRadius = 12,
            HeightRequest = 50
        };
        useButton.Clicked += OnUsePhotoClicked;

        var cancelButton = new Button
        {
            Text = "VOLVER",
            BackgroundColor = Color.FromArgb("#1A3448"),
            TextColor = Color.FromArgb("#22D3EE"),
            CornerRadius = 12,
            HeightRequest = 46
        };
        cancelButton.Clicked += OnCancelClicked;

        var layout = new Grid
        {
            Padding = new Thickness(18, 18, 18, 22),
            RowDefinitions =
            {
                new RowDefinition { Height = GridLength.Auto },
                new RowDefinition { Height = GridLength.Auto },
                new RowDefinition { Height = new GridLength(1, GridUnitType.Star) },
                new RowDefinition { Height = GridLength.Auto },
                new RowDefinition { Height = GridLength.Auto },
                new RowDefinition { Height = GridLength.Auto }
            },
            RowSpacing = 12
        };

        Grid.SetRow(description, 1);
        Grid.SetRow(imageFrame, 2);
        Grid.SetRow(fileName, 3);
        Grid.SetRow(useButton, 4);
        Grid.SetRow(cancelButton, 5);

        layout.Children.Add(heading);
        layout.Children.Add(description);
        layout.Children.Add(imageFrame);
        layout.Children.Add(fileName);
        layout.Children.Add(useButton);
        layout.Children.Add(cancelButton);

        Content = layout;
    }

    private async void OnUsePhotoClicked(object? sender, EventArgs e)
    {
        await DisplayAlert(
            "Imagen confirmada",
            "La fotografía quedó seleccionada. En la siguiente etapa conectaremos esta imagen con el motor de análisis y conteo.",
            "Continuar");

        await Navigation.PopModalAsync();
    }

    private async void OnCancelClicked(object? sender, EventArgs e)
    {
        await Navigation.PopModalAsync();
    }
}
'@

# Agrega el permiso de camara sin duplicarlo. La galeria usa el selector del sistema.
$manifestPath = Join-Path $root "KYNOXIO.Count.App\Platforms\Android\AndroidManifest.xml"
$manifest = [System.IO.File]::ReadAllText($manifestPath)
if ($manifest -notmatch 'android\.permission\.CAMERA') {
    $manifest = $manifest -replace '(<manifest\b[^>]*>)', "`$1`r`n    <uses-permission android:name=`"android.permission.CAMERA`" />"
    [System.IO.File]::WriteAllText($manifestPath, $manifest, [System.Text.UTF8Encoding]::new($false))
}

Write-Host ""
Write-Host "Captura fotografica y seleccion de galeria implementadas." -ForegroundColor Green
Write-Host "Se agrego vista previa y permiso CAMERA para Android."
Write-Host "Siguiente paso: compilar para Android y probar en el dispositivo."
