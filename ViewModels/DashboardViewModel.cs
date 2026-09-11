using System.Collections.ObjectModel;
using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GastosCompartidos.Data;
using GastosCompartidos.Helpers;
using GastosCompartidos.Models;
using GastosCompartidos.Services;
using LiveChartsCore;
using LiveChartsCore.Painting;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Painting;
using Microsoft.EntityFrameworkCore;
using SkiaSharp;
using Wpf.Ui.Appearance;

namespace GastosCompartidos.ViewModels;

public partial class DashboardViewModel : ObservableObject
{
    private static readonly CultureInfo Es = new("es-ES");

    private readonly IDbContextFactory<AppDbContext> _factory;
    private readonly ISettlementService _settlement;
    private readonly IDialogService _dialog;

    /// <summary>Etiquetas del gráfico de columnas (para poder rehacer los ejes al cambiar el tema).</summary>
    private string[] _monthLabels = Array.Empty<string>();

    public ObservableCollection<PersonBalance> Balances { get; } = new();
    public ObservableCollection<SettlementTransfer> Settlement { get; } = new();

    [ObservableProperty] private ISeries[] _categorySeries = Array.Empty<ISeries>();
    [ObservableProperty] private ISeries[] _monthlySeries = Array.Empty<ISeries>();
    [ObservableProperty] private Axis[] _monthlyXAxes = Array.Empty<Axis>();
    [ObservableProperty] private Axis[] _monthlyYAxes = Array.Empty<Axis>();

    /// <summary>Pintura del texto de leyenda y tooltip de los gráficos (sigue el tema).</summary>
    [ObservableProperty] private Paint? _chartTextPaint;

    /// <summary>Fondo de leyenda y tooltip de los gráficos (sigue el tema).</summary>
    [ObservableProperty] private Paint? _chartSurfacePaint;

    [ObservableProperty] private decimal _totalExpensesMonth;
    [ObservableProperty] private decimal _totalExpensesAll;
    [ObservableProperty] private decimal _totalIncomeMonth;
    [ObservableProperty] private int _expenseCount;
    [ObservableProperty] private bool _hasData;
    [ObservableProperty] private bool _hasCategoryData;
    [ObservableProperty] private string _pieScopeLabel = "Este mes";
    [ObservableProperty] private string _currentMonthName = "";
    [ObservableProperty] private bool _isLoading;

    public bool HasSettlement => Settlement.Count > 0;

    public DashboardViewModel(IDbContextFactory<AppDbContext> factory, ISettlementService settlement, IDialogService dialog)
    {
        _factory = factory;
        _settlement = settlement;
        _dialog = dialog;

        // Al cambiar moneda o decimales hay que refrescar los importes ya renderizados.
        CurrencyFormatter.FormatChanged += (_, _) => RefreshFormat();

        // Los ejes se pintan según el tema activo: hay que rehacerlos si cambia en caliente.
        ApplicationThemeManager.Changed += (_, _) => BuildAxes();
    }

    [RelayCommand]
    private Task Refresh() => LoadAsync();

    public async Task LoadAsync()
    {
        IsLoading = true;
        try
        {
            await LoadCoreAsync();
        }
        catch (Exception ex)
        {
            _dialog.Error("Error al cargar", $"No se pudo leer la información del panel.\n\nDetalle: {ex.Message}");
        }
        finally
        {
            IsLoading = false;
        }
    }

    private async Task LoadCoreAsync()
    {
        await using var db = await _factory.CreateDbContextAsync();

        var people = await db.People.AsNoTracking().ToListAsync();
        var expenses = await db.Expenses.AsNoTracking()
            .Include(e => e.Category)
            .Include(e => e.PaidBy)
            .Include(e => e.Shares)
            .ToListAsync();
        var incomes = await db.Incomes.AsNoTracking().ToListAsync();

        var today = DateTime.Today;
        var monthStart = new DateTime(today.Year, today.Month, 1);
        var monthEnd = monthStart.AddMonths(1); // cota superior: el mes en curso, sin fechas futuras
        string monthName = monthStart.ToString("MMMM yyyy", Es);
        CurrentMonthName = char.ToUpper(monthName[0]) + monthName[1..];

        HasData = expenses.Count > 0;
        ExpenseCount = expenses.Count;
        TotalExpensesAll = expenses.Sum(e => e.Amount);
        TotalExpensesMonth = expenses.Where(e => e.Date >= monthStart && e.Date < monthEnd).Sum(e => e.Amount);
        TotalIncomeMonth = incomes.Where(i => i.Date >= monthStart && i.Date < monthEnd).Sum(i => i.Amount);

        // Balances y liquidación (histórico completo)
        var balances = _settlement.CalculateBalances(people, expenses);
        Balances.Clear();
        foreach (var b in balances.OrderByDescending(b => b.Balance)) Balances.Add(b);

        Settlement.Clear();
        foreach (var t in _settlement.CalculateSettlement(balances)) Settlement.Add(t);
        OnPropertyChanged(nameof(HasSettlement));

        // Torta por categoría (mes actual; si está vacío, histórico)
        var scope = expenses.Where(e => e.Date >= monthStart && e.Date < monthEnd).ToList();
        PieScopeLabel = "Este mes";
        if (scope.Count == 0 && expenses.Count > 0)
        {
            scope = expenses;
            PieScopeLabel = "Histórico";
        }

        var byCat = scope
            .GroupBy(e => e.Category)
            .Select(g => new { Cat = g.Key, Total = g.Sum(e => e.Amount) })
            .Where(x => x.Total > 0)
            .OrderByDescending(x => x.Total)
            .ToList();

        CategorySeries = byCat.Select(x => (ISeries)new PieSeries<double>
        {
            Values = new[] { (double)x.Total },
            Name = x.Cat?.Name ?? "Sin categoría",
            Fill = new SolidColorPaint(SKColor.Parse(string.IsNullOrWhiteSpace(x.Cat?.ColorHex) ? "#6B7280" : x.Cat!.ColorHex)),
            Stroke = null
        }).ToArray();
        HasCategoryData = byCat.Count > 0;

        // Columnas: últimos 6 meses
        var months = Enumerable.Range(0, 6).Select(i => monthStart.AddMonths(-5 + i)).ToList();
        var values = months
            .Select(m => (double)expenses.Where(e => e.Date >= m && e.Date < m.AddMonths(1)).Sum(e => e.Amount))
            .ToArray();
        _monthLabels = months.Select(m =>
        {
            string mn = m.ToString("MMM", Es);
            return char.ToUpper(mn[0]) + mn[1..].TrimEnd('.');
        }).ToArray();

        MonthlySeries = new ISeries[]
        {
            new ColumnSeries<double>
            {
                Name = "Gastos",
                Values = values,
                Fill = ColumnPaint(),
                Rx = 5,
                Ry = 5
            }
        };
        BuildAxes();
    }

    /// <summary>
    /// Color de la serie de columnas según el tema: el azul oscuro sólo se lee sobre fondo
    /// claro (5,12:1 sobre la tarjeta clara, 2,74:1 sobre la oscura); en tema oscuro se usa
    /// un azul más claro (5,57:1 sobre #2B2B2B).
    /// </summary>
    private static SolidColorPaint ColumnPaint()
        => new(ApplicationThemeManager.GetAppTheme() == ApplicationTheme.Dark
            ? new SKColor(0x60, 0xA5, 0xFA)
            : new SKColor(0x25, 0x63, 0xEB));

    /// <summary>Ejes y pinturas de leyenda/tooltip del gráfico, derivados del tema activo.</summary>
    private void BuildAxes()
    {
        bool dark = ApplicationThemeManager.GetAppTheme() == ApplicationTheme.Dark;
        SKColor labelColor = dark ? new SKColor(255, 255, 255, 222) : new SKColor(30, 41, 59);
        SKColor separatorColor = dark ? new SKColor(255, 255, 255, 38) : new SKColor(0, 0, 0, 20);
        SKColor surfaceColor = dark ? new SKColor(43, 43, 43) : new SKColor(255, 255, 255);

        // Leyenda de la torta y tooltips: sin esto LiveCharts usa gris oscuro sobre fondo oscuro.
        ChartTextPaint = new SolidColorPaint(labelColor);
        ChartSurfacePaint = new SolidColorPaint(surfaceColor);

        // La serie ya creada también tiene que repintarse al cambiar el tema en caliente.
        foreach (var s in MonthlySeries.OfType<ColumnSeries<double>>()) s.Fill = ColumnPaint();

        MonthlyXAxes = new[]
        {
            new Axis
            {
                Labels = _monthLabels,
                TextSize = 12,
                LabelsPaint = new SolidColorPaint(labelColor),
                SeparatorsPaint = null
            }
        };
        MonthlyYAxes = new[]
        {
            new Axis
            {
                Labeler = v => CurrencyFormatter.Symbol + " " + v.ToString("N0", CultureInfo.CurrentCulture),
                TextSize = 12,
                LabelsPaint = new SolidColorPaint(labelColor),
                SeparatorsPaint = new SolidColorPaint(separatorColor)
            }
        };
    }

    /// <summary>Rehace los importes visibles cuando cambia el formato de moneda.</summary>
    private void RefreshFormat()
    {
        OnPropertyChanged(string.Empty);

        var balances = Balances.ToList();
        Balances.Clear();
        foreach (var b in balances) Balances.Add(b);

        var transfers = Settlement.ToList();
        Settlement.Clear();
        foreach (var t in transfers) Settlement.Add(t);

        BuildAxes(); // el rótulo del eje Y lleva el símbolo de moneda
    }
}
