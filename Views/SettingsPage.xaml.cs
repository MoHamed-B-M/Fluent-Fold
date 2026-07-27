using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using FluentFold.ViewModels;

namespace FluentFold.Views;

public sealed partial class SettingsPage : Page
{
    private static readonly Guid AppGuid = Guid.Parse("C5A1E627-2AFB-440C-A06A-231E03AB2ED4");

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

    private async void OnUninstallClick(object sender, RoutedEventArgs e)
    {
        var deleteData = DeleteDataCheckBox.IsChecked == true;

        var dialog = new ContentDialog
        {
            XamlRoot = XamlRoot,
            Title = "Uninstall FluentFold",
            Content = deleteData
                ? "This will uninstall FluentFold and delete all app data (settings, cache, logs). Continue?"
                : "This will uninstall FluentFold. App data will be kept. Continue?",
            PrimaryButtonText = "Uninstall",
            CloseButtonText = "Cancel",
            DefaultButton = ContentDialogButton.Close
        };

        if (await dialog.ShowAsync() != ContentDialogResult.Primary)
            return;

        if (deleteData)
        {
            try
            {
                var dataDir = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "FluentFold");
                if (Directory.Exists(dataDir))
                    Directory.Delete(dataDir, true);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to delete app data: {ex.Message}");
            }
        }

        try
        {
            var uninstallPath = FindUninstaller();
            if (!string.IsNullOrEmpty(uninstallPath))
            {
                var psi = new System.Diagnostics.ProcessStartInfo
                {
                    FileName = uninstallPath,
                    UseShellExecute = true
                };
                System.Diagnostics.Process.Start(psi);
                Application.Current.Exit();
            }
            else
            {
                var failDialog = new ContentDialog
                {
                    XamlRoot = XamlRoot,
                    Title = "Uninstaller Not Found",
                    Content = "Could not find the uninstaller. Please uninstall from Settings > Apps > Installed apps.",
                    PrimaryButtonText = "OK"
                };
                await failDialog.ShowAsync();
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Failed to launch uninstaller: {ex.Message}");
        }
    }

    private static string? FindUninstaller()
    {
        var keyName = $"{{{AppGuid}}}_is1";
        var paths = new[]
        {
            @$"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\{keyName}",
            @$"SOFTWARE\WOW6432Node\Microsoft\Windows\CurrentVersion\Uninstall\{keyName}"
        };

        foreach (var path in paths)
        {
            try
            {
                using var key = Microsoft.Win32.Registry.LocalMachine.OpenSubKey(path);
                var uninstall = key?.GetValue("UninstallString") as string;
                if (!string.IsNullOrEmpty(uninstall))
                    return uninstall;
            }
            catch { }
        }
        return null;
    }
}
