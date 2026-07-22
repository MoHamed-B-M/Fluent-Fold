using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Xaml;
using FluentFold.Services;
using FluentFold.ViewModels;
using FluentFold.Views;

namespace FluentFold;

public partial class App : Application
{
    public static MainWindow? MainWindow { get; private set; }
    public static IServiceProvider Services { get; private set; } = null!;

    public App()
    {
        InitializeComponent();
    }

    protected override void OnLaunched(Microsoft.UI.Xaml.LaunchActivatedEventArgs args)
    {
        var services = new ServiceCollection();
        ConfigureServices(services);
        Services = services.BuildServiceProvider();

        MainWindow = new MainWindow(Services);
        MainWindow.Activate();
    }

    private static void ConfigureServices(IServiceCollection services)
    {
        services.AddLogging(builder =>
        {
            builder.SetMinimumLevel(LogLevel.Information);
        });

        services.AddSingleton<IWindowService, WindowService>();
        services.AddSingleton<IAppSettingsService, AppSettingsService>();
        services.AddSingleton<IUndoService, UndoService>();
        services.AddSingleton<IFirstLaunchService, FirstLaunchService>();
        services.AddTransient<IFolderPickerService, FolderPickerService>();
        services.AddTransient<IOrganizerService, OrganizerService>();
        services.AddTransient<IAnalyzerService, AnalyzerService>();
        services.AddTransient<IRenamingService, RenamingService>();
        services.AddTransient<IRulesEngine, RulesEngine>();
        services.AddTransient<MainViewModel>();
        services.AddTransient<OrganizerViewModel>();
        services.AddTransient<AnalyzerViewModel>();
        services.AddTransient<SettingsViewModel>();
        services.AddTransient<HistoryViewModel>();
    }
}
