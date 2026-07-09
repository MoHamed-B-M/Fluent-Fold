using FluentFold.ViewModels;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Windows.Storage.Pickers;

namespace FluentFold.Views;

public sealed partial class OrganizerPage : Page
{
    public OrganizerViewModel ViewModel { get; }

    public OrganizerPage()
    {
        ViewModel = App.GetService<OrganizerViewModel>();
        InitializeComponent();
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        // Initial setup if needed
    }

    private async void OnRenameClick(object sender, RoutedEventArgs e)
    {
        if (ViewModel.IsBusy) return;

        var dialog = new ContentDialog
        {
            Title = "Bulk Rename Files",
            PrimaryButtonText = "Rename",
            CloseButtonText = "Cancel",
            DefaultButton = ContentDialogButton.Primary,
            XamlRoot = this.XamlRoot
        };

        var panel = new StackPanel
        {
            Spacing = 16,
            Padding = new Thickness(0, 8, 0, 0)
        };

        var patternBox = new TextBox
        {
            Header = "Name Pattern",
            PlaceholderText = "e.g., photo, document, file",
            Text = "file"
        };

        var numberBox = new NumberBox
        {
            Header = "Starting Number",
            Minimum = 1,
            Maximum = 999999,
            SmallChange = 1,
            LargeChange = 10,
            Value = 1,
            SpinButtonPlacementMode = NumberBoxSpinButtonPlacementMode.Compact
        };

        var preview = new TextBlock
        {
            Text = "Preview: file_001.ext, file_002.ext, ...",
            Opacity = 0.6,
            FontSize = 12,
            Margin = new Thickness(0, -4, 0, 0)
        };

        void UpdatePreview()
        {
            var p = patternBox.Text;
            if (string.IsNullOrWhiteSpace(p)) p = "file";
            var digits = numberBox.Value.ToString().Length + 2;
            if (digits < 3) digits = 3;
            preview.Text = $"Preview: {p}_{"1".PadLeft(digits, '0')}.ext, {p}_{"2".PadLeft(digits, '0')}.ext, ...";
        }

        patternBox.TextChanged += (_, _) => UpdatePreview();
        UpdatePreview();

        panel.Children.Add(patternBox);
        panel.Children.Add(numberBox);
        panel.Children.Add(preview);
        dialog.Content = panel;

        var result = await dialog.ShowAsync();
        if (result == ContentDialogResult.Primary)
        {
            var pattern = patternBox.Text.Trim();
            if (string.IsNullOrWhiteSpace(pattern))
                pattern = "file";
            var startNum = (int)numberBox.Value;
            await ViewModel.RenameFilesWithOptions(pattern, startNum);
        }
    }
}
