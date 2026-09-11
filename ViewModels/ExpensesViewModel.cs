using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GastosCompartidos.Data;
using GastosCompartidos.Helpers;
using GastosCompartidos.Models;
using GastosCompartidos.Services;
using Microsoft.EntityFrameworkCore;

namespace GastosCompartidos.ViewModels;

public partial class ExpensesViewModel : ObservableObject
{
    private readonly IDbContextFactory<AppDbContext> _factory;
    private readonly IDialogService _dialog;
    private readonly ISplitCalculator _calc;
    private readonly ICategorizationService _categorizer;
    private readonly ISettingsService _settings;

    private readonly List<Expense> _all = new();
    private List<Person> _people = new();
    private List<Category> _categories = new();

    public ObservableCollection<Expense> Expenses { get; } = new();
    public ObservableCollection<Category> CategoryFilters { get; } = new();

    [ObservableProperty] private Expense? _selectedExpense;
    [ObservableProperty] private bool _isLoading;
    [ObservableProperty] private string _searchText = "";
    [ObservableProperty] private DateTime? _from;
    [ObservableProperty] private DateTime? _to;
    [ObservableProperty] private Category? _categoryFilter;
    [ObservableProperty] private decimal _total;
    [ObservableProperty] private int _resultCount;

    public ExpensesViewModel(
        IDbContextFactory<AppDbContext> factory,
        IDialogService dialog,
        ISplitCalculator calc,
        ICategorizationService categorizer,
        ISettingsService settings)
    {
        _factory = factory;
        _dialog = dialog;
        _calc = calc;
        _categorizer = categorizer;
        _settings = settings;

        // Al cambiar moneda o decimales hay que reconstruir las filas ya renderizadas.
        CurrencyFormatter.FormatChanged += (_, _) => { OnPropertyChanged(string.Empty); ApplyFilter(); };
    }

    partial void OnSearchTextChanged(string value) => ApplyFilter();
    partial void OnFromChanged(DateTime? value) => ApplyFilter();
    partial void OnToChanged(DateTime? value) => ApplyFilter();
    partial void OnCategoryFilterChanged(Category? value) => ApplyFilter();

    public async Task LoadAsync()
    {
        IsLoading = true;
        try
        {
            await using var db = await _factory.CreateDbContextAsync();

            _people = await db.People.AsNoTracking().OrderBy(p => p.Name).ToListAsync();
            _categories = await db.Categories.AsNoTracking().OrderBy(c => c.Name).ToListAsync();

            var list = await db.Expenses.AsNoTracking()
                .Include(e => e.Category)
                .Include(e => e.PaidBy)
                .Include(e => e.Shares)
                .OrderByDescending(e => e.Date)
                .ThenByDescending(e => e.Id)
                .ToListAsync();

            _all.Clear();
            _all.AddRange(list);

            CategoryFilters.Clear();
            foreach (var c in _categories) CategoryFilters.Add(c);

            ApplyFilter();
        }
        catch (Exception ex)
        {
            _dialog.Error("Error al cargar", $"No se pudieron leer los gastos.\n\nDetalle: {ex.Message}");
        }
        finally
        {
            IsLoading = false;
        }
    }

    private void ApplyFilter()
    {
        IEnumerable<Expense> q = _all;

        if (From is { } f) q = q.Where(e => e.Date.Date >= f.Date);
        if (To is { } t) q = q.Where(e => e.Date.Date <= t.Date);
        if (CategoryFilter is { } cat) q = q.Where(e => e.CategoryId == cat.Id);

        string search = SearchText?.Trim() ?? "";
        if (search.Length > 0)
            q = q.Where(e =>
                e.Description.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                (e.Category?.Name ?? "").Contains(search, StringComparison.OrdinalIgnoreCase) ||
                (e.PaidBy?.Name ?? "").Contains(search, StringComparison.OrdinalIgnoreCase));

        var result = q.ToList();
        Expenses.Clear();
        foreach (var e in result) Expenses.Add(e);

        Total = result.Sum(e => e.Amount);
        ResultCount = result.Count;
    }

    [RelayCommand]
    private async Task Add()
    {
        if (_people.Count == 0)
        {
            _dialog.Info("Falta una persona", "Primero agregá al menos una persona en la sección Personas.");
            return;
        }

        var vm = new ExpenseEditViewModel(null, _people, _categories, _calc, _categorizer, _settings.DefaultSplitType);
        if (!_dialog.ShowExpenseEditor(vm)) return;

        await using var db = await _factory.CreateDbContextAsync();
        var expense = new Expense
        {
            Date = vm.Date,
            Description = vm.Description.Trim(),
            Amount = vm.Amount,
            CategoryId = vm.SelectedCategory!.Id,
            PaidByPersonId = vm.PaidBy!.Id,
            SplitType = vm.SplitType,
            Notes = string.IsNullOrWhiteSpace(vm.Notes) ? null : vm.Notes!.Trim(),
            CreatedAt = DateTime.Now
        };
        foreach (var s in vm.GetShares())
            expense.Shares.Add(new ExpenseShare { PersonId = s.PersonId, Amount = s.Amount, Weight = s.Weight });

        db.Expenses.Add(expense);
        await db.SaveChangesAsync();

        await _categorizer.LearnAsync(expense.Description, expense.CategoryId);
        await LoadAsync();
    }

    [RelayCommand]
    private async Task Edit(Expense? expense)
    {
        expense ??= SelectedExpense;
        if (expense is null) return;

        // Recargar el gasto con sus partes para el editor.
        await using (var loadDb = await _factory.CreateDbContextAsync())
        {
            expense = await loadDb.Expenses.AsNoTracking()
                .Include(e => e.Shares)
                .FirstOrDefaultAsync(e => e.Id == expense.Id);
        }
        if (expense is null) return;

        var vm = new ExpenseEditViewModel(expense, _people, _categories, _calc, _categorizer, _settings.DefaultSplitType);
        if (!_dialog.ShowExpenseEditor(vm)) return;

        await using var db = await _factory.CreateDbContextAsync();
        var entity = await db.Expenses.Include(e => e.Shares).FirstOrDefaultAsync(e => e.Id == vm.Id);
        if (entity is null) return;

        entity.Date = vm.Date;
        entity.Description = vm.Description.Trim();
        entity.Amount = vm.Amount;
        entity.CategoryId = vm.SelectedCategory!.Id;
        entity.PaidByPersonId = vm.PaidBy!.Id;
        entity.SplitType = vm.SplitType;
        entity.Notes = string.IsNullOrWhiteSpace(vm.Notes) ? null : vm.Notes!.Trim();

        db.ExpenseShares.RemoveRange(entity.Shares);
        foreach (var s in vm.GetShares())
            entity.Shares.Add(new ExpenseShare { PersonId = s.PersonId, Amount = s.Amount, Weight = s.Weight });

        await db.SaveChangesAsync();
        await _categorizer.LearnAsync(entity.Description, entity.CategoryId);
        await LoadAsync();
    }

    [RelayCommand]
    private async Task Delete(Expense? expense)
    {
        expense ??= SelectedExpense;
        if (expense is null) return;

        if (!_dialog.Confirm("Eliminar gasto",
                $"¿Eliminar \"{expense.Description}\"? Esta acción no se puede deshacer.")) return;

        await using var db = await _factory.CreateDbContextAsync();
        var entity = await db.Expenses.FindAsync(expense.Id);
        if (entity is not null) { db.Expenses.Remove(entity); await db.SaveChangesAsync(); }
        await LoadAsync();
    }

    [RelayCommand]
    private void ClearFilters()
    {
        SearchText = "";
        From = null;
        To = null;
        CategoryFilter = null;
    }
}
