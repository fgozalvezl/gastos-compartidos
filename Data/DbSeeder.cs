using GastosCompartidos.Models;

namespace GastosCompartidos.Data;

/// <summary>
/// Carga inicial de datos: categorías por defecto con sus palabras clave,
/// tipos de ingreso y un pequeño conjunto de datos de ejemplo para que el
/// dashboard se vea poblado en el primer arranque.
/// </summary>
public static class DbSeeder
{
    /// <summary>Bandera en Settings que marca que los datos de ejemplo ya se sembraron.</summary>
    private const string DemoSeededKey = "DemoSeeded";

    public static void Seed(AppDbContext db)
    {
        SeedCategories(db);
        SeedIncomeTypes(db);
        db.SaveChanges();

        SeedDemoData(db);
    }

    private static void SeedCategories(AppDbContext db)
    {
        if (db.Categories.Any()) return;

        // (Nombre, Emoji, Color, [palabras clave])
        var data = new (string Name, string Icon, string Color, string[] Keywords)[]
        {
            ("Supermercado", "🛒", "#16A34A", new[]
                { "super", "supermercado", "almacen", "verduleria", "carniceria", "fiambreria",
                  "coto", "carrefour", "jumbo", "disco", "mercado", "fruta", "verdura", "leche", "pan" }),
            ("Alquiler", "🏠", "#2563EB", new[]
                { "alquiler", "renta", "expensas", "inmobiliaria", "garantia" }),
            ("Servicios", "💡", "#F59E0B", new[]
                { "luz", "gas", "agua", "internet", "wifi", "telefono", "celular", "edenor", "edesur",
                  "metrogas", "aysa", "fibertel", "telecentro", "cable", "electricidad", "factura",
                  "boleta", "movistar", "claro", "personal", "flow" }),
            ("Transporte", "🚗", "#0D9488", new[]
                { "nafta", "combustible", "ypf", "shell", "axion", "sube", "colectivo", "subte", "tren",
                  "uber", "cabify", "didi", "taxi", "peaje", "estacionamiento", "cochera", "gomeria",
                  "mecanico", "patente", "seguro auto" }),
            ("Salud", "💊", "#DC2626", new[]
                { "farmacia", "medicamento", "remedio", "obra social", "prepaga", "osde", "swiss",
                  "medico", "dentista", "hospital", "clinica", "consulta", "kinesiologia", "analisis" }),
            ("Restaurantes", "🍔", "#EA580C", new[]
                { "restaurant", "resto", "cafe", "mcdonalds", "burger", "pizza", "delivery", "pedidosya",
                  "rappi", "cerveza", "almuerzo", "cena", "merienda", "helado", "sushi", "parrilla" }),
            ("Entretenimiento", "🎬", "#7C3AED", new[]
                { "cine", "teatro", "recital", "steam", "playstation", "xbox", "boliche", "entrada",
                  "evento", "netflix", "spotify", "disney", "streaming", "juego" }),
            ("Hogar", "🛋️", "#0891B2", new[]
                { "muebles", "easy", "sodimac", "ferreteria", "limpieza", "deco", "electrodomestico",
                  "sommier", "colchon", "blanqueria", "bazar" }),
            ("Ropa", "👕", "#DB2777", new[]
                { "ropa", "zapatillas", "zapatos", "indumentaria", "zara", "nike", "adidas", "remera",
                  "pantalon", "campera", "vestido", "calzado" }),
            ("Educación", "📚", "#4F46E5", new[]
                { "curso", "universidad", "facultad", "colegio", "cuota", "libro", "capacitacion",
                  "ingles", "posgrado", "matricula", "utiles" }),
            ("Mascotas", "🐾", "#65A30D", new[]
                { "veterinaria", "balanceado", "mascota", "perro", "gato", "vacuna" }),
            ("Otros", "📦", "#6B7280", Array.Empty<string>()),
        };

        foreach (var (name, icon, color, keywords) in data)
        {
            var category = new Category
            {
                Name = name,
                IconGlyph = icon,
                ColorHex = color,
                IsSystem = true,
                Keywords = keywords
                    .Select(k => new CategoryKeyword { Keyword = k, Weight = 5 })
                    .ToList()
            };
            db.Categories.Add(category);
        }
    }

    private static void SeedIncomeTypes(AppDbContext db)
    {
        if (db.IncomeTypes.Any()) return;

        string[] types = { "Sueldo", "Freelance", "Aguinaldo", "Alquiler percibido", "Inversiones", "Venta", "Otros" };
        foreach (var t in types)
            db.IncomeTypes.Add(new IncomeType { Name = t, IsSystem = true });
    }

    private static void SeedDemoData(AppDbContext db)
    {
        // Los datos de ejemplo se siembran una sola vez y queda constancia en Settings:
        // si se contaran las filas, "Borrar todos los datos" se desharía al reiniciar.
        if (db.Settings.Any(s => s.Key == DemoSeededKey)) return;

        // Base anterior a la bandera que ya tenía datos: marcar y no volver a sembrar.
        if (db.People.Any() || db.Expenses.Any())
        {
            MarkDemoSeeded(db);
            db.SaveChanges();
            return;
        }

        var ana = new Person { Name = "Ana", MonthlyIncome = 800000m, ColorHex = "#2563EB" };
        var bruno = new Person { Name = "Bruno", MonthlyIncome = 500000m, ColorHex = "#DB2777" };
        db.People.AddRange(ana, bruno);
        db.SaveChanges();

        Category Cat(string name) => db.Categories.First(c => c.Name == name);

        void AddExpense(DateTime date, string desc, decimal amount, Category cat, Person paidBy, SplitType split)
        {
            var expense = new Expense
            {
                Date = date,
                Description = desc,
                Amount = amount,
                CategoryId = cat.Id,
                PaidByPersonId = paidBy.Id,
                SplitType = split,
                CreatedAt = DateTime.Now
            };

            // Reparto entre Ana y Bruno (datos de ejemplo).
            (decimal aShare, decimal bShare, double aW, double bW) = split switch
            {
                SplitType.Proportional => ProportionalSplit(amount, ana.MonthlyIncome, bruno.MonthlyIncome),
                _ => EqualSplit(amount) // Equal y Custom de ejemplo se reparten en partes iguales
            };

            expense.Shares.Add(new ExpenseShare { PersonId = ana.Id, Amount = aShare, Weight = aW });
            expense.Shares.Add(new ExpenseShare { PersonId = bruno.Id, Amount = bShare, Weight = bW });
            db.Expenses.Add(expense);
        }

        var thisMonth = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
        var lastMonth = thisMonth.AddMonths(-1);

        AddExpense(thisMonth, "Alquiler departamento", 350000m, Cat("Alquiler"), ana, SplitType.Proportional);
        AddExpense(thisMonth.AddDays(1), "Compra mensual Coto", 85000m, Cat("Supermercado"), bruno, SplitType.Equal);
        AddExpense(thisMonth.AddDays(2), "Luz Edenor", 32000m, Cat("Servicios"), ana, SplitType.Proportional);
        AddExpense(thisMonth.AddDays(3), "Nafta YPF", 28000m, Cat("Transporte"), bruno, SplitType.Equal);
        AddExpense(thisMonth.AddDays(4), "Cena restaurante", 42000m, Cat("Restaurantes"), ana, SplitType.Equal);
        AddExpense(thisMonth.AddDays(4), "Internet Fibertel", 18000m, Cat("Servicios"), bruno, SplitType.Equal);
        AddExpense(thisMonth.AddDays(5), "Farmacia", 15000m, Cat("Salud"), ana, SplitType.Equal);

        AddExpense(lastMonth, "Alquiler departamento", 350000m, Cat("Alquiler"), ana, SplitType.Proportional);
        AddExpense(lastMonth.AddDays(2), "Supermercado Jumbo", 78000m, Cat("Supermercado"), bruno, SplitType.Equal);
        AddExpense(lastMonth.AddDays(14), "Netflix + Spotify", 9000m, Cat("Entretenimiento"), bruno, SplitType.Equal);

        // Ingresos de ejemplo
        IncomeType IncType(string name) => db.IncomeTypes.First(t => t.Name == name);
        db.Incomes.AddRange(
            new Income { Date = thisMonth, PersonId = ana.Id, IncomeTypeId = IncType("Sueldo").Id, Amount = 800000m, Description = "Sueldo mensual" },
            new Income { Date = thisMonth, PersonId = bruno.Id, IncomeTypeId = IncType("Sueldo").Id, Amount = 500000m, Description = "Sueldo mensual" },
            new Income { Date = thisMonth.AddDays(9), PersonId = bruno.Id, IncomeTypeId = IncType("Freelance").Id, Amount = 120000m, Description = "Proyecto web" }
        );

        MarkDemoSeeded(db);
        db.SaveChanges();
    }

    private static void MarkDemoSeeded(AppDbContext db)
        => db.Settings.Add(new AppSetting { Key = DemoSeededKey, Value = "true" });

    private static (decimal, decimal, double, double) EqualSplit(decimal amount)
    {
        decimal half = Math.Round(amount / 2m, 2, MidpointRounding.AwayFromZero);
        decimal other = amount - half; // garantiza que sumen exactamente el total
        return (half, other, 1, 1);
    }

    private static (decimal, decimal, double, double) ProportionalSplit(decimal amount, decimal incomeA, decimal incomeB)
    {
        decimal total = incomeA + incomeB;
        if (total <= 0) return EqualSplit(amount);

        decimal aShare = Math.Round(amount * incomeA / total, 2, MidpointRounding.AwayFromZero);
        decimal bShare = amount - aShare;
        return (aShare, bShare, (double)incomeA, (double)incomeB);
    }
}
