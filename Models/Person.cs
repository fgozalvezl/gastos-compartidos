namespace GastosCompartidos.Models;

/// <summary>
/// Persona que participa en los gastos compartidos.
/// </summary>
public class Person
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Ingreso mensual de referencia. Se usa como base para la división proporcional.
    /// </summary>
    public decimal MonthlyIncome { get; set; }

    /// <summary>Color (hex) para identificar a la persona en gráficos y listas.</summary>
    public string ColorHex { get; set; } = "#2563EB";

    /// <summary>Si está inactiva no aparece como participante por defecto en nuevos gastos.</summary>
    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.Now;

    public ICollection<Expense> ExpensesPaid { get; set; } = new List<Expense>();
    public ICollection<ExpenseShare> Shares { get; set; } = new List<ExpenseShare>();
    public ICollection<Income> Incomes { get; set; } = new List<Income>();
}
