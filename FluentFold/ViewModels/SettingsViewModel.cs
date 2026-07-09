using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.UI.Xaml;

namespace FluentFold.ViewModels;

public partial class SettingsViewModel : ObservableObject
{
    [ObservableProperty] private bool _isDarkTheme;

    public SettingsViewModel()
    {
        IsDarkTheme = Application.Current.RequestedTheme == ApplicationTheme.Dark;
    }

    partial void OnIsDarkThemeChanged(bool value)
    {
        if (App.MainWindow?.Content is FrameworkElement element)
        {
            element.RequestedTheme = value ? ElementTheme.Dark : ElementTheme.Light;
        }
    }
}