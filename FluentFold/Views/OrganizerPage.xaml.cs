using FluentFold.ViewModels;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Windows.Storage;
using WinRT.Interop;

namespace FluentFold.Views;

public sealed partial class OrganizerPage : Page
{
    public OrganizerViewModel ViewModel { get; } = new();

    public OrganizerPage()
    {
        this.InitializeComponent();
    }

    private async void OnPickFolder(object sender, RoutedEventArgs e)
    {
        var picker = new Windows.Storage.Pickers.FolderPicker
        {
            SuggestedStartLocation = Windows.Storage.Pickers.PickerLocationId.Desktop
        };
        picker.FileTypeFilter.Add("*");

        var hwnd = WindowNative.GetWindowHandle(App.MainWindow);
        InitializeWithWindow.Initialize(picker, hwnd);

        var folder = await picker.PickSingleFolderAsync();
        if (folder != null)
        {
            ViewModel.SelectedFolderPath = folder.Path;
            FolderPathText.Text = folder.Path;
            OptionsPanel.Visibility = Visibility.Visible;
            await LoadSummaryAsync(folder);
        }
    }

    private async Task LoadSummaryAsync(StorageFolder folder)
    {
        ViewModel.IsWorking = true;
        ProgressControl.IsActive = true;
        ProgressControl.Visibility = Visibility.Visible;

        try
        {
            var summary = await ViewModel._organizer.GetFolderSummaryAsync(folder);
            ViewModel.FolderSummary = summary;
            ViewModel.CategoryItems.Clear();
            foreach (var kvp in summary.CategoryCounts)
            {
                ViewModel.CategoryItems.Add(new CategoryCountItem
                {
                    CategoryName = kvp.Key.ToString(),
                    Count = kvp.Value
                });
            }
            CategoryList.ItemsSource = ViewModel.CategoryItems;
            SummaryHeader.Visibility = Visibility.Visible;
            CategoryList.Visibility = Visibility.Visible;
        }
        finally
        {
            ViewModel.IsWorking = false;
            ProgressControl.IsActive = false;
            ProgressControl.Visibility = Visibility.Collapsed;
        }
    }

    private void OnRenameToggled(object sender, RoutedEventArgs e)
    {
        PatternBox.IsEnabled = RenameToggle.IsOn;
        ViewModel.RenameFiles = RenameToggle.IsOn;
    }

    private async void OnOrganize(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(ViewModel.SelectedFolderPath))
        {
            ShowInfoBar("Please select a folder first.", InfoBarSeverity.Warning);
            return;
        }

        ViewModel.OrganizeByCategory = OrganizeToggle.IsOn;
        ViewModel.RenamePattern = PatternBox.Text;

        ViewModel.IsWorking = true;
        ProgressControl.IsActive = true;
        ProgressControl.Visibility = Visibility.Visible;
        ViewModel.CanUndo = false;

        try
        {
            var folder = await StorageFolder.GetFolderFromPathAsync(ViewModel.SelectedFolderPath);
            var result = await ViewModel._organizer.OrganizeFolderAsync(
                folder, ViewModel.RenamePattern, ViewModel.OrganizeByCategory, ViewModel.RenameFiles);

            if (result.Success)
            {
                ShowInfoBar(result.Message, InfoBarSeverity.Success);
                ViewModel.CanUndo = true;
                UndoButton.IsEnabled = true;
                await LoadSummaryAsync(folder);
            }
            else
            {
                ShowInfoBar(result.Message, InfoBarSeverity.Error);
            }
        }
        catch (Exception ex)
        {
            ShowInfoBar($"Error: {ex.Message}", InfoBarSeverity.Error);
        }
        finally
        {
            ViewModel.IsWorking = false;
            ProgressControl.IsActive = false;
            ProgressControl.Visibility = Visibility.Collapsed;
        }
    }

    private async void OnUndo(object sender, RoutedEventArgs e)
    {
        ViewModel.IsWorking = true;
        ProgressControl.IsActive = true;
        ProgressControl.Visibility = Visibility.Visible;

        try
        {
            var success = await ViewModel._undo.UndoAsync();
            if (success)
            {
                ShowInfoBar("Undo completed successfully.", InfoBarSeverity.Success);
                ViewModel.CanUndo = false;
                UndoButton.IsEnabled = false;
                if (!string.IsNullOrWhiteSpace(ViewModel.SelectedFolderPath))
                {
                    var folder = await StorageFolder.GetFolderFromPathAsync(ViewModel.SelectedFolderPath);
                    await LoadSummaryAsync(folder);
                }
            }
            else
            {
                ShowInfoBar("Nothing to undo.", InfoBarSeverity.Informational);
            }
        }
        finally
        {
            ViewModel.IsWorking = false;
            ProgressControl.IsActive = false;
            ProgressControl.Visibility = Visibility.Collapsed;
        }
    }

    private void ShowInfoBar(string message, InfoBarSeverity severity)
    {
        InfoBarControl.Message = message;
        InfoBarControl.Severity = (Microsoft.UI.Xaml.Controls.InfoBarSeverity)severity;
        InfoBarControl.IsOpen = true;
    }
}

public enum InfoBarSeverity
{
    Informational = 0,
    Success = 1,
    Warning = 2,
    Error = 3
}