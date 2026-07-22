using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using FluentFold.ViewModels;

namespace FluentFold.Views;

public sealed partial class SettingsPage : Page
{
    public SettingsViewModel ViewModel { get; set; } = null!;

    public SettingsPage()
    {
        try
        {
            ViewModel = App.Services.GetRequiredService<SettingsViewModel>();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[SettingsPage] DI failed: {ex}");
            throw;
        }
        InitializeComponent();
    }

    private void OnResetOnboardingClick(object sender, RoutedEventArgs e)
    {
        ViewModel.ResetOnboardingCommand.Execute(null);
        App.MainWindow?.ShowOnboarding();
    }

    private async void OnSaveClick(object sender, RoutedEventArgs e)
    {
        var dialog = new ContentDialog
        {
            XamlRoot = XamlRoot,
            Title = "Settings Saved",
            PrimaryButtonText = "OK",
            DefaultButton = ContentDialogButton.Primary,
            Content = new TextBlock
            {
                Text = "All settings have been saved and applied.",
                TextWrapping = TextWrapping.Wrap
            }
        };

        await dialog.ShowAsync();
    }
}
