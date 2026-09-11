using ClosedXML.Excel;
using GastosCompartidos.Models;

namespace GastosCompartidos.Services;

/// <summary>Conjunto de datos (ya filtrados por período) que alimenta el informe de Excel.</summary>
public record ReportData(
    string Title,
    DateTime? From,
    DateTime? To,
    string CurrencySymbol,
    int Decimals,
    IReadOnlyList<Person> People,
    IReadOnlyList<Expense> Expenses,
    IReadOnlyList<Income> Incomes,
    IReadOnlyList<PersonBalance> Balances,
    IReadOnlyList<SettlementTransfer> Settlement);

public interface IExcelExportService
{
    void Export(string filePath, ReportData data);
}

/// <summary>
/// Genera un libro de Excel con varias hojas usando ClosedXML (no requiere
/// tener Office instalado).
/// </summary>
public class ExcelExportService : IExcelExportService
{
    private static readonly XLColor HeaderBg = XLColor.FromHtml("#2563EB");
    private static readonly XLColor HeaderFg = XLColor.FromHtml("#FFFFFF");
    private static readonly XLColor TotalBg = XLColor.FromHtml("#E5EDFF");
    private static readonly XLColor TitleColor = XLColor.FromHtml("#1E293B");

    private string _moneyFormat = "\"$\" #,##0.00";

    public void Export(string filePath, ReportData data)
    {
        _moneyFormat = BuildMoneyFormat(data.CurrencySymbol, data.Decimals);

        using var wb = new XLWorkbook();
        wb.Properties.Title = data.Title;
        wb.Properties.Author = "Gastos Compartidos";

        BuildSummary(wb, data);
        BuildExpenseDetail(wb, data);
        BuildByCategory(wb, data);
        BuildIncomes(wb, data);

        wb.SaveAs(filePath);
    }

    private static string BuildMoneyFormat(string symbol, int decimals)
    {
        string digits = decimals > 0 ? "0." + new string('0', decimals) : "0";
        return $"\"{symbol}\" #,##{digits}";
    }

    // ---------------------------------------------------------------- Resumen
    private void BuildSummary(XLWorkbook wb, ReportData data)
    {
        var ws = wb.Worksheets.Add("Resumen");
        ws.ShowGridLines = false;
        int row = 1;

        ws.Cell(row, 1).Value = data.Title;
        ws.Cell(row, 1).Style.Font.Bold = true;
        ws.Cell(row, 1).Style.Font.FontSize = 18;
        ws.Cell(row, 1).Style.Font.FontColor = TitleColor;
        row++;

        ws.Cell(row, 1).Value = PeriodText(data);
        ws.Cell(row, 1).Style.Font.FontColor = XLColor.FromHtml("#64748B");
        row += 2;

        // Indicadores
        decimal totalExpenses = data.Expenses.Sum(e => e.Amount);
        decimal totalIncome = data.Incomes.Sum(i => i.Amount);

        WriteKpi(ws, ref row, "Total de gastos", totalExpenses);
        WriteKpi(ws, ref row, "Cantidad de gastos", data.Expenses.Count, isMoney: false);
        WriteKpi(ws, ref row, "Total de ingresos", totalIncome);
        row++;

        // Balance por persona
        ws.Cell(row, 1).Value = "Balance por persona";
        ws.Cell(row, 1).Style.Font.Bold = true;
        ws.Cell(row, 1).Style.Font.FontSize = 13;
        row++;

        string[] headers = { "Persona", "Pagó", "Le corresponde", "Balance" };
        WriteHeaderRow(ws, row, headers);
        int balanceHeaderRow = row;
        row++;

        foreach (var b in data.Balances)
        {
            ws.Cell(row, 1).Value = b.Name;
            SetMoney(ws.Cell(row, 2), b.Paid);
            SetMoney(ws.Cell(row, 3), b.Owed);
            SetMoney(ws.Cell(row, 4), b.Balance);
            ws.Cell(row, 4).Style.Font.FontColor =
                b.Balance >= 0 ? XLColor.FromHtml("#16A34A") : XLColor.FromHtml("#DC2626");
            row++;
        }
        ws.Range(balanceHeaderRow, 1, row - 1, 4).Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
        row += 2;

        // Liquidación sugerida
        ws.Cell(row, 1).Value = "Liquidación sugerida (quién le paga a quién)";
        ws.Cell(row, 1).Style.Font.Bold = true;
        ws.Cell(row, 1).Style.Font.FontSize = 13;
        row++;

        if (data.Settlement.Count == 0)
        {
            ws.Cell(row, 1).Value = "¡Las cuentas están saldadas! 🎉";
            row++;
        }
        else
        {
            foreach (var t in data.Settlement)
            {
                ws.Cell(row, 1).Value = $"{t.FromName}  →  {t.ToName}";
                SetMoney(ws.Cell(row, 2), t.Amount);
                ws.Cell(row, 2).Style.Font.Bold = true;
                row++;
            }
        }

        ws.Columns(1, 4).AdjustToContents();
        ws.Column(1).Width = Math.Max(ws.Column(1).Width, 26);
        ws.SheetView.FreezeRows(1);
    }

    private void WriteKpi(IXLWorksheet ws, ref int row, string label, decimal value, bool isMoney = true)
    {
        ws.Cell(row, 1).Value = label;
        ws.Cell(row, 1).Style.Font.FontColor = XLColor.FromHtml("#64748B");
        var cell = ws.Cell(row, 2);
        if (isMoney) SetMoney(cell, value);
        else cell.Value = (double)value;
        cell.Style.Font.Bold = true;
        row++;
    }

    // -------------------------------------------------------- Detalle de gastos
    private void BuildExpenseDetail(XLWorkbook wb, ReportData data)
    {
        var ws = wb.Worksheets.Add("Gastos");
        ws.ShowGridLines = false;

        var people = data.People;
        var headers = new List<string> { "Fecha", "Descripción", "Categoría", "Pagó", "División", "Monto" };
        headers.AddRange(people.Select(p => $"Parte {p.Name}"));

        int headerRow = 1;
        WriteHeaderRow(ws, headerRow, headers.ToArray());

        int row = headerRow + 1;
        foreach (var e in data.Expenses.OrderBy(e => e.Date).ThenBy(e => e.Id))
        {
            int col = 1;
            ws.Cell(row, col).Value = e.Date;
            ws.Cell(row, col).Style.NumberFormat.Format = "dd/mm/yyyy";
            col++;
            ws.Cell(row, col++).Value = e.Description;
            ws.Cell(row, col++).Value = e.Category?.Name ?? "";
            ws.Cell(row, col++).Value = e.PaidBy?.Name ?? "";
            ws.Cell(row, col++).Value = SplitTypeName(e.SplitType);
            SetMoney(ws.Cell(row, col++), e.Amount);

            foreach (var p in people)
            {
                decimal share = e.Shares.FirstOrDefault(s => s.PersonId == p.Id)?.Amount ?? 0m;
                SetMoney(ws.Cell(row, col++), share);
            }
            row++;
        }

        // Fila de totales
        if (data.Expenses.Count > 0)
        {
            ws.Cell(row, 1).Value = "TOTAL";
            ws.Cell(row, 1).Style.Font.Bold = true;
            SetMoney(ws.Cell(row, 6), data.Expenses.Sum(e => e.Amount));
            int col = 7;
            foreach (var p in people)
            {
                decimal totalShare = data.Expenses
                    .SelectMany(e => e.Shares)
                    .Where(s => s.PersonId == p.Id)
                    .Sum(s => s.Amount);
                SetMoney(ws.Cell(row, col++), totalShare);
            }
            ws.Range(row, 1, row, headers.Count).Style.Fill.BackgroundColor = TotalBg;
            ws.Range(row, 1, row, headers.Count).Style.Font.Bold = true;
        }

        ws.Columns(1, headers.Count).AdjustToContents();
        ws.Column(2).Width = Math.Min(Math.Max(ws.Column(2).Width, 24), 45);
        ws.SheetView.FreezeRows(1);
    }

    // ----------------------------------------------------------- Por categoría
    private void BuildByCategory(XLWorkbook wb, ReportData data)
    {
        var ws = wb.Worksheets.Add("Por categoría");
        ws.ShowGridLines = false;

        WriteHeaderRow(ws, 1, new[] { "Categoría", "Cantidad", "Total", "% del total" });

        decimal grandTotal = data.Expenses.Sum(e => e.Amount);
        var groups = data.Expenses
            .GroupBy(e => e.Category?.Name ?? "Sin categoría")
            .Select(g => new { Name = g.Key, Count = g.Count(), Total = g.Sum(e => e.Amount) })
            .OrderByDescending(g => g.Total)
            .ToList();

        int row = 2;
        foreach (var g in groups)
        {
            ws.Cell(row, 1).Value = g.Name;
            ws.Cell(row, 2).Value = g.Count;
            SetMoney(ws.Cell(row, 3), g.Total);
            double pct = grandTotal > 0 ? (double)(g.Total / grandTotal) : 0;
            ws.Cell(row, 4).Value = pct;
            ws.Cell(row, 4).Style.NumberFormat.Format = "0.0%";
            row++;
        }

        if (groups.Count > 0)
        {
            ws.Cell(row, 1).Value = "TOTAL";
            ws.Cell(row, 2).Value = groups.Sum(g => g.Count);
            SetMoney(ws.Cell(row, 3), grandTotal);
            ws.Cell(row, 4).Value = 1.0;
            ws.Cell(row, 4).Style.NumberFormat.Format = "0.0%";
            ws.Range(row, 1, row, 4).Style.Fill.BackgroundColor = TotalBg;
            ws.Range(row, 1, row, 4).Style.Font.Bold = true;
        }

        ws.Columns(1, 4).AdjustToContents();
        ws.Column(1).Width = Math.Max(ws.Column(1).Width, 22);
        ws.SheetView.FreezeRows(1);
    }

    // --------------------------------------------------------------- Ingresos
    private void BuildIncomes(XLWorkbook wb, ReportData data)
    {
        var ws = wb.Worksheets.Add("Ingresos");
        ws.ShowGridLines = false;

        WriteHeaderRow(ws, 1, new[] { "Fecha", "Persona", "Tipo", "Monto", "Descripción" });

        int row = 2;
        foreach (var i in data.Incomes.OrderBy(i => i.Date))
        {
            ws.Cell(row, 1).Value = i.Date;
            ws.Cell(row, 1).Style.NumberFormat.Format = "dd/mm/yyyy";
            ws.Cell(row, 2).Value = i.Person?.Name ?? "";
            ws.Cell(row, 3).Value = i.IncomeType?.Name ?? "";
            SetMoney(ws.Cell(row, 4), i.Amount);
            ws.Cell(row, 5).Value = i.Description ?? "";
            row++;
        }

        if (data.Incomes.Count > 0)
        {
            ws.Cell(row, 1).Value = "TOTAL";
            ws.Cell(row, 1).Style.Font.Bold = true;
            SetMoney(ws.Cell(row, 4), data.Incomes.Sum(i => i.Amount));
            ws.Range(row, 1, row, 5).Style.Fill.BackgroundColor = TotalBg;
            ws.Range(row, 1, row, 5).Style.Font.Bold = true;
        }

        ws.Columns(1, 5).AdjustToContents();
        ws.Column(5).Width = Math.Max(ws.Column(5).Width, 24);
        ws.SheetView.FreezeRows(1);
    }

    // ----------------------------------------------------------------- Helpers
    private void SetMoney(IXLCell cell, decimal value)
    {
        cell.Value = (double)value;
        cell.Style.NumberFormat.Format = _moneyFormat;
        cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;
    }

    private static void WriteHeaderRow(IXLWorksheet ws, int row, string[] headers)
    {
        for (int i = 0; i < headers.Length; i++)
        {
            var cell = ws.Cell(row, i + 1);
            cell.Value = headers[i];
            cell.Style.Font.Bold = true;
            cell.Style.Font.FontColor = HeaderFg;
            cell.Style.Fill.BackgroundColor = HeaderBg;
            cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
        }
    }

    private static string PeriodText(ReportData data)
    {
        if (data.From is null && data.To is null) return "Período: todos los registros";
        string from = data.From?.ToString("dd/MM/yyyy") ?? "inicio";
        string to = data.To?.ToString("dd/MM/yyyy") ?? "hoy";
        return $"Período: {from} – {to}";
    }

    private static string SplitTypeName(SplitType type) => type switch
    {
        SplitType.Equal => "Partes iguales",
        SplitType.Proportional => "Proporcional",
        SplitType.Custom => "Personalizada",
        _ => type.ToString()
    };
}
