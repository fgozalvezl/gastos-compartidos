using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GastosCompartidos.Data;
using GastosCompartidos.Models;
using GastosCompartidos.Services;
using Microsoft.EntityFrameworkCore;

namespace GastosCompartidos.ViewModels;

/// <summary>Elemento de la lista de categorías con datos de presentación.</summary>
public class CategoryListItem
{
    public required Category Category { get; init; }
    public int Id => Category.Id;
    public string Name => Category.Name;
    public string IconGlyph => string.IsNullOrWhiteSpace(Category.IconGlyph) ? "📦" : Category.IconGlyph;
    public string ColorHex => Category.ColorHex;
    public bool IsSystem => Category.IsSystem;
    public List<string> Keywords { get; init; } = new();
    public int ExpenseCount { get; init; }

    public string KeywordsPreview => Keywords.Count == 0
        ? "Sin palabras clave"
        : string.Join(" · ", Keywords.Take(10)) + (Keywords.Count > 10 ? " …" : "");
}

public partial class CategoriesViewModel : ObservableObject
{
    private readonly IDbContextFactory<AppDbContext> _factory;
    private readonly IDialogService _dialog;

    public ObservableCollection<CategoryListItem> Categories { get; } = new();

    [ObservableProperty] private CategoryListItem? _selectedCategory;
    [ObservableProperty] private bool _isLoading;

    public int CategoryCount => Categories.Count;

    public CategoriesViewModel(IDbContextFactory<AppDbContext> factory, IDialogService dialog)
    {
        _factory = factory;
        _dialog = dialog;
    }

    public async Task LoadAsync()
    {
        IsLoading = true;
        try
        {
            await using var db = await _factory.CreateDbContextAsync();

            var cats = await db.Categories.AsNoTracking()
                .Include(c => c.Keywords)
                .OrderBy(c => c.Name)
                .ToListAsync();

            var counts = await db.Expenses.AsNoTracking()
                .GroupBy(e => e.CategoryId)
                .Select(g => new { CategoryId = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.CategoryId, x => x.Count);

            Categories.Clear();
            foreach (var c in cats)
            {
                Categories.Add(new CategoryListItem
                {
                    Category = c,
                    Keywords = c.Keywords.OrderBy(k => k.Keyword).Select(k => k.Keyword).ToList(),
                    ExpenseCount = counts.GetValueOrDefault(c.Id)
                });
            }
            OnPropertyChanged(nameof(CategoryCount));
        }
        catch (Exception ex)
        {
            _dialog.Error("Error al cargar", $"No se pudieron leer las categorías.\n\nDetalle: {ex.Message}");
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task Add()
    {
        var vm = new CategoryEditViewModel(null);
        if (!_dialog.ShowCategoryEditor(vm)) return;

        await using var db = await _factory.CreateDbContextAsync();
        string name = vm.Name.Trim();
        if (await db.Categories.AnyAsync(c => c.Name == name))
        {
            _dialog.Info("Ya existe", $"Ya hay una categoría llamada \"{name}\".");
            return;
        }

        var category = new Category
        {
            Name = name,
            IconGlyph = vm.IconGlyph,
            ColorHex = vm.ColorHex,
            IsSystem = false,
            Keywords = vm.GetKeywords().Select(k => new CategoryKeyword { Keyword = k, Weight = 5 }).ToList()
        };
        db.Categories.Add(category);
        if (!await TrySaveAsync(db, "No se pudo crear la categoría.")) return;
        await LoadAsync();
    }

    [RelayCommand]
    private async Task Edit(CategoryListItem? item)
    {
        item ??= SelectedCategory;
        if (item is null) return;

        var vm = new CategoryEditViewModel(item.Category, item.Keywords);
        if (!_dialog.ShowCategoryEditor(vm)) return;

        await using var db = await _factory.CreateDbContextAsync();
        var entity = await db.Categories.Include(c => c.Keywords).FirstOrDefaultAsync(c => c.Id == item.Id);
        if (entity is null) return;

        string name = vm.Name.Trim();
        if (await db.Categories.AnyAsync(c => c.Name == name && c.Id != entity.Id))
        {
            _dialog.Info("Ya existe", $"Ya hay una categoría llamada \"{name}\".");
            return;
        }

        entity.Name = name;
        entity.IconGlyph = vm.IconGlyph;
        entity.ColorHex = vm.ColorHex;

        // Reconciliar palabras clave
        var desired = vm.GetKeywords().ToHashSet();
        var toRemove = entity.Keywords.Where(k => !desired.Contains(k.Keyword)).ToList();
        foreach (var k in toRemove) db.CategoryKeywords.Remove(k);

        var existing = entity.Keywords.Select(k => k.Keyword).ToHashSet();
        foreach (var kw in desired.Where(d => !existing.Contains(d)))
            entity.Keywords.Add(new CategoryKeyword { Keyword = kw, Weight = 5 });

        if (!await TrySaveAsync(db, "No se pudo guardar la categoría.")) return;
        await LoadAsync();
    }

    [RelayCommand]
    private async Task Delete(CategoryListItem? item)
    {
        item ??= SelectedCategory;
        if (item is null) return;

        if (item.IsSystem)
        {
            _dialog.Info("Categoría del sistema",
                "Las categorías que vienen por defecto no se pueden eliminar, pero podés editar su nombre, ícono, color y palabras clave.");
            return;
        }

        await using var db = await _factory.CreateDbContextAsync();

        // Se vuelve a consultar acá: ExpenseCount es una foto tomada en LoadAsync.
        int used = await db.Expenses.CountAsync(e => e.CategoryId == item.Id);
        if (used > 0)
        {
            _dialog.Info("Categoría en uso",
                $"\"{item.Name}\" tiene {used} gasto(s) asociados. Reasigná esos gastos a otra categoría antes de eliminarla.");
            return;
        }

        if (!_dialog.Confirm("Eliminar categoría", $"¿Eliminar la categoría \"{item.Name}\"?")) return;

        var entity = await db.Categories.FindAsync(item.Id);
        if (entity is not null)
        {
            db.Categories.Remove(entity);
            if (!await TrySaveAsync(db, $"No se pudo eliminar \"{item.Name}\". Puede que tenga gastos asociados.")) return;
        }
        await LoadAsync();
    }

    /// <summary>Guarda y traduce los errores de base (nombre duplicado, clave foránea en uso).</summary>
    private async Task<bool> TrySaveAsync(AppDbContext db, string message)
    {
        try
        {
            await db.SaveChangesAsync();
            return true;
        }
        catch (DbUpdateException)
        {
            _dialog.Error("No se pudo guardar", message);
            return false;
        }
    }
}
