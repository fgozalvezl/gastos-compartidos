using System.Windows;
using GastosCompartidos.ViewModels;
using Wpf.Ui.Controls;

namespace GastosCompartidos.Views.Dialogs;

public partial class CategoryEditWindow : FluentWindow
{
    public CategoryEditWindow() => InitializeComponent();

    private void Save_Click(object sender, RoutedEventArgs e)
    {
        if (DataContext is CategoryEditViewModel vm && vm.Validate())
            DialogResult = true;
    }

    private void Cancel_Click(object sender, RoutedEventArgs e) => DialogResult = false;
}
