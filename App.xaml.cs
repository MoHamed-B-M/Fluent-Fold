using Microsoft.UI.Xaml;
using FluentFold.Services;

namespace FluentFold;

public partial class App : Application
{
    private Window? _window;

    public App()
    {
        InitializeComponent();
    }

    protected override void OnLaunched(Microsoft.UI.Xaml.LaunchActivatedEventArgs args)
    {
        RegisterServices();
        _window = new MainWindow();
        _window.Activate();
    }

    private static void RegisterServices()
    {
        ServiceLocator.Register<IOrganizerService>(new OrganizerService());
    }
}
