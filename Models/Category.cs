namespace GastosCompartidos.Models;

/// <summary>
/// Categoría de gasto (Supermercado, Alquiler, Transporte, etc.).
/// </summary>
public class Category
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    /// <summary>Glifo de Segoe Fluent Icons que representa la categoría.</summary>
    public string IconGlyph { get; set; } = "";

    /// <summary>Color (hex) usado en el gráfico de torta y las etiquetas.</summary>
    public string ColorHex { get; set; } = "#6366F1";

    /// <summary>Categorías del sistema (creadas por defecto). No se pueden borrar, solo editar.</summary>
    public bool IsSystem { get; set; }

    public ICollection<Expense> Expenses { get; set; } = new List<Expense>();
    public ICollection<CategoryKeyword> Keywords { get; set; } = new List<CategoryKeyword>();
}
