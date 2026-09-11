using System.Windows.Controls;
using GastosCompartidos.ViewModels;

namespace GastosCompartidos.Views.Pages;

public partial class ReportsPage : Page
{
    private readonly ReportsViewModel _vm;

    public ReportsPage(ReportsViewModel vm)
    {
        _vm = vm;
        InitializeComponent();
        DataContext = vm;
        Loaded += async (_, _) => await _vm.LoadAsync();
    }
}
