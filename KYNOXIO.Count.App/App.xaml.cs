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