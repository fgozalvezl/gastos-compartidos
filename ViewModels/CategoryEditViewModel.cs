using CommunityToolkit.Mvvm.ComponentModel;
using GastosCompartidos.Helpers;
using GastosCompartidos.Models;

namespace GastosCompartidos.ViewModels;

/// <summary>Edición/alta de una categoría y sus palabras clave.</summary>
public partial class CategoryEditViewModel : ObservableObject
{
    public int Id { get; }
    public string Title { get; }
    public bool IsSystem { get; }
    public string[] ColorOptions => Palette.Colors;
    public string[] EmojiOptions => Palette.Emojis;

    [ObservableProperty] private string _name = "";
    [ObservableProperty] private string _iconGlyph = "📦";
    [ObservableProperty] private string _colorHex = "#6B7280";
    [ObservableProperty] private string _keywordsText = "";
    [ObservableProperty] private string? _error;

    public CategoryEditViewModel(Category? existing, IEnumerable<string>? keywords = null)
    {
        if (existing is null)
        {
            Title = "Nueva categoría";
            ColorHex = Palette.Colors[0];
        }
        else
        {
            Id = existing.Id;
            Title = "Editar categoría";
            IsSystem = existing.IsSystem;
            Name = existing.Name;
            IconGlyph = string.IsNullOrWhiteSpace(existing.IconGlyph) ? "📦" : existing.IconGlyph;
            ColorHex = existing.ColorHex;
            KeywordsText = keywords is null ? "" : string.Join(", ", keywords);
        }
    }

    public bool Validate()
    {
        if (string.IsNullOrWhiteSpace(Name)) { Error = "Ingresá un nombre."; return false; }
        Error = null;
        return true;
    }

    /// <summary>Palabras clave normalizadas (minúsculas, sin acentos, sin duplicados).</summary>
    public List<string> GetKeywords()
        => KeywordsText
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(TextNormalizer.Normalize)
            .Where(k => k.Length > 0)
            .Distinct()
            .ToList();
}
