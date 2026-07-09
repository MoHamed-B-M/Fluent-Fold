using Microsoft.UI.Xaml;
using FluentFold.Views;

namespace FluentFold;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        Title = "Fluent Fold";
        ExtendsContentIntoTitleBar = true;

        var rootFrame = new Microsoft.UI.Xaml.Controls.Frame();
        Content = rootFrame;
        rootFrame.Navigate(typeof(MainPage));

        // Enable Mica
        if (Microsoft.UI.Composition.SystemBackdrops.MicaController.IsSupported())
        {
            var micaController = new Microsoft.UI.Composition.SystemBackdrops.MicaController();
            var configurationSource = new Microsoft.UI.Composition.SystemBackdrops.SystemBackdropConfiguration
            {
                IsInputActive = true,
                Theme = SystemBackdropTheme.Default
            };
            micaController.AddSystemBackdropTarget(this);
            micaController.SetSystemBackdropConfiguration(configurationSource);
        }
    }
}