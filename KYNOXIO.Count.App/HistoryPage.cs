using KYNOXIO.Count.App.Services;
using KYNOXIO.Count.Core.Models;

namespace KYNOXIO.Count.App;

/// <summary>
/// Historial de sesiones guardadas en la base SQLite del dispositivo.
/// </summary>
public sealed class HistoryPage : ContentPage
{
    private readonly CountSessionService _sessionService;
    private readonly VerticalStackLayout _itemsLayout;
    private readonly Label _summaryLabel;

    public HistoryPage(CountSessionService sessionService)
    {
        _sessionService = sessionService;

        Title = "Historial";
        BackgroundColor = Color.FromArgb("#0B1420");

        ToolbarItems.Add(new ToolbarItem
        {
            Text = "Cerrar",
            Command = new Command(async () => await Navigation.PopModalAsync())
        });

        var heading = new Label
        {
            Text = "HISTORIAL DE CONTEOS",
            FontSize = 12,
            FontAttributes = FontAttributes.Bold,
            CharacterSpacing = 1.5,
            TextColor = Color.FromArgb("#22D3EE")
        };

        _summaryLabel = new Label
        {
            Text = "Sesiones guardadas en este dispositivo",
            FontSize = 13,
            TextColor = Color.FromArgb("#A6B7C5")
        };

        _itemsLayout = new VerticalStackLayout { Spacing = 12 };

        var scroll = new ScrollView
        {
            Content = _itemsLayout
        };

        var layout = new Grid
        {
            Padding = new Thickness(18, 18, 18, 22),
            RowDefinitions =
            {
                new RowDefinition { Height = GridLength.Auto },
                new RowDefinition { Height = GridLength.Auto },
                new RowDefinition { Height = new GridLength(1, GridUnitType.Star) }
            },
            RowSpacing = 12
        };

        Grid.SetRow(_summaryLabel, 1);
        Grid.SetRow(scroll, 2);
        layout.Children.Add(heading);
        layout.Children.Add(_summaryLabel);
        layout.Children.Add(scroll);
        Content = layout;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await LoadHistoryAsync();
    }

    private async Task LoadHistoryAsync()
    {
        _itemsLayout.Children.Clear();

        try
        {
            var sessions = await _sessionService.GetHistoryAsync();
            _summaryLabel.Text = $"{sessions.Count} sesión(es) guardada(s) en este dispositivo";

            if (sessions.Count == 0)
            {
                _itemsLayout.Children.Add(new Border
                {
                    Padding = 20,
                    BackgroundColor = Color.FromArgb("#111F2E"),
                    Stroke = Color.FromArgb("#263B4D"),
                    StrokeThickness = 1,
                    StrokeShape = new Microsoft.Maui.Controls.Shapes.RoundRectangle { CornerRadius = 16 },
                    Content = new Label
                    {
                        Text = "Todavía no hay conteos guardados. Toma una fotografía y confirma “Guardar y crear conteo” para iniciar tu historial.",
                        TextColor = Color.FromArgb("#A6B7C5"),
                        FontSize = 13
                    }
                });
                return;
            }

            foreach (var session in sessions)
                _itemsLayout.Children.Add(CreateSessionCard(session));
        }
        catch (Exception ex)
        {
            _summaryLabel.Text = "No se pudo consultar el historial";
            _itemsLayout.Children.Add(new Label
            {
                Text = ex.Message,
                TextColor = Color.FromArgb("#FCA5A5"),
                FontSize = 12
            });
        }
    }

    private View CreateSessionCard(CountSession session)
    {
        var imagePath = _sessionService.GetImageAbsolutePath(session.ImageRelativePath);
        var thumbnail = new Image
        {
            Source = File.Exists(imagePath) ? ImageSource.FromFile(imagePath) : null,
            Aspect = Aspect.AspectFill,
            HeightRequest = 82,
            WidthRequest = 82
        };

        var imageFrame = new Border
        {
            HeightRequest = 82,
            WidthRequest = 82,
            StrokeThickness = 0,
            StrokeShape = new Microsoft.Maui.Controls.Shapes.RoundRectangle { CornerRadius = 12 },
            Content = thumbnail,
            BackgroundColor = Color.FromArgb("#183B50")
        };

        var details = new VerticalStackLayout
        {
            Spacing = 4,
            VerticalOptions = LayoutOptions.Center,
            Children =
            {
                new Label
                {
                    Text = session.Folio,
                    FontSize = 13,
                    FontAttributes = FontAttributes.Bold,
                    TextColor = Color.FromArgb("#F2F7FA")
                },
                new Label
                {
                    Text = session.CreatedAtUtc.ToLocalTime().ToString("dd/MM/yyyy  HH:mm"),
                    FontSize = 11,
                    TextColor = Color.FromArgb("#A6B7C5")
                },
                new Label
                {
                    Text = session.DetectedCount.HasValue
                        ? $"Piezas detectadas: {session.DetectedCount.Value}"
                        : "Pendiente de análisis automático",
                    FontSize = 11,
                    TextColor = Color.FromArgb("#22D3EE")
                }
            }
        };

        var row = new Grid
        {
            ColumnDefinitions =
            {
                new ColumnDefinition { Width = 82 },
                new ColumnDefinition { Width = GridLength.Star }
            },
            ColumnSpacing = 12
        };
        Grid.SetColumn(details, 1);
        row.Children.Add(imageFrame);
        row.Children.Add(details);

        var card = new Border
        {
            Padding = 12,
            BackgroundColor = Color.FromArgb("#111F2E"),
            Stroke = Color.FromArgb("#263B4D"),
            StrokeThickness = 1,
            StrokeShape = new Microsoft.Maui.Controls.Shapes.RoundRectangle { CornerRadius = 16 },
            Content = row
        };

        var tap = new TapGestureRecognizer();
        tap.Tapped += async (_, _) =>
        {
            await Navigation.PushAsync(new SessionDetailPage(_sessionService, session));
        };
        card.GestureRecognizers.Add(tap);

        return card;
    }
}