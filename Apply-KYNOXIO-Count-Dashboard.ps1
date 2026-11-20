# KYNOXIO Count - Dashboard inicial
# Ejecutar desde la raiz del repositorio KYNOXIO-Count.
# Este script sustituye la pantalla de plantilla por un dashboard MAUI.
$ErrorActionPreference = "Stop"

$root = (Get-Location).Path
$required = @(
    "KYNOXIO.Count.App\KYNOXIO.Count.App.csproj",
    "KYNOXIO.Count.App\MainPage.xaml",
    "KYNOXIO.Count.App\App.xaml"
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
    [System.IO.File]::WriteAllText($fullPath, $Content.TrimStart("`r", "`n"), [System.Text.UTF8Encoding]::new($false))
}

Write-Utf8File "KYNOXIO.Count.App\MainPage.xaml" @'
<?xml version="1.0" encoding="utf-8" ?>
<ContentPage
    x:Class="KYNOXIO.Count.App.MainPage"
    xmlns="http://schemas.microsoft.com/dotnet/2021/maui"
    xmlns:x="http://schemas.microsoft.com/winfx/2009/xaml"
    BackgroundColor="{StaticResource AppBackground}"
    Shell.NavBarIsVisible="False">

    <Grid RowDefinitions="*,Auto">
        <ScrollView Grid.Row="0">
            <VerticalStackLayout Padding="22,22,22,28" Spacing="24">

                <!-- Encabezado e identidad de la aplicacion -->
                <Grid ColumnDefinitions="Auto,*,Auto" ColumnSpacing="12">
                    <Border WidthRequest="48" HeightRequest="48"
                            BackgroundColor="{StaticResource AccentBlue}"
                            StrokeThickness="0" StrokeShape="RoundRectangle 14"
                            VerticalOptions="Center">
                        <Label Text="K" FontSize="30" FontAttributes="Bold"
                               TextColor="#FFFFFF" HorizontalOptions="Center"
                               VerticalOptions="Center" />
                    </Border>
                    <VerticalStackLayout Grid.Column="1" Spacing="1" VerticalOptions="Center">
                        <Label Text="KYNOXIO" FontSize="19" FontAttributes="Bold"
                               CharacterSpacing="2" TextColor="{StaticResource TextPrimary}" />
                        <Label Text="COUNT  /  INDUSTRIAL VISION" FontSize="9"
                               CharacterSpacing="1.5" TextColor="{StaticResource AccentCyan}" />
                    </VerticalStackLayout>
                    <Border Grid.Column="2" Padding="10,7" Stroke="{StaticResource BorderColor}"
                            StrokeThickness="1" BackgroundColor="{StaticResource Surface}"
                            StrokeShape="RoundRectangle 12" VerticalOptions="Center">
                        <Label Text="0.1.0" FontSize="11" TextColor="{StaticResource TextSecondary}" />
                    </Border>
                </Grid>

                <!-- Mensaje principal -->
                <VerticalStackLayout Spacing="7">
                    <Label Text="CONTEO INTELIGENTE" FontSize="11" FontAttributes="Bold"
                           CharacterSpacing="2" TextColor="{StaticResource AccentCyan}" />
                    <Label Text="Cada pieza&#10;cuenta." FontSize="36" FontAttributes="Bold"
                           LineHeight="0.95" TextColor="{StaticResource TextPrimary}" />
                    <Label Text="Cuenta materiales y componentes industriales mediante vision artificial."
                           FontSize="14" LineHeight="1.35"
                           TextColor="{StaticResource TextSecondary}" />
                </VerticalStackLayout>

                <!-- Accion principal -->
                <Border Padding="18" BackgroundColor="{StaticResource SurfaceRaised}"
                        Stroke="{StaticResource BorderColor}" StrokeThickness="1"
                        StrokeShape="RoundRectangle 22">
                    <Grid RowDefinitions="Auto,Auto,Auto" ColumnDefinitions="*,Auto"
                          RowSpacing="12">
                        <VerticalStackLayout Spacing="5">
                            <Label Text="NUEVO PROCESO" FontSize="10" FontAttributes="Bold"
                                   CharacterSpacing="1.8" TextColor="{StaticResource AccentCyan}" />
                            <Label Text="Iniciar un conteo" FontSize="22" FontAttributes="Bold"
                                   TextColor="{StaticResource TextPrimary}" />
                            <Label Text="Comienza con una fotografia o prepara una captura en vivo."
                                   FontSize="12" TextColor="{StaticResource TextSecondary}" />
                        </VerticalStackLayout>
                        <Border Grid.Column="1" Grid.RowSpan="2" WidthRequest="54" HeightRequest="54"
                                BackgroundColor="#183B50" StrokeThickness="0"
                                StrokeShape="RoundRectangle 16" VerticalOptions="Center">
                            <Label Text="＋" FontSize="32" TextColor="{StaticResource AccentCyan}"
                                   HorizontalOptions="Center" VerticalOptions="Center" />
                        </Border>
                        <Button Grid.Row="2" Grid.ColumnSpan="2" Text="CREAR NUEVO CONTEO"
                                Clicked="OnNewCountClicked" Style="{StaticResource PrimaryActionButton}" />
                    </Grid>
                </Border>

                <!-- Modos de captura -->
                <VerticalStackLayout Spacing="12">
                    <Grid ColumnDefinitions="*,Auto">
                        <Label Text="CAPTURA" FontSize="11" FontAttributes="Bold"
                               CharacterSpacing="1.8" TextColor="{StaticResource TextSecondary}" />
                        <Label Grid.Column="1" Text="2 MODALIDADES" FontSize="9"
                               CharacterSpacing="1" TextColor="{StaticResource TextMuted}" />
                    </Grid>
                    <Grid ColumnDefinitions="*,*" ColumnSpacing="12">
                        <Border Padding="14" BackgroundColor="{StaticResource Surface}"
                                Stroke="{StaticResource BorderColor}" StrokeThickness="1"
                                StrokeShape="RoundRectangle 18">
                            <VerticalStackLayout Spacing="10">
                                <Border WidthRequest="42" HeightRequest="42" HorizontalOptions="Start"
                                        BackgroundColor="#183B50" StrokeThickness="0"
                                        StrokeShape="RoundRectangle 13">
                                    <Label Text="▧" FontSize="24" TextColor="{StaticResource AccentCyan}"
                                           HorizontalOptions="Center" VerticalOptions="Center" />
                                </Border>
                                <VerticalStackLayout Spacing="3">
                                    <Label Text="Fotografía" FontSize="16" FontAttributes="Bold"
                                           TextColor="{StaticResource TextPrimary}" />
                                    <Label Text="Analiza una imagen" FontSize="11"
                                           TextColor="{StaticResource TextSecondary}" />
                                </VerticalStackLayout>
                                <Button Text="ABRIR" Clicked="OnPhotoClicked"
                                        Style="{StaticResource SecondaryActionButton}" />
                            </VerticalStackLayout>
                        </Border>
                        <Border Grid.Column="1" Padding="14" BackgroundColor="{StaticResource Surface}"
                                Stroke="{StaticResource BorderColor}" StrokeThickness="1"
                                StrokeShape="RoundRectangle 18">
                            <VerticalStackLayout Spacing="10">
                                <Border WidthRequest="42" HeightRequest="42" HorizontalOptions="Start"
                                        BackgroundColor="#183B50" StrokeThickness="0"
                                        StrokeShape="RoundRectangle 13">
                                    <Label Text="◉" FontSize="23" TextColor="{StaticResource AccentCyan}"
                                           HorizontalOptions="Center" VerticalOptions="Center" />
                                </Border>
                                <VerticalStackLayout Spacing="3">
                                    <Label Text="Video en vivo" FontSize="16" FontAttributes="Bold"
                                           TextColor="{StaticResource TextPrimary}" />
                                    <Label Text="Conteo en tiempo real" FontSize="11"
                                           TextColor="{StaticResource TextSecondary}" />
                                </VerticalStackLayout>
                                <Button Text="INICIAR" Clicked="OnLiveVideoClicked"
                                        Style="{StaticResource SecondaryActionButton}" />
                            </VerticalStackLayout>
                        </Border>
                    </Grid>
                </VerticalStackLayout>

                <!-- Accesos secundarios -->
                <VerticalStackLayout Spacing="12">
                    <Label Text="ESPACIO DE TRABAJO" FontSize="11" FontAttributes="Bold"
                           CharacterSpacing="1.8" TextColor="{StaticResource TextSecondary}" />
                    <Border Padding="15" BackgroundColor="{StaticResource Surface}"
                            Stroke="{StaticResource BorderColor}" StrokeThickness="1"
                            StrokeShape="RoundRectangle 16">
                        <Grid ColumnDefinitions="Auto,*,Auto" ColumnSpacing="12">
                            <Label Text="▤" FontSize="22" TextColor="{StaticResource AccentCyan}"
                                   VerticalOptions="Center" />
                            <VerticalStackLayout Grid.Column="1" Spacing="2" VerticalOptions="Center">
                                <Label Text="Historial de conteos" FontSize="14" FontAttributes="Bold"
                                       TextColor="{StaticResource TextPrimary}" />
                                <Label Text="Consulta tus procesos guardados" FontSize="11"
                                       TextColor="{StaticResource TextSecondary}" />
                            </VerticalStackLayout>
                            <Label Grid.Column="2" Text="›" FontSize="25"
                                   TextColor="{StaticResource TextMuted}" VerticalOptions="Center" />
                        </Grid>
                    </Border>
                    <Border Padding="15" BackgroundColor="{StaticResource Surface}"
                            Stroke="{StaticResource BorderColor}" StrokeThickness="1"
                            StrokeShape="RoundRectangle 16">
                        <Grid ColumnDefinitions="Auto,*,Auto" ColumnSpacing="12">
                            <Label Text="⚙" FontSize="22" TextColor="{StaticResource AccentCyan}"
                                   VerticalOptions="Center" />
                            <VerticalStackLayout Grid.Column="1" Spacing="2" VerticalOptions="Center">
                                <Label Text="Configuración" FontSize="14" FontAttributes="Bold"
                                       TextColor="{StaticResource TextPrimary}" />
                                <Label Text="Preferencias de la aplicación" FontSize="11"
                                       TextColor="{StaticResource TextSecondary}" />
                            </VerticalStackLayout>
                            <Label Grid.Column="2" Text="›" FontSize="25"
                                   TextColor="{StaticResource TextMuted}" VerticalOptions="Center" />
                        </Grid>
                    </Border>
                </VerticalStackLayout>

                <Label Text="KYNOXIO  ·  EVERYTHING CONNECTED. EVERYTHING SMARTER."
                       FontSize="8" CharacterSpacing="0.7" HorizontalTextAlignment="Center"
                       TextColor="{StaticResource TextMuted}" />
            </VerticalStackLayout>
        </ScrollView>

        <!-- Navegacion inferior inicial -->
        <Grid Grid.Row="1" Padding="18,10" BackgroundColor="{StaticResource Surface}"
              ColumnDefinitions="*,*,*" ColumnSpacing="8">
            <VerticalStackLayout Spacing="3" HorizontalOptions="Center">
                <Label Text="⌂" FontSize="20" TextColor="{StaticResource AccentCyan}"
                       HorizontalTextAlignment="Center" />
                <Label Text="INICIO" FontSize="9" CharacterSpacing="1"
                       TextColor="{StaticResource AccentCyan}" />
            </VerticalStackLayout>
            <VerticalStackLayout Grid.Column="1" Spacing="3" HorizontalOptions="Center">
                <Label Text="▤" FontSize="20" TextColor="{StaticResource TextMuted}"
                       HorizontalTextAlignment="Center" />
                <Label Text="HISTORIAL" FontSize="9" CharacterSpacing="1"
                       TextColor="{StaticResource TextMuted}" />
            </VerticalStackLayout>
            <VerticalStackLayout Grid.Column="2" Spacing="3" HorizontalOptions="Center">
                <Label Text="⚙" FontSize="20" TextColor="{StaticResource TextMuted}"
                       HorizontalTextAlignment="Center" />
                <Label Text="AJUSTES" FontSize="9" CharacterSpacing="1"
                       TextColor="{StaticResource TextMuted}" />
            </VerticalStackLayout>
        </Grid>
    </Grid>
</ContentPage>
'@

Write-Utf8File "KYNOXIO.Count.App\MainPage.xaml.cs" @'
namespace KYNOXIO.Count.App;

/// <summary>
/// Pantalla principal de KYNOXIO Count.
/// Los accesos muestran su estado hasta implementar captura, historial y ajustes.
/// </summary>
public partial class MainPage : ContentPage
{
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
            "Video en vivo");

        if (option == "Tomar fotografía")
            await ShowNextStageMessageAsync("Captura fotográfica");
        else if (option == "Video en vivo")
            await ShowNextStageMessageAsync("Conteo mediante video en vivo");
    }

    private async void OnPhotoClicked(object sender, EventArgs e)
    {
        await ShowNextStageMessageAsync("Captura fotográfica");
    }

    private async void OnLiveVideoClicked(object sender, EventArgs e)
    {
        await ShowNextStageMessageAsync("Conteo mediante video en vivo");
    }

    private Task ShowNextStageMessageAsync(string feature)
    {
        return DisplayAlert(
            feature,
            "Esta función se implementará en la siguiente etapa. El dashboard ya está preparado para integrarla.",
            "Entendido");
    }
}
'@

Write-Utf8File "KYNOXIO.Count.App\App.xaml" @'
<?xml version="1.0" encoding="utf-8" ?>
<Application
    x:Class="KYNOXIO.Count.App.App"
    xmlns="http://schemas.microsoft.com/dotnet/2021/maui"
    xmlns:x="http://schemas.microsoft.com/winfx/2009/xaml">
    <Application.Resources>
        <ResourceDictionary>
            <ResourceDictionary.MergedDictionaries>
                <ResourceDictionary Source="Resources/Styles/Colors.xaml" />
                <ResourceDictionary Source="Resources/Styles/Styles.xaml" />
            </ResourceDictionary.MergedDictionaries>
        </ResourceDictionary>
    </Application.Resources>
</Application>
'@

Write-Utf8File "KYNOXIO.Count.App\Resources\Styles\Colors.xaml" @'
<?xml version="1.0" encoding="UTF-8" ?>
<ResourceDictionary
    xmlns="http://schemas.microsoft.com/dotnet/2021/maui"
    xmlns:x="http://schemas.microsoft.com/winfx/2009/xaml">
    <!-- Paleta KYNOXIO: azul profundo, cian y superficies industriales -->
    <Color x:Key="AppBackground">#0B1420</Color>
    <Color x:Key="Surface">#111F2E</Color>
    <Color x:Key="SurfaceRaised">#15283A</Color>
    <Color x:Key="BorderColor">#263B4D</Color>
    <Color x:Key="AccentBlue">#087CA7</Color>
    <Color x:Key="AccentCyan">#22D3EE</Color>
    <Color x:Key="TextPrimary">#F2F7FA</Color>
    <Color x:Key="TextSecondary">#A6B7C5</Color>
    <Color x:Key="TextMuted">#718596</Color>
</ResourceDictionary>
'@

Write-Utf8File "KYNOXIO.Count.App\Resources\Styles\Styles.xaml" @'
<?xml version="1.0" encoding="UTF-8" ?>
<ResourceDictionary
    xmlns="http://schemas.microsoft.com/dotnet/2021/maui"
    xmlns:x="http://schemas.microsoft.com/winfx/2009/xaml">
    <Style TargetType="Button" x:Key="PrimaryActionButton">
        <Setter Property="BackgroundColor" Value="{StaticResource AccentBlue}" />
        <Setter Property="TextColor" Value="#FFFFFF" />
        <Setter Property="FontSize" Value="12" />
        <Setter Property="FontAttributes" Value="Bold" />
        <Setter Property="CharacterSpacing" Value="1" />
        <Setter Property="CornerRadius" Value="12" />
        <Setter Property="HeightRequest" Value="48" />
        <Setter Property="Padding" Value="14,10" />
    </Style>
    <Style TargetType="Button" x:Key="SecondaryActionButton">
        <Setter Property="BackgroundColor" Value="#1A3448" />
        <Setter Property="TextColor" Value="{StaticResource AccentCyan}" />
        <Setter Property="FontSize" Value="11" />
        <Setter Property="FontAttributes" Value="Bold" />
        <Setter Property="CharacterSpacing" Value="1" />
        <Setter Property="CornerRadius" Value="10" />
        <Setter Property="HeightRequest" Value="42" />
        <Setter Property="Padding" Value="10,8" />
    </Style>
</ResourceDictionary>
'@

$projectPath = Join-Path $root "KYNOXIO.Count.App\KYNOXIO.Count.App.csproj"
$project = [System.IO.File]::ReadAllText($projectPath)
$project = $project.Replace("<ApplicationTitle>KYNOXIO.Count.App</ApplicationTitle>", "<ApplicationTitle>KYNOXIO Count</ApplicationTitle>")
$project = $project.Replace("<ApplicationId>com.companyname.kynoxio.count.app</ApplicationId>", "<ApplicationId>com.kynoxio.count</ApplicationId>")
$project = $project.Replace("<ApplicationDisplayVersion>1.0</ApplicationDisplayVersion>", "<ApplicationDisplayVersion>0.1.0</ApplicationDisplayVersion>")
$project = $project.Replace("<ApplicationVersion>1</ApplicationVersion>", "<ApplicationVersion>1</ApplicationVersion>")
[System.IO.File]::WriteAllText($projectPath, $project, [System.Text.UTF8Encoding]::new($false))

Write-Utf8File "KYNOXIO.Count.App\App.xaml.cs" @'
namespace KYNOXIO.Count.App;

public partial class App : Application
{
    public App()
    {
        InitializeComponent();

        // KYNOXIO Count utiliza una apariencia oscura industrial.
        UserAppTheme = AppTheme.Dark;

        MainPage = new AppShell();
    }
}
'@

Write-Utf8File "KYNOXIO.Count.App\AppShell.xaml" @'
<?xml version="1.0" encoding="UTF-8" ?>
<Shell
    x:Class="KYNOXIO.Count.App.AppShell"
    xmlns="http://schemas.microsoft.com/dotnet/2021/maui"
    xmlns:x="http://schemas.microsoft.com/winfx/2009/xaml"
    xmlns:local="clr-namespace:KYNOXIO.Count.App"
    FlyoutBehavior="Disabled"
    Shell.NavBarIsVisible="False"
    Title="KYNOXIO Count">
    <ShellContent
        ContentTemplate="{DataTemplate local:MainPage}"
        Route="MainPage" />
</Shell>
'@

Write-Host ""
Write-Host "Dashboard KYNOXIO Count aplicado." -ForegroundColor Green
Write-Host "Siguiente paso: compilar y probar en el Samsung Galaxy A52."
