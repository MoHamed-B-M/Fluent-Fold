using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Composition;
using System.Numerics;
using FluentFold.Models;
using FluentFold.ViewModels;

namespace FluentFold.Views;

public sealed partial class OrganizerPage : Page
{
    public OrganizerViewModel ViewModel { get; set; } = null!;

    private readonly Compositor _compositor;
    private SpringVector3NaturalMotionAnimation? _springAnimation;

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
        _compositor = CompositionTarget.GetCompositorForCurrentThread();
        Loaded += (_, _) => ViewModel.RefreshMode();
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
        if (_springAnimation == null)
        {
            _springAnimation = _compositor.CreateSpringVector3Animation();
            _springAnimation.Target = "Scale";
        }

        _springAnimation.FinalValue = new Vector3(finalValue);
    }
}
