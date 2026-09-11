using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace GastosCompartidos.Helpers;

/// <summary>
/// Normalización de texto para la categorización: pasa a minúsculas,
/// quita acentos y separa en palabras (tokens).
/// </summary>
public static partial class TextNormalizer
{
    [GeneratedRegex(@"[^a-z0-9 ]+")]
    private static partial Regex NonWordRegex();

    [GeneratedRegex(@"\s+")]
    private static partial Regex SpacesRegex();

    /// <summary>Minúsculas, sin acentos, sin signos. Conserva los espacios.</summary>
    public static string Normalize(string? input)
    {
        if (string.IsNullOrWhiteSpace(input)) return string.Empty;

        string lower = input.Trim().ToLowerInvariant();
        string decomposed = lower.Normalize(NormalizationForm.FormD);

        var sb = new StringBuilder(decomposed.Length);
        foreach (char c in decomposed)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark)
                sb.Append(c);
        }

        string noAccents = sb.ToString().Normalize(NormalizationForm.FormC);
        string cleaned = NonWordRegex().Replace(noAccents, " ");
        return SpacesRegex().Replace(cleaned, " ").Trim();
    }

    /// <summary>Separa el texto normalizado en palabras únicas.</summary>
    public static string[] Tokenize(string? input)
    {
        string norm = Normalize(input);
        if (norm.Length == 0) return Array.Empty<string>();
        return norm.Split(' ', StringSplitOptions.RemoveEmptyEntries);
    }
}
