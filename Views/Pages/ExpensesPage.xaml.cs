using System.Windows.Controls;
using System.Windows.Input;
using GastosCompartidos.Models;
using GastosCompartidos.ViewModels;

namespace GastosCompartidos.Views.Pages;

public partial class ExpensesPage : Page
{
    private readonly ExpensesViewModel _vm;

    public ExpensesPage(ExpensesViewModel vm)
    {
        _vm = vm;
        InitializeComponent();
        DataContext = vm;
        Loaded += async (_, _) => await _vm.LoadAsync();
    }

    private void ExpensesGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (_vm.SelectedExpense is Expense expense && _vm.EditCommand.CanExecute(expense))
            _vm.EditCommand.Execute(expense);
    }
}
