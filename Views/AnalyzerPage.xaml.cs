using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Hosting;
using Microsoft.UI.Composition;
using System.Numerics;
using FluentFold.ViewModels;

namespace FluentFold.Views;

public sealed partial class AnalyzerPage : Page
{
    public AnalyzerViewModel ViewModel { get; set; } = null!;

    private readonly Compositor _compositor;
    private SpringVector3NaturalMotionAnimation? _springAnimation;
    private DropShadow? _glowShadow;
    private ColorKeyFrameAnimation? _glowAnimation;

    public AnalyzerPage()
    {
        try
        {
            ViewModel = App.Services.GetRequiredService<AnalyzerViewModel>();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[AnalyzerPage] DI failed: {ex}");
            throw;
        }
        InitializeComponent();
        _compositor = CompositionTarget.GetCompositorForCurrentThread();
        Loaded += OnPageLoaded;
    }

    private void OnPageLoaded(object sender, RoutedEventArgs e)
    {
        SetupGlowEffect();
        StartGlowAnimation();
    }

    private void SetupGlowEffect()
    {
        _glowShadow = _compositor.CreateDropShadow();
        _glowShadow.BlurRadius = 40;
        _glowShadow.Color = Microsoft.UI.Colors.DodgerBlue;
        _glowShadow.Offset = new Vector3(0, 0, 0);

        var shadowVisual = _compositor.CreateSpriteVisual();
        shadowVisual.Size = new Vector2(200, 200);
        shadowVisual.Shadow = _glowShadow;

        ElementCompositionPreview.SetElementChildVisual(GlowEllipse, shadowVisual);
    }

    private void StartGlowAnimation()
    {
        if (_glowShadow is null) return;
        _glowAnimation = _compositor.CreateColorKeyFrameAnimation();
        _glowAnimation.Duration = TimeSpan.FromSeconds(3);
        _glowAnimation.IterationBehavior = AnimationIterationBehavior.Forever;

        _glowAnimation.InsertKeyFrame(0.0f, Microsoft.UI.Colors.DodgerBlue);
        _glowAnimation.InsertKeyFrame(0.33f, Microsoft.UI.Colors.MediumPurple);
        _glowAnimation.InsertKeyFrame(0.66f, Microsoft.UI.Colors.HotPink);
        _glowAnimation.InsertKeyFrame(1.0f, Microsoft.UI.Colors.DodgerBlue);

        _glowShadow.StartAnimation("Color", _glowAnimation);
    }

    private void ScanButton_PointerEntered(object sender, PointerRoutedEventArgs e)
    {
        CreateOrUpdateSpringAnimation(1.08f);
        (sender as UIElement)?.StartAnimation(_springAnimation);
    }

    private void ScanButton_PointerExited(object sender, PointerRoutedEventArgs e)
    {
        CreateOrUpdateSpringAnimation(1.0f);
        (sender as UIElement)?.StartAnimation(_springAnimation);
    }

    private void ScanButton_PointerPressed(object sender, PointerRoutedEventArgs e)
    {
        CreateOrUpdateSpringAnimation(0.95f);
        (sender as UIElement)?.StartAnimation(_springAnimation);
    }

    private void ScanButton_PointerReleased(object sender, PointerRoutedEventArgs e)
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
