using GastosCompartidos.Views.Pages;
using Wpf.Ui.Controls;

namespace GastosCompartidos;

public partial class MainWindow : FluentWindow
{
    public MainWindow(IServiceProvider services)
    {
        InitializeComponent();
        RootNavigation.SetServiceProvider(services);
        Loaded += (_, _) => RootNavigation.Navigate(typeof(DashboardPage));
    }
}
