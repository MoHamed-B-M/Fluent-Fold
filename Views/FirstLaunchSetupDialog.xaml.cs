using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;

namespace FluentFold.Views;

public sealed partial class FirstLaunchSetupDialog : ContentDialog
{
    public string? SelectedMode { get; private set; }

    public FirstLaunchSetupDialog()
    {
        InitializeComponent();
        IsPrimaryButtonEnabled = false;
    }

    private void StandardCard_Tapped(object sender, TappedRoutedEventArgs e)
    {
        SelectedMode = "Standard";
        IsPrimaryButtonEnabled = true;
        StandardCard.BorderBrush = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.DodgerBlue);
        ProCard.BorderBrush = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.Transparent);
    }

    private void ProCard_Tapped(object sender, TappedRoutedEventArgs e)
    {
        SelectedMode = "Pro";
        IsPrimaryButtonEnabled = true;
        ProCard.BorderBrush = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.DodgerBlue);
        StandardCard.BorderBrush = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.Transparent);
    }

    private void OnPrimaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
    {
        if (SelectedMode is null)
            args.Cancel = true;
    }
}
