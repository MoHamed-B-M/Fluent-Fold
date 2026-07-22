using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Animation;
using Microsoft.UI.Windowing;
using FluentFold.Services;
using FluentFold.ViewModels;
using WinRT.Interop;

namespace FluentFold;

public sealed partial class MainWindow : Window
{
    public MainViewModel ViewModel { get; }
    private readonly IAppSettingsService _settings;
    private readonly IFirstLaunchService _firstLaunch;
    private readonly ILogger<MainWindow> _logger;

    private int _onboardSlide;

    public MainWindow(IServiceProvider serviceProvider)
    {
        ViewModel = serviceProvider.GetRequiredService<MainViewModel>();
        _settings = serviceProvider.GetRequiredService<IAppSettingsService>();
        _firstLaunch = serviceProvider.GetRequiredService<IFirstLaunchService>();
        _logger = serviceProvider.GetRequiredService<ILogger<MainWindow>>();

        InitializeComponent();

        ExtendsContentIntoTitleBar = true;
        SetTitleBar(AppTitleBar);

        var appWindow = AppWindow;
        var iconPath = Path.Combine(AppContext.BaseDirectory, "Assets", "AppIcon.ico");
        if (File.Exists(iconPath))
            appWindow.SetIcon(iconPath);
        else
            appWindow.SetIcon("Assets/AppIcon.ico");

        var ws = (WindowService)serviceProvider.GetRequiredService<IWindowService>();
        ws.WindowHandle = WindowNative.GetWindowHandle(this);

        RestoreWindowPosition();
        Activated += OnActivated;
        Closed += OnClosed;
    }

    private async void OnActivated(object sender, WindowActivatedEventArgs e)
    {
        Activated -= OnActivated;
        ViewModel.Initialize(ContentFrame);
        await Task.Delay(200);
        ShowOnboarding();
    }

    public void ShowOnboarding()
    {
        if (!_firstLaunch.IsFirstLaunch) return;
        _onboardSlide = 0;
        ShowSlide(0);
        OnboardingOverlayElement.Visibility = Visibility.Visible;
        OnboardingCardRoot.Opacity = 1;
    }

    private void DismissOnboarding()
    {
        if (OnboardingOverlayElement.Visibility != Visibility.Visible) return;
        _firstLaunch.MarkCompleted();
        OnboardingOverlayElement.Visibility = Visibility.Collapsed;
    }

    private void OnCloseClick(object sender, RoutedEventArgs e)
    {
        DismissOnboarding();
    }

    private void OnSkipClick(object sender, RoutedEventArgs e)
    {
        DismissOnboarding();
    }

    private void OnNextClick(object sender, RoutedEventArgs e)
    {
        if (_onboardSlide >= 3)
        {
            DismissOnboarding();
            return;
        }
        ShowSlide(_onboardSlide + 1);
    }

    private void OnBackClick(object sender, RoutedEventArgs e)
    {
        if (_onboardSlide <= 0) return;
        ShowSlide(_onboardSlide - 1);
    }

    private void ShowSlide(int index)
    {
        var slides = new Panel?[] { Slide0, Slide1, Slide2, Slide3 };
        if (slides[_onboardSlide] is not null)
            slides[_onboardSlide]!.Visibility = Visibility.Collapsed;
        if (slides[index] is Panel nextPanel)
        {
            nextPanel.ChildrenTransitions = new TransitionCollection
            {
                new EntranceThemeTransition
                {
                    FromHorizontalOffset = index > _onboardSlide ? 60 : -60
                }
            };
            nextPanel.Visibility = Visibility.Visible;
        }
        _onboardSlide = index;
        BackButton.Visibility = index == 0 ? Visibility.Collapsed : Visibility.Visible;
        ToolTipService.SetToolTip(NextButton, index >= 3 ? "Get Started" : "Next");
        var accent = GetBrush("SystemAccentColor", Microsoft.UI.Colors.DodgerBlue);
        var muted = GetBrush("TextFillColorTertiaryBrush", Microsoft.UI.Colors.Gray);
        Dot0.Fill = index == 0 ? accent : muted;
        Dot1.Fill = index == 1 ? accent : muted;
        Dot2.Fill = index == 2 ? accent : muted;
        Dot3.Fill = index == 3 ? accent : muted;
    }

    private static Brush GetBrush(string key, Windows.UI.Color fallback)
    {
        try { return (Brush)Application.Current.Resources[key]; }
        catch { return new SolidColorBrush(fallback); }
    }

    private void RestoreWindowPosition()
    {
        try
        {
            var appWindow = AppWindow;
            var w = _settings.WindowWidth;
            var h = _settings.WindowHeight;
            var x = _settings.WindowX;
            var y = _settings.WindowY;

            if (w > 0 && h > 0)
            {
                var size = new Windows.Graphics.SizeInt32 { Width = (int)w, Height = (int)h };
                appWindow.Resize(size);
            }

            if (x >= 0 && y >= 0)
            {
                var pos = new Windows.Graphics.PointInt32 { X = (int)x, Y = (int)y };
                appWindow.Move(pos);
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Restore window position failed: {ex.Message}");
        }
    }

    private void OnClosed(object sender, WindowEventArgs args)
    {
        try
        {
            var appWindow = AppWindow;
            var pos = appWindow.Position;
            var size = appWindow.Size;
            _settings.WindowWidth = size.Width;
            _settings.WindowHeight = size.Height;
            _settings.WindowX = pos.X;
            _settings.WindowY = pos.Y;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Save window position failed: {ex.Message}");
        }
    }

    private void OnItemInvoked(NavigationView sender, NavigationViewItemInvokedEventArgs args)
    {
        if (args.InvokedItemContainer?.Tag is string tag)
            ViewModel.NavigateToPageCommand.Execute(tag);
    }
}
