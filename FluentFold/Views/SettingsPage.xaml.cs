using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace FluentFold.Views;

public sealed partial class SettingsPage : Page
{
    public SettingsPage()
    {
        InitializeComponent();
        ThemeToggle.IsOn = Application.Current.RequestedTheme == ApplicationTheme.Dark;
    }

    private void OnThemeToggled(object sender, RoutedEventArgs e)
    {
        if (App.MainWindow?.Content is FrameworkElement element)
        {
            element.RequestedTheme = ThemeToggle.IsOn ? ElementTheme.Dark : ElementTheme.Light;
        }
    }
}