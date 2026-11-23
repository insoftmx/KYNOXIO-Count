using KYNOXIO.Count.App.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Maui.Storage;

namespace KYNOXIO.Count.App;

/// <summary>
/// Vista previa de la fotografía antes de crear una sesión local de conteo.
/// </summary>
public sealed class PhotoPreviewPage : ContentPage
{
    private readonly FileResult _photo;
    private readonly Button _saveButton;
    private bool _isSaving;

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
            Text = "Confirma que los materiales o componentes se vean con claridad. La imagen se guardará en el teléfono y se registrará en tu historial.",
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
            StrokeShape = new Microsoft.Maui.Controls.Shapes.RoundRectangle { CornerRadius = 16 },
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

        _saveButton = new Button
        {
            Text = "GUARDAR Y CREAR CONTEO",
            BackgroundColor = Color.FromArgb("#087CA7"),
            TextColor = Colors.White,
            FontAttributes = FontAttributes.Bold,
            CornerRadius = 12,
            HeightRequest = 50
        };
        _saveButton.Clicked += OnSaveClicked;

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
        Grid.SetRow(_saveButton, 4);
        Grid.SetRow(cancelButton, 5);

        layout.Children.Add(heading);
        layout.Children.Add(description);
        layout.Children.Add(imageFrame);
        layout.Children.Add(fileName);
        layout.Children.Add(_saveButton);
        layout.Children.Add(cancelButton);

        Content = layout;
    }

    private async void OnSaveClicked(object? sender, EventArgs e)
    {
        if (_isSaving)
            return;

        _isSaving = true;
        _saveButton.IsEnabled = false;
        _saveButton.Text = "GUARDANDO...";

        try
        {
            var service = MauiProgram.Services.GetRequiredService<CountSessionService>();
            var session = await service.CreateFromPhotoAsync(_photo);

            await DisplayAlert(
                "Conteo guardado",
                $"Se creó la sesión {session.Folio}. La fotografía quedó almacenada en este dispositivo. El análisis automático está pendiente.",
                "Continuar");

            await Navigation.PopModalAsync();
        }
        catch (Exception ex)
        {
            await DisplayAlert(
                "No se pudo guardar",
                $"La fotografía no pudo registrarse. No se creó una sesión incompleta. Detalle: {ex.Message}",
                "Aceptar");
        }
        finally
        {
            _isSaving = false;
            _saveButton.IsEnabled = true;
            _saveButton.Text = "GUARDAR Y CREAR CONTEO";
        }
    }

    private async void OnCancelClicked(object? sender, EventArgs e)
    {
        if (_isSaving)
            return;

        await Navigation.PopModalAsync();
    }
}