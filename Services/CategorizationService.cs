using GastosCompartidos.Data;
using GastosCompartidos.Helpers;
using GastosCompartidos.Models;
using Microsoft.EntityFrameworkCore;

namespace GastosCompartidos.Services;

public interface ICategorizationService
{
    /// <summary>Sugiere el Id de categoría más probable según la descripción, o null si no hay coincidencias.</summary>
    Task<int?> SuggestCategoryIdAsync(string description, CancellationToken ct = default);

    /// <summary>Aprende: asocia las palabras de la descripción a la categoría elegida por el usuario.</summary>
    Task LearnAsync(string description, int categoryId, CancellationToken ct = default);
}

/// <summary>
/// Categorización por palabras clave. Suma el peso de cada palabra que aparece
/// en la descripción y devuelve la categoría con mayor puntaje. Aprende de las
/// confirmaciones del usuario reforzando (o creando) las palabras clave.
/// </summary>
public class CategorizationService : ICategorizationService
{
    private readonly IDbContextFactory<AppDbContext> _factory;

    private const int MaxWeight = 60;
    private const int LearnIncrement = 3;
    private const int MinLearnLength = 4;

    private static readonly HashSet<string> StopWords = new(StringComparer.Ordinal)
    {
        "para", "por", "con", "del", "los", "las", "una", "uno", "unos", "unas",
        "que", "como", "este", "esta", "esto", "esos", "esas", "mas", "muy", "pero",
        "the", "and", "mensual", "anual", "pago", "pagos", "cuota", "compra", "gasto",
        "enero", "febrero", "marzo", "abril", "mayo", "junio", "julio", "agosto",
        "septiembre", "setiembre", "octubre", "noviembre", "diciembre",
    };

    public CategorizationService(IDbContextFactory<AppDbContext> factory) => _factory = factory;

    public async Task<int?> SuggestCategoryIdAsync(string description, CancellationToken ct = default)
    {
        string normalized = TextNormalizer.Normalize(description);
        if (normalized.Length == 0) return null;

        var tokens = normalized.Split(' ', StringSplitOptions.RemoveEmptyEntries).ToHashSet();

        await using var db = await _factory.CreateDbContextAsync(ct);
        var keywords = await db.CategoryKeywords.AsNoTracking().ToListAsync(ct);

        var scores = new Dictionary<int, int>();
        foreach (var kw in keywords)
        {
            if (string.IsNullOrEmpty(kw.Keyword)) continue;

            // Palabras cortas: deben coincidir como token completo (evita falsos positivos).
            // Palabras largas o frases: basta con que aparezcan dentro de la descripción.
            bool matches = kw.Keyword.Length <= 3
                ? tokens.Contains(kw.Keyword)
                : normalized.Contains(kw.Keyword, StringComparison.Ordinal);

            if (matches)
                scores[kw.CategoryId] = scores.GetValueOrDefault(kw.CategoryId) + Math.Max(1, kw.Weight);
        }

        if (scores.Count == 0) return null;

        return scores.OrderByDescending(s => s.Value).First().Key;
    }

    public async Task LearnAsync(string description, int categoryId, CancellationToken ct = default)
    {
        var tokens = TextNormalizer.Tokenize(description)
            .Where(t => t.Length >= MinLearnLength && !StopWords.Contains(t))
            .Distinct()
            .Take(6)
            .ToList();

        if (tokens.Count == 0) return;

        await using var db = await _factory.CreateDbContextAsync(ct);

        foreach (var token in tokens)
        {
            var existing = await db.CategoryKeywords
                .FirstOrDefaultAsync(k => k.CategoryId == categoryId && k.Keyword == token, ct);

            if (existing is null)
            {
                db.CategoryKeywords.Add(new CategoryKeyword
                {
                    CategoryId = categoryId,
                    Keyword = token,
                    Weight = LearnIncrement
                });
            }
            else
            {
                existing.Weight = Math.Min(MaxWeight, existing.Weight + LearnIncrement);
            }
        }

        await db.SaveChangesAsync(ct);
    }
}
