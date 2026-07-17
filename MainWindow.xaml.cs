using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using FluentFold.ViewModels;

namespace FluentFold;

public sealed partial class MainWindow : Window
{
    public MainViewModel ViewModel { get; }

    public MainWindow()
    {
        ViewModel = new MainViewModel();
        InitializeComponent();

        ExtendsContentIntoTitleBar = true;
        SetTitleBar(AppTitleBar);

        AppWindow.SetIcon("Assets/AppIcon.ico");

        ViewModel.Initialize(ContentFrame);
    }

    private void OnItemInvoked(NavigationView sender, NavigationViewItemInvokedEventArgs args)
    {
        if (args.InvokedItemContainer?.Tag is string tag)
        {
            ViewModel.NavigateToPageCommand.Execute(tag);
        }
    }
}
