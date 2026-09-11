using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;
using GastosCompartidos.Helpers;
using GastosCompartidos.Models;

namespace GastosCompartidos.Converters;

/// <summary>decimal/numérico -> texto de moneda con el símbolo configurado.</summary>
public class CurrencyConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        decimal d = value switch
        {
            decimal m => m,
            double db => (decimal)db,
            int i => i,
            long l => l,
            _ => 0m
        };
        return CurrencyFormatter.Format(d);
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => Binding.DoNothing;
}

/// <summary>Como CurrencyConverter pero con signo (+/-), para balances.</summary>
public class SignedCurrencyConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        decimal d = value is decimal m ? m : value is double db ? (decimal)db : 0m;
        return CurrencyFormatter.FormatSigned(d);
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => Binding.DoNothing;
}

/// <summary>
/// decimal -> pincel semántico: positivo / negativo / cero.
/// Resuelve los pinceles del diccionario de recursos activo (los tokens de WPF-UI ya
/// traen un valor por tema y cumplen 4,5:1 en claro y en oscuro), en vez de congelar
/// hexadecimales que sólo funcionan en un tema.
/// </summary>
public class BalanceToBrushConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        decimal d = value is decimal m ? m : value is double db ? (decimal)db : 0m;
        return d > 0 ? Resolve("SystemFillColorSuccessBrush", "#0F7B0F")
             : d < 0 ? Resolve("SystemFillColorCriticalBrush", "#C42B1C")
                     : Resolve("TextFillColorSecondaryBrush", "#616161");
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => Binding.DoNothing;

    /// <summary>Busca el pincel por clave en los recursos de la app; si no está, usa el valor claro.</summary>
    private static Brush Resolve(string key, string fallbackHex)
    {
        if (Application.Current?.TryFindResource(key) is Brush b) return b;
        var f = new SolidColorBrush((Color)ColorConverter.ConvertFromString(fallbackHex));
        f.Freeze();
        return f;
    }
}

/// <summary>double (ancho disponible) -> true si supera el umbral pasado en ConverterParameter.</summary>
public class WidthOverConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        double w = value is double d ? d : 0;
        double threshold = double.TryParse(parameter as string, NumberStyles.Float, CultureInfo.InvariantCulture, out var t) ? t : 0;
        return w >= threshold;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => Binding.DoNothing;
}

/// <summary>Cadena hexadecimal (#RRGGBB) -> SolidColorBrush.</summary>
public class HexToBrushConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        string hex = value as string ?? "#6B7280";
        try
        {
            var brush = new SolidColorBrush((Color)ColorConverter.ConvertFromString(hex));
            brush.Freeze();
            return brush;
        }
        catch
        {
            return Brushes.Gray;
        }
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is SolidColorBrush b) return b.Color.ToString();
        return Binding.DoNothing;
    }
}

/// <summary>SplitType -> nombre en español.</summary>
public class SplitTypeConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value is SplitType t ? Name(t) : "";

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => Binding.DoNothing;

    public static string Name(SplitType t) => t switch
    {
        SplitType.Equal => "Partes iguales",
        SplitType.Proportional => "Proporcional al ingreso",
        SplitType.Custom => "Personalizada",
        _ => t.ToString()
    };
}

/// <summary>Para enlazar RadioButtons a una propiedad enum (ConverterParameter = nombre del valor).</summary>
public class EnumToBooleanConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value?.ToString() == parameter?.ToString();

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is true && parameter is string name && targetType.IsEnum)
            return Enum.Parse(targetType, name);
        return Binding.DoNothing;
    }
}

/// <summary>int (cantidad) -> Visible si &gt; 0, si no Collapsed.</summary>
public class CountToVisibilityConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => (value is int n && n > 0) ? Visibility.Visible : Visibility.Collapsed;

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => Binding.DoNothing;
}

/// <summary>int (cantidad) -> Visible si == 0 (estado vacío), si no Collapsed.</summary>
public class ZeroToVisibilityConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => (value is int n && n == 0) ? Visibility.Visible : Visibility.Collapsed;

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => Binding.DoNothing;
}

/// <summary>bool -> Visible/Collapsed invertido.</summary>
public class InverseBoolToVisibilityConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => (value is bool b && b) ? Visibility.Collapsed : Visibility.Visible;

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => Binding.DoNothing;
}

/// <summary>Texto no vacío -> Visible; vacío/nulo -> Collapsed.</summary>
public class NotEmptyToVisibilityConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => string.IsNullOrWhiteSpace(value as string) ? Visibility.Collapsed : Visibility.Visible;

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => Binding.DoNothing;
}

/// <summary>AppThemeMode -> nombre en español.</summary>
public class ThemeNameConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value is AppThemeMode m
            ? m switch
            {
                AppThemeMode.Light => "Claro",
                AppThemeMode.Dark => "Oscuro",
                _ => "Automático (según Windows)"
            }
            : "";

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => Binding.DoNothing;
}

/// <summary>
/// texto &lt;-&gt; número para las cajas de monto con <c>UpdateSourceTrigger=PropertyChanged</c>.
/// Con el convertidor por defecto de WPF, al teclear el separador decimal el texto
/// "1500," se convierte a 1500 y el binding reescribe la caja como "1500": la coma
/// desaparece y los dígitos que siguen quedan como enteros (1500,50 -&gt; 150050).
/// Acá se recuerda el texto que tecleó el usuario y se devuelve mientras el valor
/// no cambie, así el separador sobrevive. Parsea siempre con CurrentCulture.
/// Guarda estado: se declara EN LÍNEA en cada binding, no como recurso compartido.
/// </summary>
public class DecimalTextConverter : IValueConverter
{
    private string? _text;
    private object? _value;

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => _text is not null && Equals(value, _value)
            ? _text
            : System.Convert.ToString(value, CultureInfo.CurrentCulture) ?? "";

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        string text = value as string ?? "";
        bool isDouble = targetType == typeof(double) || targetType == typeof(double?);
        object parsed;

        if (text.Trim().Length == 0)
        {
            // caja vacía = 0; si no, el valor anterior quedaría vivo sin verse.
            parsed = isDouble ? 0d : 0m;
        }
        else if (isDouble)
        {
            if (!double.TryParse(text, NumberStyles.Number, CultureInfo.CurrentCulture, out double d))
                return Binding.DoNothing;
            parsed = d;
        }
        else
        {
            if (!decimal.TryParse(text, NumberStyles.Number, CultureInfo.CurrentCulture, out decimal m))
                return Binding.DoNothing;
            parsed = m;
        }

        _text = text;
        _value = parsed;
        return parsed;
    }
}
