using System.Windows.Controls;
using GastosCompartidos.ViewModels;

namespace GastosCompartidos.Views.Pages;

public partial class SettingsPage : Page
{
    private readonly SettingsViewModel _vm;

    public SettingsPage(SettingsViewModel vm)
    {
        _vm = vm;
        InitializeComponent();
        DataContext = vm;
        Loaded += async (_, _) => await _vm.LoadAsync();
    }
}
