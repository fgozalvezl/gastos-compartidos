namespace GastosCompartidos.Models;

/// <summary>
/// Un gasto registrado: quién lo pagó, cuánto, de qué categoría y cómo se reparte.
/// </summary>
public class Expense
{
    public int Id { get; set; }

    public DateTime Date { get; set; } = DateTime.Today;

    public string Description { get; set; } = string.Empty;

    public decimal Amount { get; set; }

    public int CategoryId { get; set; }
    public Category? Category { get; set; }

    /// <summary>Persona que efectivamente pagó el gasto.</summary>
    public int PaidByPersonId { get; set; }
    public Person? PaidBy { get; set; }

    public SplitType SplitType { get; set; } = SplitType.Equal;

    public string? Notes { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.Now;

    /// <summary>Cuánto le corresponde pagar a cada participante de este gasto.</summary>
    public ICollection<ExpenseShare> Shares { get; set; } = new List<ExpenseShare>();
}
