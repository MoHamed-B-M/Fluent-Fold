using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Composition;
using System.Numerics;
using FluentFold.Models;
using FluentFold.Services;
using FluentFold.ViewModels;

namespace FluentFold.Views;

public sealed partial class OrganizerPage : Page
{
    public OrganizerViewModel ViewModel { get; set; } = null!;

    private Compositor? _compositor;
    private SpringVector3NaturalMotionAnimation? _springAnimation;
    private bool _selectFolderTipShown;
    private bool _organizeTipShown;

    public OrganizerPage()
    {
        try
        {
            ViewModel = App.Services.GetRequiredService<OrganizerViewModel>();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[OrganizerPage] DI failed: {ex}");
            throw;
        }
        InitializeComponent();
        Loaded += OnLoaded;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        Loaded -= OnLoaded;
        try
        {
            _compositor = CompositionTarget.GetCompositorForCurrentThread();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[OrganizerPage] Compositor init failed: {ex}");
        }
        ViewModel.RefreshMode();
        ViewModel.PropertyChanged += OnViewModelPropertyChanged;
        _ = ShowSelectFolderTeachingTipAsync();
    }

    private void OnViewModelPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(ViewModel.HasFolder) && ViewModel.HasFolder)
        {
            _ = ShowOrganizeTeachingTipAsync();
        }
    }

    private async Task ShowSelectFolderTeachingTipAsync()
    {
        if (_selectFolderTipShown) return;
        var settings = App.Services.GetRequiredService<IAppSettingsService>();
        if (!settings.ShowTeachingTips) return;

        _selectFolderTipShown = true;
        await Task.Delay(800);

        if (ViewModel.IsStandardMode && StandardSelectFolderButton.IsLoaded)
        {
            StandardSelectFolderTip.IsOpen = true;
        }
        else if (ViewModel.IsProMode && ProSelectFolderButton.IsLoaded)
        {
            ProSelectFolderTip.IsOpen = true;
        }
    }

    private async Task ShowOrganizeTeachingTipAsync()
    {
        if (_organizeTipShown) return;
        var settings = App.Services.GetRequiredService<IAppSettingsService>();
        if (!settings.ShowTeachingTips) return;

        _organizeTipShown = true;
        await Task.Delay(600);

        if (ViewModel.IsStandardMode && HeroOrganizeButton.IsLoaded)
        {
            HeroOrganizeTip.IsOpen = true;
        }
        else if (ViewModel.IsProMode && OrganizeButton.IsLoaded)
        {
            ProOrganizeTip.IsOpen = true;
        }
    }

    private void OnRemoveRuleClick(object sender, RoutedEventArgs e)
    {
        if (sender is Button button && button.DataContext is ExtensionRule rule)
        {
            ViewModel.RemoveCustomRuleCommand.Execute(rule);
        }
    }

    private void OnRemoveTriggerRuleClick(object sender, RoutedEventArgs e)
    {
        if (sender is Button button && button.DataContext is RuleModel rule)
        {
            ViewModel.RemoveTriggerRuleCommand.Execute(rule);
        }
    }

    private async void OnCleanupActionClick(object sender, object e)
    {
        var dialog = new CleanupReviewDialog
        {
            XamlRoot = XamlRoot,
            DataContext = ViewModel
        };
        await dialog.ShowAsync();
    }

    private void Button_PointerEntered(object sender, PointerRoutedEventArgs e)
    {
        CreateOrUpdateSpringAnimation(1.08f);
        (sender as UIElement)?.StartAnimation(_springAnimation);
    }

    private void Button_PointerExited(object sender, PointerRoutedEventArgs e)
    {
        CreateOrUpdateSpringAnimation(1.0f);
        (sender as UIElement)?.StartAnimation(_springAnimation);
    }

    private void Button_PointerPressed(object sender, PointerRoutedEventArgs e)
    {
        CreateOrUpdateSpringAnimation(0.95f);
        (sender as UIElement)?.StartAnimation(_springAnimation);
    }

    private void Button_PointerReleased(object sender, PointerRoutedEventArgs e)
    {
        CreateOrUpdateSpringAnimation(1.0f);
        (sender as UIElement)?.StartAnimation(_springAnimation);
    }

    private void CreateOrUpdateSpringAnimation(float finalValue)
    {
        if (_compositor is null) return;
        _springAnimation ??= _compositor.CreateSpringVector3Animation();
        _springAnimation.Target = "Scale";
        _springAnimation.FinalValue = new Vector3(finalValue);
    }
}
