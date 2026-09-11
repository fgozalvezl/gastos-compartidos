namespace GastosCompartidos.Models;

/// <summary>
/// Tipo de ingreso (Sueldo, Freelance, Aguinaldo, Alquiler, etc.).
/// </summary>
public class IncomeType
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public bool IsSystem { get; set; }

    public ICollection<Income> Incomes { get; set; } = new List<Income>();
}
