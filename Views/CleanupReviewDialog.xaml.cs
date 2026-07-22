using System.Collections.ObjectModel;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Windows.Storage;
using FluentFold.Models;
using FluentFold.ViewModels;
using FluentFold.Helpers;

namespace FluentFold.Views;

public sealed partial class CleanupReviewDialog : ContentDialog
{
    private OrganizerViewModel ViewModel => (OrganizerViewModel)DataContext;
    private ObservableCollection<CleanupFileItem> CleanupFiles => ViewModel.CleanupFiles;

    public CleanupReviewDialog()
    {
        InitializeComponent();
    }

    private async void OnPrimaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
    {
        var deferral = args.GetDeferral();
        args.Cancel = true;
        var selected = CleanupFiles.Where(f => f.IsSelected).ToList();

        if (selected.Count > 0)
        {
            IsPrimaryButtonEnabled = false;
            try
            {
                var deletedCount = 0;
                var freedBytes = 0L;
                var useRecycleBin = ViewModel.UseRecycleBin;
                foreach (var file in selected)
                {
                    try
                    {
                        if (System.IO.File.Exists(file.FilePath))
                        {
                            if (useRecycleBin)
                            {
                                var storageFile = await StorageFile.GetFileFromPathAsync(file.FilePath);
                                await storageFile.DeleteAsync(StorageDeleteOption.Default);
                            }
                            else
                            {
                                System.IO.File.Delete(file.FilePath);
                            }
                            deletedCount++;
                            freedBytes += file.Size;
                        }
                    }
                    catch (UnauthorizedAccessException) { }
                    catch (IOException) { }
                }

                foreach (var file in selected)
                    CleanupFiles.Remove(file);

                ViewModel.ShowCleanupSuggestion = CleanupFiles.Count > 0;

                if (deletedCount > 0)
                {
                    ViewModel.ShowInfoBarInvoke(
                        $"Deleted {deletedCount} file(s), freed {FormatHelper.FormatSize(freedBytes)}.",
                        InfoBarSeverity.Success);
                }
            }
            finally
            {
                IsPrimaryButtonEnabled = true;
            }
        }

        deferral.Complete();
        Hide();
    }
}
