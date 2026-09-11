using System.Globalization;

namespace GastosCompartidos.Helpers;

/// <summary>
/// Formateo de moneda compartido por toda la app. La configuración lo actualiza
/// cuando el usuario cambia el símbolo o la cantidad de decimales, de modo que
/// los conversores de la interfaz pueden usarlo de forma estática.
/// </summary>
public static class CurrencyFormatter
{
    private static string _symbol = "$";
    private static int _decimals = 2;

    /// <summary>Se dispara cuando cambia el símbolo o la cantidad de decimales.</summary>
    public static event EventHandler? FormatChanged;

    public static string Symbol
    {
        get => _symbol;
        set { if (_symbol == value) return; _symbol = value; FormatChanged?.Invoke(null, EventArgs.Empty); }
    }

    public static int Decimals
    {
        get => _decimals;
        set { if (_decimals == value) return; _decimals = value; FormatChanged?.Invoke(null, EventArgs.Empty); }
    }

    public static string Format(decimal value)
    {
        string number = value.ToString("N" + Decimals, CultureInfo.CurrentCulture);
        return $"{Symbol} {number}";
    }

    /// <summary>Como <see cref="Format"/> pero anteponiendo el signo (para balances).</summary>
    public static string FormatSigned(decimal value)
    {
        string sign = value < 0 ? "-" : value > 0 ? "+" : "";
        string number = Math.Abs(value).ToString("N" + Decimals, CultureInfo.CurrentCulture);
        return $"{sign}{Symbol} {number}";
    }
}
