using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FluentFold.Services;

namespace FluentFold.ViewModels;

public partial class SettingsViewModel : ObservableObject
{
    private readonly ThemeService _themeService;

    [ObservableProperty]
    private bool _isDarkTheme;

    [ObservableProperty]
    private string _themeLabel = "Light";

    public string AppVersion => "1.0.0.0";

    public SettingsViewModel(ThemeService themeService)
    {
        _themeService = themeService;
        IsDarkTheme = _themeService.CurrentTheme == ElementTheme.Dark;
        ThemeLabel = IsDarkTheme ? "Dark" : "Light";

        _themeService.ThemeChanged += OnThemeChanged;
    }

    partial void OnIsDarkThemeChanged(bool value)
    {
        ThemeLabel = value ? "Dark" : "Light";
        _themeService.SetTheme(value ? ElementTheme.Dark : ElementTheme.Light);
    }

    private void OnThemeChanged(ElementTheme theme)
    {
        IsDarkTheme = theme == ElementTheme.Dark;
    }
}
