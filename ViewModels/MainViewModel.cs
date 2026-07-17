using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.UI.Xaml.Controls;
using FluentFold.Views;

namespace FluentFold.ViewModels;

public sealed partial class MainViewModel : ObservableObject
{
    private Frame? _contentFrame;

#pragma warning disable MVVMTK0045
    [ObservableProperty]
    private string _selectedNavTag = "organizer";
#pragma warning restore MVVMTK0045

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
            "settings" => typeof(SettingsPage),
            _ => typeof(OrganizerPage)
        };

        if (_contentFrame.CurrentSourcePageType != pageType)
            _contentFrame.Navigate(pageType);
    }
}
