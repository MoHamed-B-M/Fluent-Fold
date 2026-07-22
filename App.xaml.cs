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
        UnhandledException += (_, e) =>
        {
            var log = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "FluentFold", "crash.log");
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(log)!);
                File.AppendAllText(log, $"[{DateTime.Now:O}] Unhandled: {e.Exception}\n");
            }
            catch { }
            e.Handled = true;
        };
    }

    protected override void OnLaunched(Microsoft.UI.Xaml.LaunchActivatedEventArgs args)
    {
        try
        {
            var services = new ServiceCollection();
            ConfigureServices(services);
            Services = services.BuildServiceProvider();

            MainWindow = new MainWindow(Services);
            MainWindow.Activate();
        }
        catch (Exception ex)
        {
            var log = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "FluentFold", "crash.log");
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(log)!);
                File.AppendAllText(log, $"[{DateTime.Now:O}] Startup failed: {ex}\n");
            }
            catch { }
        }
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
