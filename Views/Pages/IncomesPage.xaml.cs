using System.Windows.Controls;
using System.Windows.Input;
using GastosCompartidos.Models;
using GastosCompartidos.ViewModels;

namespace GastosCompartidos.Views.Pages;

public partial class IncomesPage : Page
{
    private readonly IncomesViewModel _vm;

    public IncomesPage(IncomesViewModel vm)
    {
        _vm = vm;
        InitializeComponent();
        DataContext = vm;
        Loaded += async (_, _) => await _vm.LoadAsync();
    }

    private void IncomesGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (_vm.SelectedIncome is Income income && _vm.EditCommand.CanExecute(income))
            _vm.EditCommand.Execute(income);
    }
}
