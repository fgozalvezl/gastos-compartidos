using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GastosCompartidos.Data;
using GastosCompartidos.Helpers;
using GastosCompartidos.Models;
using GastosCompartidos.Services;
using Microsoft.EntityFrameworkCore;

namespace GastosCompartidos.ViewModels;

public partial class PeopleViewModel : ObservableObject
{
    private readonly IDbContextFactory<AppDbContext> _factory;
    private readonly IDialogService _dialog;

    public ObservableCollection<Person> People { get; } = new();

    [ObservableProperty] private Person? _selectedPerson;
    [ObservableProperty] private bool _isLoading;

    public int PeopleCount => People.Count;

    public PeopleViewModel(IDbContextFactory<AppDbContext> factory, IDialogService dialog)
    {
        _factory = factory;
        _dialog = dialog;

        // Al cambiar moneda o decimales hay que reconstruir las tarjetas ya renderizadas.
        CurrencyFormatter.FormatChanged += (_, _) => RefreshFormat();
    }

    public async Task LoadAsync()
    {
        IsLoading = true;
        try
        {
            await using var db = await _factory.CreateDbContextAsync();
            var list = await db.People.AsNoTracking().OrderBy(p => p.Name).ToListAsync();
            People.Clear();
            foreach (var p in list) People.Add(p);
            OnPropertyChanged(nameof(PeopleCount));
        }
        catch (Exception ex)
        {
            _dialog.Error("Error al cargar", $"No se pudieron leer las personas.\n\nDetalle: {ex.Message}");
        }
        finally
        {
            IsLoading = false;
        }
    }

    private void RefreshFormat()
    {
        var snapshot = People.ToList();
        People.Clear();
        foreach (var p in snapshot) People.Add(p);
    }

    [RelayCommand]
    private async Task Add()
    {
        var vm = new PersonEditViewModel(null);
        if (!_dialog.ShowPersonEditor(vm)) return;

        await using var db = await _factory.CreateDbContextAsync();
        db.People.Add(new Person
        {
            Name = vm.Name.Trim(),
            MonthlyIncome = vm.MonthlyIncome,
            ColorHex = vm.ColorHex,
            IsActive = vm.IsActive
        });
        await db.SaveChangesAsync();
        await LoadAsync();
    }

    [RelayCommand]
    private async Task Edit(Person? person)
    {
        person ??= SelectedPerson;
        if (person is null) return;

        var vm = new PersonEditViewModel(person);
        if (!_dialog.ShowPersonEditor(vm)) return;

        await using var db = await _factory.CreateDbContextAsync();
        var entity = await db.People.FindAsync(person.Id);
        if (entity is null) return;

        entity.Name = vm.Name.Trim();
        entity.MonthlyIncome = vm.MonthlyIncome;
        entity.ColorHex = vm.ColorHex;
        entity.IsActive = vm.IsActive;
        await db.SaveChangesAsync();
        await LoadAsync();
    }

    [RelayCommand]
    private async Task Delete(Person? person)
    {
        person ??= SelectedPerson;
        if (person is null) return;

        await using var db = await _factory.CreateDbContextAsync();
        bool referenced = await db.Expenses.AnyAsync(e => e.PaidByPersonId == person.Id)
                          || await db.ExpenseShares.AnyAsync(s => s.PersonId == person.Id);

        if (referenced)
        {
            bool deactivate = _dialog.Confirm("No se puede eliminar",
                $"{person.Name} tiene gastos asociados, por eso no se puede eliminar.\n\n¿Querés desactivarla? Dejará de aparecer como participante en nuevos gastos.");
            if (!deactivate) return;

            var entity = await db.People.FindAsync(person.Id);
            if (entity is not null) { entity.IsActive = false; await db.SaveChangesAsync(); }
            await LoadAsync();
            return;
        }

        if (!_dialog.Confirm("Eliminar persona",
                $"¿Eliminar a {person.Name}? Esta acción no se puede deshacer.")) return;

        var toRemove = await db.People.FindAsync(person.Id);
        if (toRemove is not null) { db.People.Remove(toRemove); await db.SaveChangesAsync(); }
        await LoadAsync();
    }
}
