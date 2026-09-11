namespace GastosCompartidos.Models;

/// <summary>
/// Ingreso percibido por una persona en una fecha, de un tipo determinado.
/// </summary>
public class Income
{
    public int Id { get; set; }

    public DateTime Date { get; set; } = DateTime.Today;

    public int PersonId { get; set; }
    public Person? Person { get; set; }

    public int IncomeTypeId { get; set; }
    public IncomeType? IncomeType { get; set; }

    public decimal Amount { get; set; }

    public string? Description { get; set; }
}
