using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GastosCompartidos.Data;
using GastosCompartidos.Models;
using GastosCompartidos.Services;
using Microsoft.EntityFrameworkCore;

namespace GastosCompartidos.ViewModels;

public partial class SettingsViewModel : ObservableObject
{
    private readonly ISettingsService _settings;
    private readonly IDbContextFactory<AppDbContext> _factory;
    private readonly IDialogService _dialog;
    private bool _initializing;

    public ObservableCollection<IncomeType> IncomeTypes { get; } = new();
    public AppThemeMode[] ThemeOptions { get; } = { AppThemeMode.System, AppThemeMode.Light, AppThemeMode.Dark };
    public SplitType[] SplitOptions { get; } = { SplitType.Equal, SplitType.Proportional, SplitType.Custom };
    public int[] DecimalOptions { get; } = { 0, 1, 2, 3, 4 };

    [ObservableProperty] private string _currencySymbol = "$";
    [ObservableProperty] private int _decimals = 2;
    [ObservableProperty] private AppThemeMode _theme = AppThemeMode.System;
    [ObservableProperty] private SplitType _defaultSplitType = SplitType.Equal;
    [ObservableProperty] private string _newIncomeTypeName = "";
    [ObservableProperty] private string _dataFolderPath = "";

    public SettingsViewModel(ISettingsService settings, IDbContextFactory<AppDbContext> factory, IDialogService dialog)
    {
        _settings = settings;
        _factory = factory;
        _dialog = dialog;
    }

    public async Task LoadAsync()
    {
        _initializing = true;
        CurrencySymbol = _settings.CurrencySymbol;
        Decimals = _settings.Decimals;
        Theme = _settings.Theme;
        DefaultSplitType = _settings.DefaultSplitType;
        DataFolderPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "GastosCompartidos");
        _initializing = false;

        try
        {
            await using var db = await _factory.CreateDbContextAsync();
            var types = await db.IncomeTypes.AsNoTracking().OrderBy(t => t.Name).ToListAsync();
            IncomeTypes.Clear();
            foreach (var t in types) IncomeTypes.Add(t);
        }
        catch (Exception ex)
        {
            _dialog.Error("Error al cargar", $"No se pudo leer la configuración.\n\nDetalle: {ex.Message}");
        }
    }

    partial void OnCurrencySymbolChanged(string value) { if (!_initializing) _settings.CurrencySymbol = value; }
    partial void OnDecimalsChanged(int value) { if (!_initializing) _settings.Decimals = value; }
    partial void OnDefaultSplitTypeChanged(SplitType value) { if (!_initializing) _settings.DefaultSplitType = value; }
    partial void OnThemeChanged(AppThemeMode value)
    {
        if (_initializing) return;
        _settings.Theme = value;
        App.ApplyTheme(value);
    }

    [RelayCommand]
    private async Task AddIncomeType()
    {
        string name = NewIncomeTypeName?.Trim() ?? "";
        if (name.Length == 0) return;

        await using var db = await _factory.CreateDbContextAsync();
        if (await db.IncomeTypes.AnyAsync(t => t.Name == name))
        {
            _dialog.Info("Ya existe", "Ese tipo de ingreso ya existe.");
            return;
        }
        db.IncomeTypes.Add(new IncomeType { Name = name, IsSystem = false });
        try
        {
            await db.SaveChangesAsync();
        }
        catch (DbUpdateException)
        {
            // Red de seguridad: el índice único puede fallar igual (p. ej. otra ventana).
            _dialog.Error("No se pudo guardar", $"No se pudo crear el tipo de ingreso \"{name}\".");
            return;
        }
        NewIncomeTypeName = "";
        await LoadAsync();
    }

    [RelayCommand]
    private async Task DeleteIncomeType(IncomeType? type)
    {
        if (type is null) return;

        await using var db = await _factory.CreateDbContextAsync();
        if (await db.Incomes.AnyAsync(i => i.IncomeTypeId == type.Id))
        {
            _dialog.Info("Tipo en uso", $"\"{type.Name}\" tiene ingresos asociados y no se puede eliminar.");
            return;
        }
        var entity = await db.IncomeTypes.FindAsync(type.Id);
        if (entity is not null)
        {
            db.IncomeTypes.Remove(entity);
            try
            {
                await db.SaveChangesAsync();
            }
            catch (DbUpdateException)
            {
                _dialog.Error("No se pudo eliminar",
                    $"No se pudo eliminar \"{type.Name}\". Puede que tenga ingresos asociados.");
                return;
            }
        }
        await LoadAsync();
    }

    [RelayCommand]
    private void OpenDataFolder()
    {
        try
        {
            Directory.CreateDirectory(DataFolderPath);
            Process.Start(new ProcessStartInfo(DataFolderPath) { UseShellExecute = true });
        }
        catch { /* ignorar */ }
    }

    [RelayCommand]
    private async Task ResetData()
    {
        if (!_dialog.Confirm("Borrar todos los datos",
                "Esto eliminará TODAS las personas, gastos e ingresos.\nLas categorías y la configuración se conservan.\n\n¿Continuar?"))
            return;
        if (!_dialog.Confirm("Confirmación final", "¿Estás seguro? Esta acción no se puede deshacer."))
            return;

        await using var db = await _factory.CreateDbContextAsync();
        db.ExpenseShares.RemoveRange(db.ExpenseShares);
        db.Expenses.RemoveRange(db.Expenses);
        db.Incomes.RemoveRange(db.Incomes);
        db.People.RemoveRange(db.People);
        await db.SaveChangesAsync();

        _dialog.Info("Listo", "Se borraron los datos. Las pantallas se actualizarán al navegar entre secciones.");
    }
}
