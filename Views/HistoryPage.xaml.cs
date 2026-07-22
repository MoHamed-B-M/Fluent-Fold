using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using FluentFold.Models;
using FluentFold.ViewModels;

namespace FluentFold.Views;

public sealed partial class HistoryPage : Page
{
    public HistoryViewModel ViewModel { get; set; } = null!;

    public HistoryPage()
    {
        try
        {
            ViewModel = App.Services.GetRequiredService<HistoryViewModel>();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[HistoryPage] DI failed: {ex}");
            throw;
        }
        InitializeComponent();
        Loaded += (_, _) => ViewModel.RefreshHistory();
    }

    private void OnUndoClick(object sender, RoutedEventArgs e)
    {
        if (sender is Button button && button.DataContext is OrganizeOperation operation)
        {
            _ = ViewModel.UndoOperationCommand.ExecuteAsync(operation);
        }
    }
}
