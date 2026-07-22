using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Animation;
using FluentFold.Services;

namespace FluentFold.Views;

public sealed partial class OnboardingDialog : ContentDialog
{
    private int _currentSlide;
    private readonly IFirstLaunchService _firstLaunch;

    public OnboardingDialog()
    {
        _firstLaunch = App.Services.GetRequiredService<IFirstLaunchService>();
        InitializeComponent();
    }

    protected override void OnApplyTemplate()
    {
        base.OnApplyTemplate();
        UpdateNavigation();
    }

    private void OnBackClick(object sender, RoutedEventArgs e)
    {
        if (_currentSlide <= 0) return;
        GoToSlide(_currentSlide - 1);
    }

    private void OnNextClick(object sender, RoutedEventArgs e)
    {
        if (_currentSlide >= 3)
        {
            _firstLaunch.MarkCompleted();
            Hide();
            return;
        }
        GoToSlide(_currentSlide + 1);
    }

    private void GoToSlide(int index)
    {
        var slides = new Panel?[] { Slide0, Slide1, Slide2, Slide3 };

        if (slides[_currentSlide] is not null)
            slides[_currentSlide]!.Visibility = Visibility.Collapsed;

        if (slides[index] is Panel nextPanel)
        {
            nextPanel.ChildrenTransitions = new TransitionCollection
            {
                new EntranceThemeTransition
                {
                    FromHorizontalOffset = index > _currentSlide ? 60 : -60
                }
            };
            nextPanel.Visibility = Visibility.Visible;
        }

        _currentSlide = index;
        UpdateNavigation();
    }

    private void UpdateNavigation()
    {
        BackButton.Visibility = _currentSlide == 0 ? Visibility.Collapsed : Visibility.Visible;
        NextButton.Content = _currentSlide == 3 ? "\uE8FB" : "\uE76C";
        NextButton.FontSize = 18;
        NextButton.Width = 40;
        ToolTipService.SetToolTip(NextButton, _currentSlide == 3 ? "Get Started" : "Next");
        UpdateDots();
    }

    private void UpdateDots()
    {
        var accent = GetResource("SystemAccentColor", Microsoft.UI.Colors.DodgerBlue);
        var muted = GetResource("TextFillColorTertiaryBrush", Microsoft.UI.Colors.Gray);

        Dot0.Fill = _currentSlide == 0 ? accent : muted;
        Dot1.Fill = _currentSlide == 1 ? accent : muted;
        Dot2.Fill = _currentSlide == 2 ? accent : muted;
        Dot3.Fill = _currentSlide == 3 ? accent : muted;
    }

    private static Brush GetResource(string key, Windows.UI.Color fallback)
    {
        try
        {
            return (Brush)Application.Current.Resources[key];
        }
        catch
        {
            return new SolidColorBrush(fallback);
        }
    }
}
