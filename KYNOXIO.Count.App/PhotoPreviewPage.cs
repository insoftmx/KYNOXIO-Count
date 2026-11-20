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