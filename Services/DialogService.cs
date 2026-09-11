using System.Windows;
using GastosCompartidos.ViewModels;
using GastosCompartidos.Views.Dialogs;
using Microsoft.Win32;

namespace GastosCompartidos.Services;

public interface IDialogService
{
    bool ShowExpenseEditor(ExpenseEditViewModel vm);
    bool ShowPersonEditor(PersonEditViewModel vm);
    bool ShowCategoryEditor(CategoryEditViewModel vm);
    bool ShowIncomeEditor(IncomeEditViewModel vm);

    bool Confirm(string title, string message);
    void Info(string title, string message);
    void Error(string title, string message);

    /// <summary>Muestra el diálogo de "Guardar como" y devuelve la ruta elegida (o null).</summary>
    string? SaveFile(string suggestedName, string filter);
}

public class DialogService : IDialogService
{
    private static Window? Owner => Application.Current?.MainWindow;

    public bool ShowExpenseEditor(ExpenseEditViewModel vm) => ShowEditor(new ExpenseEditWindow(), vm);
    public bool ShowPersonEditor(PersonEditViewModel vm) => ShowEditor(new PersonEditWindow(), vm);
    public bool ShowCategoryEditor(CategoryEditViewModel vm) => ShowEditor(new CategoryEditWindow(), vm);
    public bool ShowIncomeEditor(IncomeEditViewModel vm) => ShowEditor(new IncomeEditWindow(), vm);

    private static bool ShowEditor(Window window, object vm)
    {
        window.DataContext = vm;
        if (Owner is not null && !ReferenceEquals(Owner, window))
            window.Owner = Owner;
        return window.ShowDialog() == true;
    }

    public bool Confirm(string title, string message)
        => MessageBox.Show(message, title, MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes;

    public void Info(string title, string message)
        => MessageBox.Show(message, title, MessageBoxButton.OK, MessageBoxImage.Information);

    public void Error(string title, string message)
        => MessageBox.Show(message, title, MessageBoxButton.OK, MessageBoxImage.Error);

    public string? SaveFile(string suggestedName, string filter)
    {
        var dlg = new SaveFileDialog
        {
            FileName = suggestedName,
            Filter = filter,
            AddExtension = true,
            OverwritePrompt = true
        };
        return dlg.ShowDialog() == true ? dlg.FileName : null;
    }
}
