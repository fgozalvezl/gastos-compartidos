namespace GastosCompartidos.Models;

/// <summary>
/// Palabra clave asociada a una categoría. Es la base de la categorización
/// "inteligente": cuando la descripción de un gasto contiene la palabra, se
/// sugiere la categoría. El <see cref="Weight"/> crece a medida que el usuario
/// confirma sugerencias, de modo que el sistema aprende de sus correcciones.
/// </summary>
public class CategoryKeyword
{
    public int Id { get; set; }

    /// <summary>Palabra clave normalizada (minúsculas, sin acentos).</summary>
    public string Keyword { get; set; } = string.Empty;

    public int CategoryId { get; set; }
    public Category? Category { get; set; }

    /// <summary>Peso/relevancia. A mayor peso, más fuerte la señal hacia la categoría.</summary>
    public int Weight { get; set; } = 1;
}
