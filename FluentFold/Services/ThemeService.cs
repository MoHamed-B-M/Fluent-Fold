using Microsoft.UI;
using Microsoft.UI.Xaml;
using Windows.UI.ViewManagement;

namespace FluentFold.Services;

public class ThemeService
{
    private readonly UISettings _uiSettings = new();

    public ElementTheme CurrentTheme { get; private set; }

    public event Action<ElementTheme>? ThemeChanged;

    public ThemeService()
    {
        CurrentTheme = GetSystemTheme();
        _uiSettings.ColorValuesChanged += OnColorValuesChanged;
    }

    public void SetTheme(ElementTheme theme)
    {
        CurrentTheme = theme;
        ThemeChanged?.Invoke(theme);
    }

    public void ToggleTheme()
    {
        CurrentTheme = CurrentTheme switch
        {
            ElementTheme.Light => ElementTheme.Dark,
            ElementTheme.Dark => ElementTheme.Light,
            _ => ElementTheme.Default
        };
        ThemeChanged?.Invoke(CurrentTheme);
    }

    public ElementTheme GetSystemTheme()
    {
        var uiSettings = new UISettings();
        var foreground = uiSettings.GetColorValue(UIColorType.Foreground);
        // Simple heuristic: if foreground is light, background is dark
        return foreground.R > 128 && foreground.G > 128 && foreground.B > 128
            ? ElementTheme.Dark
            : ElementTheme.Light;
    }

    private void OnColorValuesChanged(UISettings sender, object args)
    {
        // System theme changed - update if using default/auto
        if (CurrentTheme == ElementTheme.Default)
        {
            ThemeChanged?.Invoke(GetSystemTheme());
        }
    }
}
