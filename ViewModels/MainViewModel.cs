using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Xaml.Controls;
using FluentFold.Views;

namespace FluentFold.ViewModels;

public sealed partial class MainViewModel : ObservableObject
{
    private readonly ILogger<MainViewModel> _logger;
    private Frame? _contentFrame;

#pragma warning disable MVVMTK0045
    [ObservableProperty]
    private string _selectedNavTag = "organizer";
#pragma warning restore MVVMTK0045

    public MainViewModel(ILogger<MainViewModel> logger)
    {
        _logger = logger;
    }

    public void Initialize(Frame frame)
    {
        _contentFrame = frame;
        NavigateToPageCommand.Execute("organizer");
    }

    [RelayCommand]
    private void NavigateToPage(string? pageTag)
    {
        if (_contentFrame is null || pageTag is null)
            return;

        var pageType = pageTag switch
        {
            "organizer" => typeof(OrganizerPage),
            "history" => typeof(HistoryPage),
            "analyzer" => typeof(AnalyzerPage),
            "settings" => typeof(SettingsPage),
            "about" => typeof(AboutPage),
            _ => typeof(OrganizerPage)
        };

        if (_contentFrame.CurrentSourcePageType == pageType)
            return;

        try
        {
            if (!_contentFrame.Navigate(pageType))
                _logger.LogError("Navigation to {Page} failed (Navigate returned false)", pageType.Name);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Navigation to {Page} threw an exception", pageType.Name);
        }
    }
}
