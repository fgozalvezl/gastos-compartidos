namespace GastosCompartidos.Models;

/// <summary>
/// Forma en la que se reparte el monto de un gasto entre los participantes.
/// </summary>
public enum SplitType
{
    /// <summary>Todos pagan lo mismo.</summary>
    Equal = 0,

    /// <summary>Cada uno paga en proporción a su ingreso de referencia.</summary>
    Proportional = 1,

    /// <summary>Reparto manual con pesos/porcentajes definidos por el usuario.</summary>
    Custom = 2
}

/// <summary>Tema visual de la aplicación.</summary>
public enum AppThemeMode
{
    System = 0,
    Light = 1,
    Dark = 2
}
