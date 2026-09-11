# CLAUDE.md — Guía del proyecto

App de escritorio **Windows / .NET 8 WPF** para dividir gastos compartidos.
UI en **español**, diseño **Fluent (Windows 11)**, **100 % offline**.

## Comandos

```powershell
dotnet build                 # compilar
dotnet run                   # ejecutar
dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true
```

No hay proyecto de tests en la solución. Para verificar el motor sin GUI se usó
un proyecto temporal headless que referenciaba `Models/`, `Data/`, `Services/`
y `Helpers/` (sin WPF) y validaba split/liquidación/categorización/Excel.

## Arquitectura (MVVM)

- `Models/` — entidades POCO. Enum `SplitType` (Equal / Proportional / Custom).
- `Data/AppDbContext.cs` — EF Core + SQLite. Esquema creado con
  `Database.EnsureCreated()` (NO hay migraciones). `DbSeeder.cs` carga
  categorías + palabras clave + tipos de ingreso + datos demo (Ana y Bruno).
- `Services/`
  - `SplitCalculator` — divide un monto; reparte centavos por **resto mayor**
    (las partes SIEMPRE suman el total exacto).
  - `SettlementService` — balances por persona + liquidación voraz "quién paga a quién".
  - `CategorizationService` — sugiere categoría por palabras clave y **aprende**
    en `LearnAsync` (refuerza/crea keywords al confirmar un gasto).
  - `ExcelExportService` — ClosedXML, 4 hojas (Resumen/Gastos/Por categoría/Ingresos).
  - `SettingsService` — pares clave-valor (moneda, decimales, tema, división por defecto).
  - `DialogService` — abre las ventanas de edición y diálogos de archivo.
- `ViewModels/` — `CommunityToolkit.Mvvm` (`[ObservableProperty]`, `[RelayCommand]`).
- `Views/Pages` y `Views/Dialogs` — XAML. Navegación con `ui:NavigationView`.
- `App.xaml.cs` — DI con `ServiceCollection`; `AddDbContextFactory<AppDbContext>`.

## Convenciones y "gotchas"

- **DB por operación**: se inyecta `IDbContextFactory<AppDbContext>` y se crea un
  contexto corto por unidad de trabajo (no un DbContext compartido).
- **SQLite + decimal**: SQLite no agrega `decimal` en SQL. Siempre **materializar
  con `ToList()` y sumar en memoria** (no `.Sum()` traducido a SQL).
- **Páginas**: registradas como singleton; cada `Page` setea `DataContext = vm` y
  llama `await vm.LoadAsync()` en `Loaded` (se recarga al navegar).
- **Editores**: `DialogService.Show*Editor(vm)` → `ShowDialog()`; el botón Guardar
  llama `vm.Validate()` y, si pasa, setea `DialogResult = true`. El VM de lista
  persiste y llama `LoadAsync()`.
- **Íconos**: enum `SymbolRegular` de WPF-UI (p. ej. `DataPie24`). Usar sintaxis
  de elemento-propiedad para `ui:Button.Icon` (no el markup extension).
- **Montos en TextBox**: todo binding numérico con `UpdateSourceTrigger=PropertyChanged`
  debe usar `conv:DecimalTextConverter` (Converters/AppConverters.cs). Sin él, al tipear
  el separador decimal el binding reescribe la caja y "1500,50" termina en 150050.
  Los campos con `LostFocus` no lo necesitan. `App.OnStartup` fija además
  `FrameworkElement.Language` a la cultura actual.
- **Distribuible**: `dotnet publish ... -o dist` (ver README). Los procesos lanzados desde la
  app de escritorio de Claude ven un `%LocalAppData%` redirigido (LocalCache del paquete),
  o sea otra `gastos.db`: no es un bug de la app.
- **NU1701**: silenciado en el `.csproj` (SkiaSharp/OpenTK de LiveCharts2).
- **Datos**: `%LocalAppData%\GastosCompartidos\gastos.db`.

## Paquetes

WPF-UI 3.0.5 · LiveChartsCore.SkiaSharpView.WPF 2.0.0-rc5.4 ·
Microsoft.EntityFrameworkCore.Sqlite 8.0.11 · ClosedXML 0.104.2 ·
CommunityToolkit.Mvvm 8.3.2 · Microsoft.Extensions.Hosting 8.0.1
