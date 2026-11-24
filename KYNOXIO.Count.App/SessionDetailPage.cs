using KYNOXIO.Count.App.Services;
using KYNOXIO.Count.Core.Models;
using System.Globalization;

namespace KYNOXIO.Count.App;

/// <summary>
/// Detalle de sesión con captura de resultado manual y evidencia fotográfica.
/// </summary>
public sealed class SessionDetailPage : ContentPage
{
    private readonly CountSessionService _sessionService;
    private readonly CountSession _session;
    private readonly Entry _itemEntry;
    private readonly Entry _quantityEntry;
    private readonly Editor _notesEditor;
    private readonly Label _resultLabel;
    private readonly Label _statusLabel;
    private readonly Button _saveButton;
    private bool _isSaving;

    public SessionDetailPage(CountSessionService sessionService, CountSession session)
    {
        _sessionService = sessionService ?? throw new ArgumentNullException(nameof(sessionService));
        _session = session ?? throw new ArgumentNullException(nameof(session));

        Title = "Detalle del conteo";
        BackgroundColor = Color.FromArgb("#0B1420");

        var imagePath = _sessionService.GetImageAbsolutePath(_session.ImageRelativePath);
        var image = new Image
        {
            Source = File.Exists(imagePath) ? ImageSource.FromFile(imagePath) : null,
            Aspect = Aspect.AspectFit,
            HeightRequest = 280
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

        _resultLabel = new Label
        {
            FontSize = 14,
            TextColor = Color.FromArgb("#22D3EE")
        };
        _statusLabel = new Label
        {
            FontSize = 12,
            TextColor = Color.FromArgb("#A6B7C5")
        };

        _itemEntry = new Entry
        {
            Placeholder = "Ej. Polín de acero, rondana, panel PIR",
            Text = _session.ItemDescription,
            MaxLength = 160,
            TextColor = Colors.White,
            PlaceholderColor = Color.FromArgb("#718596"),
            BackgroundColor = Color.FromArgb("#111F2E")
        };

        _quantityEntry = new Entry
        {
            Placeholder = "Cantidad total",
            Text = _session.ManualCount?.ToString(CultureInfo.InvariantCulture) ?? string.Empty,
            Keyboard = Keyboard.Numeric,
            MaxLength = 9,
            TextColor = Colors.White,
            PlaceholderColor = Color.FromArgb("#718596"),
            BackgroundColor = Color.FromArgb("#111F2E")
        };

        _notesEditor = new Editor
        {
            Placeholder = "Observaciones, unidad de medida, condiciones del material...",
            Text = _session.Notes,
            AutoSize = EditorAutoSizeOption.TextChanges,
            HeightRequest = 90,
            MaxLength = 1000,
            TextColor = Colors.White,
            PlaceholderColor = Color.FromArgb("#718596"),
            BackgroundColor = Color.FromArgb("#111F2E")
        };

        _saveButton = new Button
        {
            Text = "GUARDAR RESULTADO MANUAL",
            BackgroundColor = Color.FromArgb("#087CA7"),
            TextColor = Colors.White,
            FontAttributes = FontAttributes.Bold,
            CornerRadius = 12,
            HeightRequest = 50
        };
        _saveButton.Clicked += OnSaveManualCountClicked;

        var content = new VerticalStackLayout
        {
            Padding = new Thickness(18, 18, 18, 28),
            Spacing = 12,
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
                    Text = _session.Folio,
                    FontSize = 19,
                    FontAttributes = FontAttributes.Bold,
                    TextColor = Color.FromArgb("#F2F7FA")
                },
                new Label
                {
                    Text = _session.CreatedAtUtc.ToLocalTime().ToString("dd/MM/yyyy  HH:mm:ss"),
                    FontSize = 12,
                    TextColor = Color.FromArgb("#A6B7C5")
                },
                imageFrame,
                new Label
                {
                    Text = "RESULTADO REGISTRADO",
                    FontSize = 10,
                    FontAttributes = FontAttributes.Bold,
                    CharacterSpacing = 1.3,
                    TextColor = Color.FromArgb("#718596")
                },
                _resultLabel,
                _statusLabel,
                new Label
                {
                    Text = "DESCRIPCIÓN DEL MATERIAL O COMPONENTE",
                    FontSize = 10,
                    FontAttributes = FontAttributes.Bold,
                    CharacterSpacing = 1,
                    TextColor = Color.FromArgb("#A6B7C5")
                },
                _itemEntry,
                new Label
                {
                    Text = "CANTIDAD CONTADA",
                    FontSize = 10,
                    FontAttributes = FontAttributes.Bold,
                    CharacterSpacing = 1,
                    TextColor = Color.FromArgb("#A6B7C5")
                },
                _quantityEntry,
                new Label
                {
                    Text = "OBSERVACIONES (OPCIONAL)",
                    FontSize = 10,
                    FontAttributes = FontAttributes.Bold,
                    CharacterSpacing = 1,
                    TextColor = Color.FromArgb("#A6B7C5")
                },
                _notesEditor,
                _saveButton,
                new Label
                {
                    Text = "Este resultado es capturado por el usuario. No representa un análisis automático de IA.",
                    FontSize = 11,
                    TextColor = Color.FromArgb("#718596")
                }
            }
        };

        RefreshResultLabels();
        Content = new ScrollView { Content = content };
    }

    private void RefreshResultLabels()
    {
        _resultLabel.Text = _session.ManualCount.HasValue
            ? $"{_session.ItemDescription}: {_session.ManualCount.Value:N0} piezas (conteo manual)"
            : "Sin cantidad registrada. Captura y guarda el conteo realizado.";
        _statusLabel.Text = $"Estado: {_session.Status}";
    }

    private async void OnSaveManualCountClicked(object? sender, EventArgs e)
    {
        if (_isSaving)
            return;

        if (!int.TryParse(_quantityEntry.Text, NumberStyles.None, CultureInfo.InvariantCulture, out var quantity))
        {
            await DisplayAlert("Cantidad no válida", "Escribe una cantidad entera igual o mayor que cero.", "Aceptar");
            return;
        }

        if (string.IsNullOrWhiteSpace(_itemEntry.Text))
        {
            await DisplayAlert("Falta la descripción", "Indica qué material o componente estás contando.", "Aceptar");
            return;
        }

        _isSaving = true;
        _saveButton.IsEnabled = false;
        _saveButton.Text = "GUARDANDO...";

        try
        {
            var updated = await _sessionService.SaveManualCountAsync(
                _session.Id,
                _itemEntry.Text,
                quantity,
                _notesEditor.Text);

            _session.ItemDescription = updated.ItemDescription;
            _session.ManualCount = updated.ManualCount;
            _session.Notes = updated.Notes;
            _session.CountMethod = updated.CountMethod;
            _session.Status = updated.Status;
            RefreshResultLabels();

            await DisplayAlert("Conteo actualizado", "El resultado manual quedó guardado en el historial local.", "Continuar");
        }
        catch (Exception ex)
        {
            await DisplayAlert("No se pudo guardar", ex.Message, "Aceptar");
        }
        finally
        {
            _isSaving = false;
            _saveButton.IsEnabled = true;
            _saveButton.Text = "GUARDAR RESULTADO MANUAL";
        }
    }
}