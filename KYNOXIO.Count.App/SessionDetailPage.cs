using KYNOXIO.Count.App.Services;
using KYNOXIO.Count.Core.Models;

namespace KYNOXIO.Count.App;

/// <summary>
/// Detalle de una sesión. No presenta una cantidad estimada cuando todavía no hay IA.
/// </summary>
public sealed class SessionDetailPage : ContentPage
{
    public SessionDetailPage(CountSessionService sessionService, CountSession session)
    {
        Title = "Detalle del conteo";
        BackgroundColor = Color.FromArgb("#0B1420");

        var imagePath = sessionService.GetImageAbsolutePath(session.ImageRelativePath);
        var image = new Image
        {
            Source = File.Exists(imagePath) ? ImageSource.FromFile(imagePath) : null,
            Aspect = Aspect.AspectFit,
            HeightRequest = 300
        };

        var imageFrame = new Border
        {
            Padding = 8,
            BackgroundColor = Color.FromArgb("#111F2E"),
            Stroke = Color.FromArgb("#263B4D"),
            StrokeThickness = 1,
            StrokeShape = new Microsoft.Maui.Controls.Shapes.RoundRectangle { CornerRadius = 16 },
            Content = image
        };

        var statusText = session.DetectedCount.HasValue
            ? $"Piezas detectadas: {session.DetectedCount.Value}"
            : "Aún no procesada. No se muestra una cantidad estimada.";

        var content = new VerticalStackLayout
        {
            Padding = new Thickness(18, 18, 18, 24),
            Spacing = 14,
            Children =
            {
                new Label
                {
                    Text = "SESIÓN DE CONTEO",
                    FontSize = 11,
                    FontAttributes = FontAttributes.Bold,
                    CharacterSpacing = 1.5,
                    TextColor = Color.FromArgb("#22D3EE")
                },
                new Label
                {
                    Text = session.Folio,
                    FontSize = 19,
                    FontAttributes = FontAttributes.Bold,
                    TextColor = Color.FromArgb("#F2F7FA")
                },
                new Label
                {
                    Text = session.CreatedAtUtc.ToLocalTime().ToString("dd/MM/yyyy  HH:mm:ss"),
                    FontSize = 12,
                    TextColor = Color.FromArgb("#A6B7C5")
                },
                imageFrame,
                new Label
                {
                    Text = "RESULTADO",
                    FontSize = 10,
                    FontAttributes = FontAttributes.Bold,
                    CharacterSpacing = 1.3,
                    TextColor = Color.FromArgb("#718596")
                },
                new Label
                {
                    Text = statusText,
                    FontSize = 14,
                    TextColor = Color.FromArgb("#22D3EE")
                },
                new Label
                {
                    Text = $"Estado: {session.Status}",
                    FontSize = 12,
                    TextColor = Color.FromArgb("#A6B7C5")
                }
            }
        };

        Content = new ScrollView { Content = content };
    }
}