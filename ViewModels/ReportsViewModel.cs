using System.Diagnostics;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GastosCompartidos.Data;
using GastosCompartidos.Helpers;
using GastosCompartidos.Models;
using GastosCompartidos.Services;
using Microsoft.EntityFrameworkCore;

namespace GastosCompartidos.ViewModels;

public partial class ReportsViewModel : ObservableObject
{
    private readonly IDbContextFactory<AppDbContext> _factory;
    private readonly IExcelExportService _excel;
    private readonly ISettlementService _settlement;
    private readonly IDialogService _dialog;

    [ObservableProperty] private DateTime? _from;
    [ObservableProperty] private DateTime? _to;
    [ObservableProperty] private decimal _totalExpenses;
    [ObservableProperty] private decimal _totalIncome;
    [ObservableProperty] private int _expenseCount;
    [ObservableProperty] private int _incomeCount;
    [ObservableProperty] private string _periodLabel = "";
    [ObservableProperty] private bool _isBusy;
    [ObservableProperty] private bool _isLoading;

    public ReportsViewModel(
        IDbContextFactory<AppDbContext> factory,
        IExcelExportService excel,
        ISettlementService settlement,
        IDialogService dialog)
    {
        _factory = factory;
        _excel = excel;
        _settlement = settlement;
        _dialog = dialog;

        var today = DateTime.Today;
        _from = new DateTime(today.Year, today.Month, 1);
        _to = today;
        UpdatePeriodLabel();

        // Al cambiar moneda o decimales hay que refrescar los importes ya renderizados.
        CurrencyFormatter.FormatChanged += (_, _) => OnPropertyChanged(string.Empty);
    }

    public async Task LoadAsync()
    {
        IsLoading = true;
        try
        {
            await UpdatePreviewAsync();
        }
        finally
        {
            IsLoading = false;
        }
    }

    partial void OnFromChanged(DateTime? value) { UpdatePeriodLabel(); _ = UpdatePreviewAsync(); }
    partial void OnToChanged(DateTime? value) { UpdatePeriodLabel(); _ = UpdatePreviewAsync(); }

    private void UpdatePeriodLabel()
    {
        if (From is null && To is null) { PeriodLabel = "Todos los registros"; return; }
        string f = From?.ToString("dd/MM/yyyy") ?? "inicio";
        string t = To?.ToString("dd/MM/yyyy") ?? "hoy";
        PeriodLabel = $"{f}  –  {t}";
    }

    private async Task UpdatePreviewAsync()
    {
        try
        {
            await using var db = await _factory.CreateDbContextAsync();
            var (expenses, incomes) = await LoadFilteredAsync(db);
            TotalExpenses = expenses.Sum(e => e.Amount);
            TotalIncome = incomes.Sum(i => i.Amount);
            ExpenseCount = expenses.Count;
            IncomeCount = incomes.Count;
        }
        catch (Exception ex)
        {
            _dialog.Error("Error al cargar", $"No se pudieron leer los datos del informe.\n\nDetalle: {ex.Message}");
        }
    }

    private async Task<(List<Expense> expenses, List<Income> incomes)> LoadFilteredAsync(AppDbContext db)
    {
        var expenses = await db.Expenses.AsNoTracking()
            .Include(e => e.Category)
            .Include(e => e.PaidBy)
            .Include(e => e.Shares)
            .ToListAsync();
        var incomes = await db.Incomes.AsNoTracking()
            .Include(i => i.Person)
            .Include(i => i.IncomeType)
            .ToListAsync();

        if (From is { } f)
        {
            expenses = expenses.Where(e => e.Date.Date >= f.Date).ToList();
            incomes = incomes.Where(i => i.Date.Date >= f.Date).ToList();
        }
        if (To is { } t)
        {
            expenses = expenses.Where(e => e.Date.Date <= t.Date).ToList();
            incomes = incomes.Where(i => i.Date.Date <= t.Date).ToList();
        }
        return (expenses, incomes);
    }

    [RelayCommand]
    private void Preset(string which)
    {
        var today = DateTime.Today;
        switch (which)
        {
            case "month":
                From = new DateTime(today.Year, today.Month, 1);
                To = today;
                break;
            case "lastmonth":
                var lm = new DateTime(today.Year, today.Month, 1).AddMonths(-1);
                From = lm;
                To = lm.AddMonths(1).AddDays(-1);
                break;
            case "year":
                From = new DateTime(today.Year, 1, 1);
                To = today;
                break;
            case "all":
                From = null;
                To = null;
                break;
        }
    }

    [RelayCommand]
    private async Task Export()
    {
        if (IsBusy) return;
        IsBusy = true;
        try
        {
            await using var db = await _factory.CreateDbContextAsync();
            var people = await db.People.AsNoTracking().OrderBy(p => p.Name).ToListAsync();
            var (expenses, incomes) = await LoadFilteredAsync(db);

            if (expenses.Count == 0 && incomes.Count == 0)
            {
                _dialog.Info("Sin datos", "No hay gastos ni ingresos en el período seleccionado.");
                return;
            }

            var balances = _settlement.CalculateBalances(people, expenses);
            var settlement = _settlement.CalculateSettlement(balances);

            string suggested = $"Gastos Compartidos {DateTime.Now:yyyy-MM-dd}.xlsx";
            string? path = _dialog.SaveFile(suggested, "Libro de Excel (*.xlsx)|*.xlsx");
            if (path is null) return;

            var data = new ReportData(
                "Informe de Gastos Compartidos",
                From, To,
                CurrencyFormatter.Symbol,
                CurrencyFormatter.Decimals,
                people, expenses, incomes,
                balances, settlement);

            await Task.Run(() => _excel.Export(path, data));

            if (_dialog.Confirm("Informe exportado",
                    $"El informe se guardó en:\n{path}\n\n¿Querés abrirlo ahora?"))
            {
                Process.Start(new ProcessStartInfo(path) { UseShellExecute = true });
            }
        }
        catch (Exception ex)
        {
            _dialog.Error("Error al exportar",
                $"No se pudo generar el archivo. ¿Está abierto en Excel?\n\nDetalle: {ex.Message}");
        }
        finally
        {
            IsBusy = false;
        }
    }
}
