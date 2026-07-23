using System.Reflection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Windows.ApplicationModel;
using Windows.System;

namespace FluentFold.Views;

public sealed partial class AboutPage : Page
{
    public AboutPage()
    {
        InitializeComponent();
        try
        {
            var ver = Package.Current.Id.Version;
            VersionText.Text = $"Version {ver.Major}.{ver.Minor}.{ver.Build}";
        }
        catch
        {
            var ver = Assembly.GetExecutingAssembly().GetName().Version;
            VersionText.Text = $"Version {ver?.Major ?? 1}.{ver?.Minor ?? 0}.{ver?.Build ?? 0}";
        }
    }

    private async void OnGitHubClick(object sender, RoutedEventArgs e)
    {
        await Launcher.LaunchUriAsync(new Uri("https://github.com/MoHamed-B-M/Fluent-Fold"));
    }

    private async void OnFeedbackClick(object sender, RoutedEventArgs e)
    {
        await Launcher.LaunchUriAsync(new Uri("https://github.com/MoHamed-B-M/Fluent-Fold/issues"));
    }
}
