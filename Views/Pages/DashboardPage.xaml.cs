using System.Windows.Controls;
using GastosCompartidos.ViewModels;

namespace GastosCompartidos.Views.Pages;

public partial class DashboardPage : Page
{
    private readonly DashboardViewModel _vm;

    public DashboardPage(DashboardViewModel vm)
    {
        _vm = vm;
        InitializeComponent();
        DataContext = vm;
        Loaded += async (_, _) => await _vm.LoadAsync();
    }
}
