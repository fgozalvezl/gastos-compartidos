using System.Windows.Controls;
using GastosCompartidos.ViewModels;

namespace GastosCompartidos.Views.Pages;

public partial class CategoriesPage : Page
{
    private readonly CategoriesViewModel _vm;

    public CategoriesPage(CategoriesViewModel vm)
    {
        _vm = vm;
        InitializeComponent();
        DataContext = vm;
        Loaded += async (_, _) => await _vm.LoadAsync();
    }
}
