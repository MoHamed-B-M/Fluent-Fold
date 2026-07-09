using System;
using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using FluentFold.Services;
using FluentFold.ViewModels;
using FluentFold.Views;

namespace FluentFold;

public partial class App : Application
{
    private static Window? _mainWindow;
    private static IServiceProvider? _serviceProvider;
    private ThemeService? _themeService;

    public static Window MainWindow =>
        _mainWindow ?? throw new InvalidOperationException("Window not initialized");

    public static T GetService<T>() where T : class =>
        _serviceProvider?.GetService<T>() ??
        throw new InvalidOperationException($"Service {typeof(T).Name} not registered");

    public App()
    {
        InitializeComponent();
    }

    protected override void OnLaunched(LaunchActivatedEventArgs args)
    {
        _mainWindow = new Window();
        _mainWindow.Title = "Fluent Fold";

        ConfigureServices();
        _themeService = GetService<ThemeService>();

        _mainWindow.SystemBackdrop = new MicaBackdrop();

        var shell = CreateShell();
        _mainWindow.Content = shell;
        _mainWindow.Activate();

        ApplyTheme(_themeService.CurrentTheme);

        _themeService.ThemeChanged += theme => ApplyTheme(theme);
    }

    private static void ConfigureServices()
    {
        var services = new ServiceCollection();
        services.AddSingleton(_ => new ThemeService());
        services.AddSingleton(_ => new OrganizerViewModel());
        services.AddTransient(sp => new SettingsViewModel(sp.GetService<ThemeService>()));
        _serviceProvider = services.BuildServiceProvider();
    }

    private static NavigationView CreateShell()
    {
        var navView = new NavigationView
        {
            OpenPaneLength = 240,
            CompactPaneLength = 48,
            PaneDisplayMode = NavigationViewPaneDisplayMode.Auto,
            AlwaysShowHeader = false,
            IsBackButtonVisible = NavigationViewBackButtonVisible.Collapsed,
            Margin = new Thickness(0),
        };

        navView.MenuItems.Add(new NavigationViewItem
        {
            Content = "Organizer",
            Tag = "organizer",
            Icon = new SymbolIcon(Symbol.Tag)
        });

        var settingsItem = new NavigationViewItem
        {
            Content = "Settings",
            Icon = new SymbolIcon(Symbol.Setting)
        };
        navView.PaneFooter = new Grid
        {
            Children = { settingsItem }
        };

        var frame = new Frame();
        navView.Content = frame;
        frame.Navigate(typeof(OrganizerPage));

        navView.SelectedItem = navView.MenuItems[0];

        navView.ItemInvoked += (_, args) =>
        {
            if (args.InvokedItemContainer == settingsItem)
            {
                frame.Navigate(typeof(SettingsPage));
            }
            else if (args.InvokedItemContainer is NavigationViewItem item)
            {
                if (item.Tag?.ToString() == "organizer")
                    frame.Navigate(typeof(OrganizerPage));
            }
        };

        return navView;
    }

    private void ApplyTheme(ElementTheme theme)
    {
        if (_mainWindow?.Content is FrameworkElement root)
        {
            root.RequestedTheme = theme;
        }
    }
}

// Minimal DI container
internal class ServiceCollection
{
    private readonly Dictionary<Type, Func<IServiceProvider, object>> _factories = new();

    public void AddSingleton<T>(Func<IServiceProvider, T> factory) where T : class
    {
        var lazy = new Lazy<object>(() => factory(new ServiceProvider(this)));
        _factories[typeof(T)] = _ => lazy.Value;
    }

    public void AddTransient<T>(Func<IServiceProvider, T> factory) where T : class
    {
        _factories[typeof(T)] = sp => factory(sp);
    }

    public IServiceProvider BuildServiceProvider() => new ServiceProvider(this);

    internal class ServiceProvider : IServiceProvider
    {
        private readonly ServiceCollection _collection;
        public ServiceProvider(ServiceCollection collection) => _collection = collection;

        public object? GetService(Type serviceType)
        {
            return _collection._factories.TryGetValue(serviceType, out var factory)
                ? factory(this)
                : null;
        }

        public T GetService<T>() where T : class => (GetService(typeof(T)) as T)!;
    }
}

internal interface IServiceProvider
{
    object? GetService(Type serviceType);
}
