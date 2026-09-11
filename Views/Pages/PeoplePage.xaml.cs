using System.Windows.Controls;
using GastosCompartidos.ViewModels;

namespace GastosCompartidos.Views.Pages;

public partial class PeoplePage : Page
{
    private readonly PeopleViewModel _vm;

    public PeoplePage(PeopleViewModel vm)
    {
        _vm = vm;
        InitializeComponent();
        DataContext = vm;
        Loaded += async (_, _) => await _vm.LoadAsync();
    }
}
