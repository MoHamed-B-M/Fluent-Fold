using Microsoft.UI.Xaml;
using FluentFold.Views;

namespace FluentFold;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        Title = "Fluent Fold";
        ExtendsContentIntoTitleBar = true;

        var rootFrame = new Microsoft.UI.Xaml.Controls.Frame();
        Content = rootFrame;
        rootFrame.Navigate(typeof(MainPage));
    }
}