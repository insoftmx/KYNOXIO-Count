using KYNOXIO.Count.App.Services;
using KYNOXIO.Count.Core.Models;
using Microsoft.Maui.Media;

namespace KYNOXIO.Count.App;

/// <summary>Captura y clasifica fotografías destinadas exclusivamente al banco de entrenamiento.</summary>
public sealed class TrainingSamplesPage : ContentPage
{
    private readonly TrainingSampleService _service;
    private readonly Picker _groupPicker = new() { Title = "Selecciona el grupo" };
    private readonly Entry _classEntry = new() { Placeholder = "Ej. Rondana galvanizada", MaxLength = 160 };
    private readonly Entry _countEntry = new() { Placeholder = "Cantidad física comprobada (opcional)", Keyboard = Keyboard.Numeric };
    private readonly Picker _lightingPicker = new() { Title = "Iluminación" };
    private readonly Picker _distancePicker = new() { Title = "Distancia de cámara" };
    private readonly Picker _anglePicker = new() { Title = "Ángulo" };
    private readonly Picker _occlusionPicker = new() { Title = "Oclusión" };
    private readonly Editor _notesEditor = new() { Placeholder = "Observaciones de la muestra", AutoSize = EditorAutoSizeOption.TextChanges, HeightRequest = 80 };
    private readonly Image _preview = new() { Aspect = Aspect.AspectFit, HeightRequest = 190, IsVisible = false };
    private readonly Label _photoLabel = new() { Text = "No has seleccionado una fotografía.", TextColor = Color.FromArgb("#A6B7C5"), FontSize = 12 };
    private readonly Button _saveButton = new() { Text = "GUARDAR MUESTRA COMO BORRADOR", BackgroundColor = Color.FromArgb("#087CA7"), TextColor = Colors.White, FontAttributes = FontAttributes.Bold, CornerRadius = 12, HeightRequest = 50 };
    private readonly Button _exportButton = new() { Text = "EXPORTAR BANCO DE IMÁGENES", BackgroundColor = Color.FromArgb("#183B50"), TextColor = Color.FromArgb("#22D3EE"), FontAttributes = FontAttributes.Bold, CornerRadius = 12, HeightRequest = 48 };
    private readonly VerticalStackLayout _samplesList = new() { Spacing = 10 };
    private FileResult? _selectedPhoto;
    private bool _isSaving;

    public TrainingSamplesPage(TrainingSampleService service)
    {
        _service = service ?? throw new ArgumentNullException(nameof(service));
        Title = "Banco de entrenamiento";
        BackgroundColor = Color.FromArgb("#0B1420");

        _groupPicker.Items.Add("Piezas individuales agrupadas");
        _groupPicker.Items.Add("Material apilado");
        _groupPicker.SelectedIndex = 0;

        AddOptions(_lightingPicker, "Buena", "Baja", "Irregular", "Contraluz");
        AddOptions(_distancePicker, "Cercana", "Media", "Lejana");
        AddOptions(_anglePicker, "Frontal", "Superior", "Lateral", "Diagonal");
        AddOptions(_occlusionPicker, "Sin oclusión visible", "Oclusión parcial", "Oclusión alta");

        var cameraButton = MakeButton("TOMAR FOTOGRAFÍA", "#183B50", "#22D3EE");
        cameraButton.Clicked += async (_, _) => await SelectPhotoAsync(true);
        var galleryButton = MakeButton("ELEGIR DE GALERÍA", "#183B50", "#22D3EE");
        galleryButton.Clicked += async (_, _) => await SelectPhotoAsync(false);
        _saveButton.Clicked += OnSaveClicked;
        _exportButton.Clicked += OnExportClicked;

        ToolbarItems.Add(new ToolbarItem
        {
            Text = "Cerrar",
            Command = new Command(async () => await Navigation.PopModalAsync())
        });

        Content = new ScrollView
        {
            Content = new VerticalStackLayout
            {
                Padding = 18,
                Spacing = 12,
                Children =
                {
                    Heading("BANCO DE IMÁGENES"),
                    Body("Recopila fotografías reales para entrenar KYNOXIO Count. Estas muestras se guardan separadas del historial de conteos."),
                    new Label { Text = "FOTOGRAFÍA", TextColor = Color.FromArgb("#22D3EE"), FontAttributes = FontAttributes.Bold, FontSize = 12 },
                    new HorizontalStackLayout { Spacing = 8, Children = { cameraButton, galleryButton } },
                    _preview,
                    _photoLabel,
                    new BoxView { HeightRequest = 1, Color = Color.FromArgb("#263B4D") },
                    FieldLabel("GRUPO DE MATERIAL"), _groupPicker,
                    FieldLabel("CLASE / MATERIAL"), _classEntry,
                    FieldLabel("CANTIDAD FÍSICA COMPROBADA"), _countEntry,
                    Body("La cantidad es opcional para guardar un borrador. Debe verificarse físicamente; nunca se debe copiar una estimación de IA."),
                    FieldLabel("ILUMINACIÓN"), _lightingPicker,
                    FieldLabel("DISTANCIA"), _distancePicker,
                    FieldLabel("ÁNGULO"), _anglePicker,
                    FieldLabel("OCLUSIÓN"), _occlusionPicker,
                    FieldLabel("OBSERVACIONES"), _notesEditor,
                    _saveButton,
                    Body("Estado inicial: Draft. La muestra no se marcará como lista para entrenamiento hasta completar las anotaciones geométricas de piezas o capas."),
                    new BoxView { HeightRequest = 1, Color = Color.FromArgb("#263B4D") },
                    Heading("MUESTRAS GUARDADAS"),
                    _samplesList
                }
            }
        };

        Loaded += async (_, _) => await RefreshSamplesAsync();
    }

    private async Task SelectPhotoAsync(bool camera)
    {
        try
        {
            _selectedPhoto = camera
                ? await MediaPicker.Default.CapturePhotoAsync()
                : await MediaPicker.Default.PickPhotoAsync();

            if (_selectedPhoto is null)
                return;

            _preview.Source = ImageSource.FromFile(_selectedPhoto.FullPath);
            _preview.IsVisible = true;
            _photoLabel.Text = _selectedPhoto.FileName;
        }
        catch (Exception ex)
        {
            await DisplayAlert("No se pudo obtener la fotografía", ex.Message, "Aceptar");
        }
    }

    private async void OnSaveClicked(object? sender, EventArgs e)
    {
        if (_isSaving)
            return;

        if (_selectedPhoto is null)
        {
            await DisplayAlert("Falta la fotografía", "Toma una fotografía o selecciona una imagen de la galería.", "Aceptar");
            return;
        }

        if (string.IsNullOrWhiteSpace(_classEntry.Text))
        {
            await DisplayAlert("Falta la clase", "Escribe el nombre del material o componente.", "Aceptar");
            return;
        }

        int? verifiedCount = null;
        if (!string.IsNullOrWhiteSpace(_countEntry.Text))
        {
            if (!int.TryParse(_countEntry.Text, out var parsedCount) || parsedCount < 0)
            {
                await DisplayAlert("Cantidad no válida", "Escribe un número entero igual o mayor que cero, o deja el campo vacío.", "Aceptar");
                return;
            }
            verifiedCount = parsedCount;
        }

        try
        {
            _isSaving = true;
            _saveButton.IsEnabled = false;

            var sample = await _service.CreateDraftAsync(
                _selectedPhoto,
                _groupPicker.SelectedIndex == 0 ? "IndividualParts" : "StackedMaterials",
                _classEntry.Text,
                verifiedCount,
                SelectedText(_lightingPicker),
                SelectedText(_distancePicker),
                SelectedText(_anglePicker),
                SelectedText(_occlusionPicker),
                _notesEditor.Text ?? string.Empty);

            await DisplayAlert("Muestra guardada", $"Se guardó como borrador.\nID: {sample.Id[..8].ToUpperInvariant()}", "Aceptar");
            _selectedPhoto = null;
            _preview.Source = null;
            _preview.IsVisible = false;
            _photoLabel.Text = "No has seleccionado una fotografía.";
            _classEntry.Text = string.Empty;
            _countEntry.Text = string.Empty;
            _notesEditor.Text = string.Empty;
            await RefreshSamplesAsync();
        }
        catch (Exception ex)
        {
            await DisplayAlert("No se pudo guardar la muestra", ex.Message, "Aceptar");
        }
        finally
        {
            _isSaving = false;
            _saveButton.IsEnabled = true;
        }
    }

    private async void OnExportClicked(object? sender, EventArgs e)
    {
        if (!_exportButton.IsEnabled)
            return;

        try
        {
            _exportButton.IsEnabled = false;
            var path = await _service.ExportAndShareDatasetAsync();
            await DisplayAlert(
                "Banco exportado",
                $"Se preparó el paquete ZIP con las muestras y fotografías disponibles.\n\n{Path.GetFileName(path)}",
                "Aceptar");
        }
        catch (Exception ex)
        {
            await DisplayAlert("No se pudo exportar el banco", ex.Message, "Aceptar");
        }
        finally
        {
            _exportButton.IsEnabled = true;
        }
    }

    private async Task RefreshSamplesAsync()
    {
        _samplesList.Children.Clear();
        var samples = await _service.GetAllAsync();

        if (samples.Count == 0)
        {
            _samplesList.Children.Add(Body("Aún no hay muestras. Agrega la primera fotografía."));
            return;
        }

        foreach (var sample in samples)
        {
            var group = sample.MaterialGroup == "IndividualParts" ? "Piezas individuales" : "Material apilado";
            var count = sample.VerifiedCount.HasValue ? $"Cantidad verificada: {sample.VerifiedCount.Value}" : "Cantidad pendiente de verificar";
            _samplesList.Children.Add(new Border
            {
                Padding = 12,
                BackgroundColor = Color.FromArgb("#111F2E"),
                Stroke = Color.FromArgb("#263B4D"),
                StrokeThickness = 1,
                StrokeShape = new Microsoft.Maui.Controls.Shapes.RoundRectangle { CornerRadius = 12 },
                Content = new VerticalStackLayout
                {
                    Spacing = 3,
                    Children =
                    {
                        new Label { Text = sample.ClassName, TextColor = Colors.White, FontAttributes = FontAttributes.Bold },
                        new Label { Text = group + " · " + count, TextColor = Color.FromArgb("#A6B7C5"), FontSize = 12 },
                        new Label { Text = "BORRADOR · " + sample.CreatedAtUtc.ToLocalTime().ToString("dd/MM/yyyy HH:mm"), TextColor = Color.FromArgb("#22D3EE"), FontSize = 10 }
                    }
                }
            });
        }
    }

    private static void AddOptions(Picker picker, params string[] options)
    {
        foreach (var option in options)
            picker.Items.Add(option);
        picker.SelectedIndex = 0;
    }

    private static string SelectedText(Picker picker) =>
        picker.SelectedItem?.ToString() ?? string.Empty;

    private static Button MakeButton(string text, string background, string foreground) => new()
    {
        Text = text,
        BackgroundColor = Color.FromArgb(background),
        TextColor = Color.FromArgb(foreground),
        CornerRadius = 10,
        FontSize = 11,
        HorizontalOptions = LayoutOptions.FillAndExpand
    };

    private static Label Heading(string text) => new()
    {
        Text = text,
        TextColor = Color.FromArgb("#22D3EE"),
        FontAttributes = FontAttributes.Bold,
        FontSize = 12,
        CharacterSpacing = 1.3
    };

    private static Label Body(string text) => new()
    {
        Text = text,
        TextColor = Color.FromArgb("#A6B7C5"),
        FontSize = 12
    };

    private static Label FieldLabel(string text) => new()
    {
        Text = text,
        TextColor = Color.FromArgb("#718596"),
        FontAttributes = FontAttributes.Bold,
        FontSize = 10,
        CharacterSpacing = 1
    };
}