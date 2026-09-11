namespace GastosCompartidos.Models;

/// <summary>
/// Parte de un gasto que le corresponde a una persona.
/// La suma de las <see cref="Amount"/> de un gasto es igual al total del gasto.
/// </summary>
public class ExpenseShare
{
    public int Id { get; set; }

    public int ExpenseId { get; set; }
    public Expense? Expense { get; set; }

    public int PersonId { get; set; }
    public Person? Person { get; set; }

    /// <summary>Monto que le corresponde pagar a esta persona en este gasto.</summary>
    public decimal Amount { get; set; }

    /// <summary>
    /// Peso utilizado para el reparto (ingreso en proporcional, valor manual en
    /// personalizado, 1 en partes iguales). Se guarda con fines informativos.
    /// </summary>
    public double Weight { get; set; }
}
