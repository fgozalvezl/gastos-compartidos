using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GastosCompartidos.Data;
using GastosCompartidos.Helpers;
using GastosCompartidos.Models;
using GastosCompartidos.Services;
using Microsoft.EntityFrameworkCore;

namespace GastosCompartidos.ViewModels;

public partial class IncomesViewModel : ObservableObject
{
    private readonly IDbContextFactory<AppDbContext> _factory;
    private readonly IDialogService _dialog;

    private readonly List<Income> _all = new();
    private List<Person> _people = new();
    private List<IncomeType> _types = new();

    public ObservableCollection<Income> Incomes { get; } = new();

    [ObservableProperty] private Income? _selectedIncome;
    [ObservableProperty] private bool _isLoading;
    [ObservableProperty] private string _searchText = "";
    [ObservableProperty] private DateTime? _from;
    [ObservableProperty] private DateTime? _to;
    [ObservableProperty] private decimal _total;
    [ObservableProperty] private int _resultCount;

    public IncomesViewModel(IDbContextFactory<AppDbContext> factory, IDialogService dialog)
    {
        _factory = factory;
        _dialog = dialog;

        // Al cambiar moneda o decimales hay que reconstruir las filas ya renderizadas.
        CurrencyFormatter.FormatChanged += (_, _) => { OnPropertyChanged(string.Empty); ApplyFilter(); };
    }

    partial void OnSearchTextChanged(string value) => ApplyFilter();
    partial void OnFromChanged(DateTime? value) => ApplyFilter();
    partial void OnToChanged(DateTime? value) => ApplyFilter();

    public async Task LoadAsync()
    {
        IsLoading = true;
        try
        {
            await using var db = await _factory.CreateDbContextAsync();
            _people = await db.People.AsNoTracking().OrderBy(p => p.Name).ToListAsync();
            _types = await db.IncomeTypes.AsNoTracking().OrderBy(t => t.Name).ToListAsync();

            var list = await db.Incomes.AsNoTracking()
                .Include(i => i.Person)
                .Include(i => i.IncomeType)
                .OrderByDescending(i => i.Date)
                .ToListAsync();

            _all.Clear();
            _all.AddRange(list);
            ApplyFilter();
        }
        catch (Exception ex)
        {
            _dialog.Error("Error al cargar", $"No se pudieron leer los ingresos.\n\nDetalle: {ex.Message}");
        }
        finally
        {
            IsLoading = false;
        }
    }

    private void ApplyFilter()
    {
        IEnumerable<Income> q = _all;
        if (From is { } f) q = q.Where(i => i.Date.Date >= f.Date);
        if (To is { } t) q = q.Where(i => i.Date.Date <= t.Date);

        string search = SearchText?.Trim() ?? "";
        if (search.Length > 0)
            q = q.Where(i =>
                (i.Description ?? "").Contains(search, StringComparison.OrdinalIgnoreCase) ||
                (i.Person?.Name ?? "").Contains(search, StringComparison.OrdinalIgnoreCase) ||
                (i.IncomeType?.Name ?? "").Contains(search, StringComparison.OrdinalIgnoreCase));

        var result = q.ToList();
        Incomes.Clear();
        foreach (var i in result) Incomes.Add(i);
        Total = result.Sum(i => i.Amount);
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

        var vm = new IncomeEditViewModel(null, _people, _types);
        if (!_dialog.ShowIncomeEditor(vm)) return;

        await using var db = await _factory.CreateDbContextAsync();
        db.Incomes.Add(new Income
        {
            Date = vm.Date,
            PersonId = vm.SelectedPerson!.Id,
            IncomeTypeId = vm.SelectedType!.Id,
            Amount = vm.Amount,
            Description = string.IsNullOrWhiteSpace(vm.Description) ? null : vm.Description!.Trim()
        });
        await db.SaveChangesAsync();
        await LoadAsync();
    }

    [RelayCommand]
    private async Task Edit(Income? income)
    {
        income ??= SelectedIncome;
        if (income is null) return;

        var vm = new IncomeEditViewModel(income, _people, _types);
        if (!_dialog.ShowIncomeEditor(vm)) return;

        await using var db = await _factory.CreateDbContextAsync();
        var entity = await db.Incomes.FindAsync(income.Id);
        if (entity is null) return;

        entity.Date = vm.Date;
        entity.PersonId = vm.SelectedPerson!.Id;
        entity.IncomeTypeId = vm.SelectedType!.Id;
        entity.Amount = vm.Amount;
        entity.Description = string.IsNullOrWhiteSpace(vm.Description) ? null : vm.Description!.Trim();
        await db.SaveChangesAsync();
        await LoadAsync();
    }

    [RelayCommand]
    private async Task Delete(Income? income)
    {
        income ??= SelectedIncome;
        if (income is null) return;
        if (!_dialog.Confirm("Eliminar ingreso", "¿Eliminar este ingreso?")) return;

        await using var db = await _factory.CreateDbContextAsync();
        var entity = await db.Incomes.FindAsync(income.Id);
        if (entity is not null) { db.Incomes.Remove(entity); await db.SaveChangesAsync(); }
        await LoadAsync();
    }

    [RelayCommand]
    private void ClearFilters()
    {
        SearchText = "";
        From = null;
        To = null;
    }
}
