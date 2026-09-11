# Revisión de UI/UX y de módulos — Gastos Compartidos v1.0.0

Revisión de **solo lectura** (no se modificó ni una línea de código) del árbol completo:
`App.xaml`, `MainWindow.xaml`, `Views/Pages/*`, `Views/Dialogs/*`, `Converters/`,
`Helpers/`, `Models/`, `Data/`, `Services/`, `ViewModels/`.

---

## Resumen ejecutivo

La app está bien estructurada (MVVM limpio, DI, contexto corto por operación, motor
de división y liquidación correctos y probados) y el aspecto Fluent es coherente. Los
problemas serios no están en la arquitectura sino en **tres frentes concretos**:

1. **Cultura de números.** Los `TextBox` enlazados a `decimal`/`double` parsean con
   **en-US** (la cultura por defecto de `FrameworkElement.Language`), mientras que toda
   la app *muestra* importes con **es-AR**. Escribir `1500,50` en el monto de un gasto
   guarda **150 050**; escribir `1.500` guarda **1,5**. Es un error de dinero silencioso.
2. **La app se re-siembra a sí misma.** Después de "Borrar todos los datos", el
   siguiente arranque vuelve a crear a Ana, Bruno, 10 gastos y 3 ingresos de demo.
3. **Ancho mínimo declarado (940 px) que la UI no soporta.** La fila de filtros de
   Gastos necesita ~888 px y sólo tiene ~615; los KPI del Dashboard necesitan 157 px de
   texto en 118 px útiles. A 940 px se recorta contenido sin elipsis ni scroll.

Además hay una brecha de **accesibilidad de color**: los tres colores de balance
(`#16A34A`, `#DC2626`, `#64748B`) están hardcodeados y ninguno cumple 4,5:1 en ambos
temas; y los gráficos LiveCharts no siguen el tema (separadores y etiquetas
prácticamente invisibles en oscuro). El helper de scroll cumple su objetivo pero traga
la rueda de forma incondicional y se desincroniza al arrastrar la barra.

**Conteo:** 16 Bug · 20 UX · 8 Mejora · 20 Deuda técnica = **64 hallazgos**.

---

## Metodología y verificación

- `dotnet build` → **correcto, 0 advertencias, 0 errores**.
- Sonda headless propia (fuera del proyecto, en el scratchpad) que referencia
  `GastosCompartidos.dll` y verifica: cultura real de binding en `TextBox`/`DatePicker`,
  anchos de texto reales con `FormattedText` (Segoe UI), `SplitCalculator` en 13 casos
  borde, `SettlementService`, el patrón `RemoveRange` sobre la navegación de EF, el ciclo
  reset → reinicio del `DbSeeder`, `CategorizationService` y ratios de contraste WCAG.
- Todo hallazgo marcado *(verificado)* tiene salida de ejecución detrás; el resto sale
  de lectura de código con cita de línea.
- **No verificado por captura de pantalla**: no se ejecutó la GUI (requiere control del
  escritorio). Los cálculos de recorte son aritmética de anchos medidos + los tamaños
  declarados en el XAML, y están explicitados para que se puedan comprobar.

---

# 1. Bugs

### B1 — Los campos numéricos parsean en en-US mientras la app muestra en es-AR
- **Severidad:** Bug (crítico)
- **Archivo:** `Views/Dialogs/ExpenseEditWindow.xaml:38` (Monto), `:104` (Peso) ·
  `Views/Dialogs/IncomeEditWindow.xaml:35` · `Views/Dialogs/PersonEditWindow.xaml:27` ·
  `Helpers/CurrencyFormatter.cs:17`
- **Descripción:** *(verificado)* `FrameworkElement.Language` vale `en-us` por defecto y
  es la `ConverterCulture` implícita de todo `Binding`. Con `CurrentCulture = es-AR`:

  | El usuario escribe | Se guarda | Comentario |
  |---|---|---|
  | `1500,50` | `150050` | la coma se lee como separador de miles → **×100** |
  | `1.500` | `1,5` | el punto se lee como decimal → **÷1000** |
  | `1234567,89` | `123456789` | ×100 |
  | `9.999.999,99` | *(sin cambio)* | error de conversión: borde rojo, **sin mensaje** |
  | `1500.50` | `1500,50` | sólo funciona si se escribe a la yanqui |

  En sentido inverso, al **editar** un gasto de $1.234,56 el `TextBox` muestra `1234.56`
  mientras la grilla de al lado muestra `$ 1.234,56`. Lo mismo con el peso de la división
  personalizada: `1,5` → `15`.
- **Por qué importa:** es una app de plata. Un error de ×100 en un gasto rompe balances,
  liquidación y el Excel, y el usuario no tiene forma de darse cuenta salvo releyendo.
  Es el hallazgo de mayor impacto del informe.
- **Sugerencia:** fijar `Language="{x:Static ...}"` a la cultura actual en cada
  `FluentWindow` (`FrameworkElement.LanguageProperty.OverrideMetadata` en `App.OnStartup`
  es la forma de una línea), o bien enlazar a `string` y convertir con un
  `IValueConverter` que use `CultureInfo.CurrentCulture` + `NumberStyles.Currency`. En
  cualquier caso agregar `ValidatesOnExceptions`/`NotifyOnValidationError` para que el
  error de formato muestre texto y no sólo un borde.

### B2 — "Borrar todos los datos" se deshace solo en el siguiente arranque
- **Severidad:** Bug (crítico)
- **Archivo:** `Data/DbSeeder.cs:92` · `ViewModels/SettingsViewModel.cs:112-128` ·
  `App.xaml.cs:37`
- **Descripción:** *(verificado)* `SeedDemoData` sólo se saltea si
  `db.People.Any() || db.Expenses.Any()`. `ResetData` borra exactamente esas dos tablas.
  Secuencia comprobada: arranque 1 → 2 personas / 10 gastos / 3 ingresos → reset → 0/0/0 →
  arranque 2 → **2 personas / 10 gastos / 3 ingresos otra vez**.
- **Por qué importa:** el usuario borra sus datos, cierra la app y al abrirla se encuentra
  con gastos de Ana y Bruno mezclados. Además contradice el propio mensaje de confirmación.
- **Sugerencia:** marcar el sembrado con una fila en `Settings`
  (`DemoDataSeeded = true`) y consultar esa bandera en vez de contar filas; `ResetData`
  no la toca.

### B3 — "Gastos del mes" e "Ingresos del mes" incluyen fechas futuras
- **Severidad:** Bug
- **Archivo:** `ViewModels/DashboardViewModel.cs:72`, `:73`, `:85`
- **Descripción:** el filtro es `e.Date >= monthStart` **sin cota superior**. Un gasto
  cargado con fecha del mes que viene (perfectamente posible: el `DatePicker` no tiene
  `DisplayDateEnd`) entra en el KPI "Gastos del mes" y en la torta rotulada "Este mes".
- **Por qué importa:** el KPI principal del dashboard miente y la torta rotula mal su
  alcance. `TotalExpensesAll` en cambio sí es correcto, lo que hace el error más confuso.
- **Sugerencia:** `e.Date >= monthStart && e.Date < monthStart.AddMonths(1)` en las tres
  líneas (el gráfico de 6 meses, línea 112, ya lo hace bien).

### B4 — `SplitCalculator` lanza `OverflowException` con montos ≥ ~9,3 × 10¹⁶
- **Severidad:** Bug
- **Archivo:** `Services/SplitCalculator.cs:60` · rutas de llamada
  `ViewModels/ExpenseEditViewModel.cs:109` (por tecla) y `:188` (al guardar) ·
  `ViewModels/ExpensesViewModel.cs:127`, `:168`
- **Descripción:** *(verificado)* `(long)Math.Round(totalAmount * 100m, ...)` desborda
  `Int64` por encima de `92233720368547758,07`. `Validate()` (`ExpenseEditViewModel.cs:168`)
  sólo exige `Amount > 0`, sin techo. Escribiendo 17+ dígitos en Monto:
  - la excepción durante `Recompute()` se pierde en el motor de binding → la previsualización
    de partes queda **desactualizada sin aviso**;
  - al pulsar Guardar, `GetShares()` vuelve a llamar a `Calculate` desde un
    `AsyncRelayCommand` sin `try/catch` → cuadro "Ocurrió un error inesperado".
- **Por qué importa:** un cero de más al tipear rompe el diálogo con un error técnico en
  vez de un mensaje de validación.
- **Sugerencia:** validar un máximo razonable (p. ej. `Amount <= 999_999_999_999m`) en
  `Validate()` y devolver una lista vacía o lanzar `ArgumentOutOfRangeException`
  documentada en el calculador en vez de desbordar.

### B5 — Peso `NaN` en la división personalizada produce importes absurdos
- **Severidad:** Bug
- **Archivo:** `Services/SplitCalculator.cs:42`
- **Descripción:** *(verificado)* el saneo es `if (weights[i] < 0) weights[i] = 0;` y
  `NaN < 0` es `false`, así que `NaN` pasa. Resultado con dos participantes y un peso
  `NaN`: `[-92.233.720.368.547.758,07, -92.233.720.368.547.758,07]`. Y `double.TryParse("NaN")`
  **devuelve true en en-US** (verificado), que es justamente la cultura del `TextBox` de
  peso (ver B1), así que escribir literalmente `NaN` en el campo alcanza.
- **Por qué importa:** además de lo grotesco de la salida, esos valores se guardarían en
  `ExpenseShare.Amount` con `SaveChanges` si el usuario confirma.
- **Sugerencia:** `if (!double.IsFinite(weights[i]) || weights[i] < 0) weights[i] = 0;`.

### B6 — El Excel exportado ignora la cantidad de decimales configurada
- **Severidad:** Bug
- **Archivo:** `Services/ExcelExportService.cs:38`
- **Descripción:** *(verificado)* `BuildMoneyFormat(data.CurrencySymbol, 2)` fija 2
  decimales a mano. Con `Decimals = 0` en Configuración, la app muestra `$ 1.235` y el
  Excel emite el formato `"US$" #,##0.00`. `ReportData` ni siquiera transporta el valor.
- **Por qué importa:** el informe es la salida "oficial" hacia afuera; que no respete la
  configuración es una inconsistencia visible entre pantalla y archivo.
- **Sugerencia:** agregar `int Decimals` a `ReportData`, poblarlo desde
  `CurrencyFormatter.Decimals` en `ReportsViewModel.Export` y pasarlo a `BuildMoneyFormat`.

### B7 — Nombre duplicado de categoría o tipo de ingreso → excepción cruda de SQLite
- **Severidad:** Bug
- **Archivo:** `ViewModels/CategoriesViewModel.cs:84-91` (alta), `:108` (edición) ·
  `Data/AppDbContext.cs:28` (índice único) · `ViewModels/CategoryEditViewModel.cs:41`
- **Descripción:** `Validate()` sólo comprueba que el nombre no esté vacío. El índice
  único sobre `Category.Name` hace fallar `SaveChangesAsync` con `DbUpdateException`;
  como el comando es un `AsyncRelayCommand` sin `try/catch`, la excepción sube al
  `DispatcherUnhandledException` y el usuario ve *"Ocurrió un error inesperado: SQLite
  Error 19: 'UNIQUE constraint failed: Categories.Name'"*.
- **Por qué importa:** un caso de uso trivial (crear "Viajes" dos veces) rompe la app con
  un mensaje de base de datos en inglés.
- **Sugerencia:** comprobar la existencia antes de guardar (como ya se hace bien en
  `SettingsViewModel.AddIncomeType:73`) y envolver los `SaveChangesAsync` de los comandos
  en `try/catch (DbUpdateException)` con un `_dialog.Error` en español.

### B8 — Cambiar moneda o decimales no refresca lo que ya está en pantalla
- **Severidad:** Bug
- **Archivo:** `Helpers/CurrencyFormatter.cs:12-13` · `Converters/AppConverters.cs:11-41` ·
  `ViewModels/SettingsViewModel.cs:56-57` · `Views/Pages/SettingsPage.xaml:40`
- **Descripción:** `CurrencyFormatter` es estático y mutable sin notificación, y los
  converters no tienen forma de reevaluarse. Los importes ya renderizados conservan el
  símbolo viejo hasta navegar a otra sección y volver. Además, con
  `UpdateSourceTrigger=PropertyChanged` en el símbolo, **cada tecla** dispara
  `SettingsService.Set` → `SaveChanges` síncrono en el hilo de UI.
- **Por qué importa:** el cambio parece no haber tenido efecto y el usuario lo repite.
- **Sugerencia:** convertir `CurrencyFormatter` en un servicio observable (o publicar un
  evento estático `FormatChanged` al que los VM se suscriban para lanzar
  `OnPropertyChanged(string.Empty)`), y pasar el símbolo a `UpdateSourceTrigger=LostFocus`.

### B9 — `Format` y `FormatSigned` colocan el signo negativo en lugares distintos
- **Severidad:** Bug (menor)
- **Archivo:** `Helpers/CurrencyFormatter.cs:18` vs `:26`
- **Descripción:** *(verificado)* `Format(-100)` → `$ -100,00`; `FormatSigned(-100)` →
  `-$ 100,00`. Ambos aparecen en el Dashboard (la fila "Pagó" usa `Currency`, el balance
  usa `SignedCurrency`) uno debajo del otro.
- **Por qué importa:** inconsistencia visible en la misma tarjeta.
- **Sugerencia:** unificar en `-$ 100,00` (o en el patrón de `CultureInfo.NumberFormat`)
  y reutilizar una sola función.

### B10 — `async void TrySuggestCategory`: excepciones no observadas, CTS sin liberar, sugerencia que no se limpia
- **Severidad:** Bug
- **Archivo:** `ViewModels/ExpenseEditViewModel.cs:139-163`
- **Descripción:** tres problemas en el mismo método:
  1. Es `async void` y sólo captura `OperationCanceledException`. Cualquier otro fallo
     (base bloqueada por otro proceso, disco lleno) escapa como excepción no observada
     desde una pulsación de tecla.
  2. `_suggestCts` nunca se `Dispose()` — se crea un `CancellationTokenSource` por cada
     carácter escrito en la descripción.
  3. `_applyingSuggestion = false` (línea 159) no está en `finally`, y `SuggestionInfo`
     **nunca se limpia**: el cartel "✨ Categoría sugerida: Salud" sigue visible aunque
     el usuario cambie la categoría a mano o borre la descripción.
- **Por qué importa:** (1) puede tumbar el diálogo, (3) muestra información falsa.
- **Sugerencia:** devolver `Task` y consumirlo con un helper `SafeFireAndForget`, o al
  menos `catch (Exception ex) { SuggestionInfo = null; }`; usar `try/finally` para
  `_applyingSuggestion`; poner `SuggestionInfo = null` al principio de
  `OnDescriptionChanged` y en `OnSelectedCategoryChanged` cuando el cambio es manual.

### B11 — `ExpenseCount` obsoleto al borrar una categoría
- **Severidad:** Bug
- **Archivo:** `ViewModels/CategoriesViewModel.cs:138`
- **Descripción:** `item.ExpenseCount` es una foto tomada en `LoadAsync`. Si entre la
  carga y el borrado se creó un gasto en esa categoría (posible con dos ventanas de la
  app, o simplemente navegando y volviendo sin recargar), el chequeo pasa y el
  `DeleteBehavior.Restrict` de `AppDbContext.cs:45` hace fallar `SaveChanges` con la
  misma excepción cruda de B7.
- **Sugerencia:** re-consultar `db.Expenses.AnyAsync(e => e.CategoryId == item.Id)` dentro
  del mismo contexto que hace el borrado (como sí hace `PeopleViewModel.Delete:86`).

### B12 — Doble clic sobre el encabezado del DataGrid abre el editor
- **Severidad:** Bug (menor)
- **Archivo:** `Views/Pages/ExpensesPage.xaml.cs:20-24` ·
  `Views/Pages/IncomesPage.xaml.cs:20-24`
- **Descripción:** `MouseDoubleClick` está en el `DataGrid` entero y el handler usa
  `_vm.SelectedExpense`, no la fila bajo el puntero. Doble clic en el encabezado de
  columna, en la barra de scroll o en el área vacía bajo las filas abre el editor del
  último elemento seleccionado.
- **Sugerencia:** resolver el `DataGridRow` desde `e.OriginalSource` con
  `VisualTreeHelper` y salir si no hay fila, o usar
  `<Style TargetType="DataGridRow"><EventSetter Event="MouseDoubleClick" .../></Style>`.

### B13 — `Preset` de período dispara dos recargas completas y hay carrera
- **Severidad:** Bug (menor)
- **Archivo:** `ViewModels/ReportsViewModel.cs:47-48`, `:95-117`
- **Descripción:** `OnFromChanged`/`OnToChanged` hacen `_ = UpdatePreviewAsync()`
  (fire-and-forget, sin cancelación, sin `try/catch`). `Preset` asigna `From` y luego
  `To`, así que cada botón de período lanza dos lecturas de **toda** la tabla de gastos e
  ingresos, y no hay garantía de que termine última la que corresponde al estado final.
- **Sugerencia:** una bandera `_suspendPreview` alrededor de las asignaciones de `Preset`
  y un `CancellationTokenSource` reemplazable para el preview.

### B14 — `IsLoading` está declarado en cuatro VM y no se usa en ningún XAML
- **Severidad:** Bug (código muerto + estado faltante)
- **Archivo:** `ViewModels/ExpensesViewModel.cs:27` · `IncomesViewModel.cs:23` ·
  `PeopleViewModel.cs:19` · `CategoriesViewModel.cs:36`
- **Descripción:** *(verificado por grep)* ninguna vista enlaza `IsLoading`. La propiedad
  se setea y nadie la mira: **no existe estado de carga en ninguna lista**.
- **Por qué importa:** con una base grande, al navegar a Gastos la pantalla queda vacía
  sin explicación (y el estado vacío "No hay gastos para mostrar" aparece *durante* la
  carga, porque `ResultCount` vale 0).
- **Sugerencia:** enlazar un `ui:ProgressRing` a `IsLoading` y ocultar el estado vacío
  mientras carga (`ResultCount == 0 && !IsLoading`).

### B15 — Las cargas de página no manejan errores
- **Severidad:** Bug
- **Archivo:** todas las páginas, p. ej. `Views/Pages/DashboardPage.xaml.cs:15`
- **Descripción:** `Loaded += async (_, _) => await _vm.LoadAsync();` es un `async void`
  de facto. Si la base está bloqueada o corrupta, la excepción llega al
  `DispatcherUnhandledException` con un mensaje técnico y la página queda en blanco.
- **Sugerencia:** `try/catch` en el handler con `_dialog.Error` en español, o un
  `ErrorMessage` observable renderizado en la página (`InfoBar` de WPF-UI).

### B16 — Si falla el arranque, la app queda como proceso sin ventana
- **Severidad:** Bug (menor)
- **Archivo:** `App.xaml.cs:33-47`, `:115-123`
- **Descripción:** `EnsureCreated()`/`Seed()` corren antes de `main.Show()`. Si lanzan,
  `OnUnhandledException` muestra el cuadro y pone `Handled = true`; `MainWindow` nunca se
  crea y, con `ShutdownMode.OnLastWindowClose` sin ninguna ventana abierta, el proceso
  queda vivo e invisible.
- **Sugerencia:** envolver el bloque de arranque en su propio `try/catch` que muestre el
  error y llame a `Shutdown(1)`.

---

# 2. UX / diseño visual

### U1 — La fila de filtros de Gastos se recorta al ancho mínimo de ventana
- **Severidad:** UX (alta)
- **Archivo:** `Views/Pages/ExpensesPage.xaml:31-77` · `MainWindow.xaml:8` (MinWidth=940)
- **Descripción:** *(medido)* a 940 px de ventana el ancho útil de página es ~643 px
  (940 − ~16 de marco − 225 del rail − 56 de `PagePadding`), y ~615 dentro de la `Card`
  con `Padding="14"`. Los mínimos declarados suman **888 px**: búsqueda 220 + 10, combo
  categoría 170 + 10, dos `DatePicker` 130 + 8 y 130 + 10, botón Limpiar ~80 + 10, bloque
  "Total filtrado" ~110. Faltan **~273 px** y el `Grid` no tiene scroll ni colapso: el
  bloque "Total filtrado" (el dato más importante de la fila) queda fuera de la tarjeta.
- **Ingresos:** mismo problema, 708 px necesarios vs 615 (`IncomesPage.xaml:29-59`).
- **Sugerencia:** mover los filtros a un `WrapPanel` que apile en dos filas por debajo de
  ~1100 px, o poner las fechas y la categoría detrás de un `ui:DropDownButton` "Filtros"
  y dejar en la barra sólo búsqueda + total.

### U2 — Los KPI del Dashboard cortan los importes grandes
- **Severidad:** UX (alta)
- **Archivo:** `Views/Pages/DashboardPage.xaml:50-90`
- **Descripción:** *(medido con `FormattedText`, Segoe UI)* `"$ 9.999.999,99"` a 23 px
  Bold mide **157 px**. En un `UniformGrid` de 4 columnas a 940 px de ventana cada tarjeta
  tiene ~152 px externos, menos el `Padding` de `ui:Card` quedan **~118 px** de texto útil.
  Se recorta ~39 px sin elipsis (los `TextBlock` no tienen `TextTrimming` ni
  `TextWrapping`). La etiqueta "Cantidad de gastos" (119 px) más el icono de 16 px y su
  margen de 8 también excede.
- **Por qué importa:** un importe de 7 cifras es normal en pesos; se ve `$ 9.999.99` y el
  usuario no puede saber que está truncado.
- **Sugerencia:** `Rows="2" Columns="2"` por debajo de ~1100 px (o `TextWrapping="Wrap"` +
  `FontSize` variable), y añadir `TextTrimming="CharacterEllipsis"` con `ToolTip` con el
  valor completo como red de seguridad.

### U3 — El DataGrid de Gastos no entra a 940 px
- **Severidad:** UX
- **Archivo:** `Views/Pages/ExpensesPage.xaml:93-142`
- **Descripción:** *(medido)* las columnas de ancho fijo suman **756 px**
  (100 + 170 + 110 + 150 + 130 + 96) y hay ~699 px disponibles. La columna `Descripción`
  (`Width="*"`) se queda con 0 px y aparece barra horizontal: la descripción, que es el
  dato que identifica el gasto, desaparece.
- **Sugerencia:** pasar `División` (150 px, texto largo: "Proporcional al ingreso" mide
  143 px) a un icono o chip corto, y usar `Width="*"` con `MinWidth` en Descripción,
  Categoría y Pagó para que compitan proporcionalmente.

### U4 — Los cuatro diálogos no responden a Enter ni a Esc
- **Severidad:** UX
- **Archivo:** `Views/Dialogs/ExpenseEditWindow.xaml:136-141` ·
  `PersonEditWindow.xaml:51-56` · `CategoryEditWindow.xaml:65-70` ·
  `IncomeEditWindow.xaml:47-52`
- **Descripción:** ningún botón tiene `IsDefault="True"` / `IsCancel="True"`. En un
  diálogo modal de alta rápida (cargar 10 gastos seguidos) obliga a usar el mouse.
- **Sugerencia:** `IsDefault` en Guardar, `IsCancel` en Cancelar. Ojo: con `IsDefault`,
  el binding `LostFocus` de Monto en `IncomeEditWindow.xaml:35` y
  `PersonEditWindow.xaml:27` **no se actualiza** al pulsar Enter — hay que pasarlos a
  `PropertyChanged` o forzar `UpdateSource()` en el handler.

### U5 — Los colores de balance están hardcodeados y ninguno cumple contraste en ambos temas
- **Severidad:** UX / accesibilidad (alta)
- **Archivo:** `Converters/AppConverters.cs:46-48` ·
  `Views/Pages/DashboardPage.xaml:177` (`#16A34A`) ·
  `ExpenseEditWindow.xaml:129`, `PersonEditWindow.xaml:45`, `CategoryEditWindow.xaml:59`,
  `IncomeEditWindow.xaml:41` (`#DC2626`)
- **Descripción:** *(calculado, WCAG 2.1, fondo de tarjeta `#FFFFFF` / `#2B2B2B`)*

  | Color | Uso | Claro | Oscuro |
  |---|---|---|---|
  | `#16A34A` | balance positivo, "cuentas saldadas" | **3,30:1 ✗** | 4,30:1 ✗ |
  | `#DC2626` | balance negativo, **texto de error de los 4 diálogos** | 4,83:1 ✓ | **2,93:1 ✗** |
  | `#64748B` | balance cero | 4,76:1 ✓ | **2,98:1 ✗** |

  El verde falla en los dos temas; el rojo de los mensajes de error queda en 2,93:1 en
  tema oscuro, que es menos de la mitad del mínimo AA.
- **Por qué importa:** el mensaje de error de validación es precisamente el texto que
  *tiene* que leerse. Y el documento `docs/especificaciones-de-diseno.md` §1.4 exige que
  todo recurso funcione en ambos temas.
- **Sugerencia:** definir cuatro tokens (`PositiveTextBrush`, `NegativeTextBrush`,
  `NeutralTextBrush`, y usar `SystemFillColorCriticalBrush` de WPF-UI para los errores)
  en `App.xaml` con valor claro y oscuro, y hacer que `BalanceToBrushConverter` los
  resuelva por `Application.Current.TryFindResource` en vez de congelar hexadecimales.

### U6 — Los gráficos no siguen el tema
- **Severidad:** UX (alta)
- **Archivo:** `ViewModels/DashboardViewModel.cs:126`, `:131`, `:136-139`
- **Descripción:** tres decisiones fijas:
  - `SeparatorsPaint = new SolidColorPaint(new SKColor(0, 0, 0, 20))` — negro al 8 %:
    invisible sobre fondo oscuro.
  - `MonthlyXAxes`/`MonthlyYAxes` no definen `LabelsPaint`, así que LiveCharts usa su
    pintura por defecto (gris oscuro) → **las etiquetas de los ejes y la leyenda de la
    torta quedan casi ilegibles en tema oscuro**.
  - La barra `#2563EB` da *(calculado)* 2,74:1 sobre `#2B2B2B`.

  Además nada de esto se recalcula al cambiar de tema en caliente: hay que navegar fuera
  y volver para que `LoadAsync` reconstruya las series.
- **Sugerencia:** derivar `LabelsPaint`/`SeparatorsPaint` del tema activo
  (`ApplicationThemeManager.GetAppTheme()`), usar el color de acento para la serie, y
  suscribirse a `ApplicationThemeManager.Changed` para reconstruir los ejes.

### U7 — Con todas las personas inactivas el editor de gasto abre en un callejón sin salida
- **Severidad:** UX
- **Archivo:** `ViewModels/ExpenseEditViewModel.cs:69-71`
- **Descripción:** `PaidBy = PeopleSource.FirstOrDefault(p => p.IsActive) ?? PeopleSource.FirstOrDefault()`
  elige una persona **inactiva**, y `AddRow(p, included: p.IsActive, ...)` deja todos los
  checkboxes destildados. El usuario ve la previsualización en `$ 0,00`, pulsa Guardar y
  recibe "Elegí al menos un participante" sin explicación de por qué nadie está tildado.
  Es un estado alcanzable: `PeopleViewModel.Delete:91-98` ofrece desactivar como
  alternativa al borrado, así que se pueden desactivar todas.
- **Sugerencia:** si no hay ninguna persona activa, tildar a todas por defecto y mostrar
  un `InfoBar` "Todas las personas están inactivas — se incluyeron igual". También
  conviene filtrar (o al menos marcar) las inactivas en el combo "Pagó".

### U8 — Los pesos negativos se silencian a 0 sin avisar
- **Severidad:** UX
- **Archivo:** `Services/SplitCalculator.cs:42` · `ViewModels/ExpenseEditViewModel.cs:172`
- **Descripción:** *(verificado)* con pesos `[-5, 1]` el resultado es `[0, 100]`: la
  persona con peso negativo paga 0 y no hay ningún aviso. `Validate` sólo reacciona si la
  **suma** de pesos es ≤ 0.
- **Sugerencia:** validar peso por peso (`< 0` → error "Los pesos deben ser 0 o mayores")
  o directamente no permitir escribir el signo en el campo.

### U9 — El botón Exportar no se deshabilita mientras exporta
- **Severidad:** UX
- **Archivo:** `Views/Pages/ReportsPage.xaml:89-96` · `ViewModels/ReportsViewModel.cs:122`
- **Descripción:** hay un `ProgressRing` enlazado a `IsBusy`, pero el botón sigue
  habilitado y el guard es `if (IsBusy) return;` — el clic parece no hacer nada.
- **Sugerencia:** `[RelayCommand(CanExecute = nameof(CanExport))]` con
  `[NotifyCanExecuteChangedFor]` sobre `IsBusy`, o `IsEnabled="{Binding IsBusy, Converter=...}"`.

### U10 — Botones de icono sin nombre accesible y con área de clic chica
- **Severidad:** UX / accesibilidad
- **Archivo:** `ExpensesPage.xaml:123`, `:131` · `IncomesPage.xaml:90`, `:98` ·
  `CategoriesPage.xaml:70`, `:78` · `PeoplePage.xaml:71` · `SettingsPage.xaml:94`
- **Descripción:** los botones sólo llevan `ToolTip`. UI Automation expone el `ToolTip`
  como *HelpText*, no como *Name*, así que un lector de pantalla anuncia "botón" sin más.
  Con `Padding="7"` sobre un `SymbolIcon` de ~16 px el objetivo queda en ~30 px, por
  debajo de los 32-40 px recomendados en Fluent; el de `SettingsPage.xaml:94`
  (`Padding="3"`, icono 13 px) queda en ~19 px.
- **Sugerencia:** agregar `AutomationProperties.Name="Editar gasto"` / `"Eliminar gasto"`
  etc. y subir el padding a 8-10.

### U11 — Los selectores de color y emoji no muestran el foco de teclado ni tienen nombre
- **Severidad:** UX / accesibilidad
- **Archivo:** `App.xaml:68-90` (`SwatchItemStyle`)
- **Descripción:** el `ControlTemplate` sólo reacciona a `IsSelected` e `IsMouseOver`; no
  hay `FocusVisualStyle` ni trigger para `IsKeyboardFocused`, así que navegando con
  flechas dentro del `ListBox` no se ve dónde está el foco (salvo que coincida con la
  selección). Los ítems son `string`s hexadecimales, de modo que un lector de pantalla
  anuncia "numeral 2 5 6 3 E B".
- **Sugerencia:** trigger de `IsKeyboardFocused` con un anillo punteado y
  `AutomationProperties.Name` por swatch (nombre legible del color / del emoji).

### U12 — El `DatePicker` usa formato largo de es-AR y no entra en 130 px
- **Severidad:** UX
- **Archivo:** `ExpensesPage.xaml:63`, `:65` · `IncomesPage.xaml:47`, `:48` ·
  `ReportsPage.xaml:37`, `:41`
- **Descripción:** *(verificado)* el `DatePicker` sí toma `CultureInfo.CurrentCulture`
  (a diferencia de los `TextBox`, ver B1) y con `SelectedDateFormat` por defecto
  (`Long`) muestra **`jueves, 31 de diciembre de 2026`** — imposible en 130 px. Las
  grillas de al lado usan `StringFormat=dd/MM/yyyy`, así que la misma fecha se ve de dos
  formas distintas en la misma pantalla.
- **Sugerencia:** `SelectedDateFormat="Short"` en los seis `DatePicker` y ampliar a 140 px.

### U13 — Reportes se alinea a la izquierda con espacio vacío a la derecha
- **Severidad:** UX (menor)
- **Archivo:** `Views/Pages/ReportsPage.xaml:11`
- **Descripción:** `MaxWidth="760"` combinado con `HorizontalAlignment="Stretch"` deja el
  contenido pegado al borde izquierdo en pantalla maximizada, con ~700 px vacíos.
  `SettingsPage.xaml:11` hace lo mismo con `Left` explícito. Dashboard y Gastos, en
  cambio, ocupan todo el ancho: dos criterios de layout distintos conviviendo.
- **Sugerencia:** elegir uno. Si se quiere ancho de lectura acotado, usar
  `HorizontalAlignment="Center"` en ambas; si no, quitar el `MaxWidth`.

### U14 — No hay estado vacío ni búsqueda en Categorías
- **Severidad:** UX (menor)
- **Archivo:** `Views/Pages/CategoriesPage.xaml:28-93`
- **Descripción:** es la única lista sin estado vacío y sin filtro. Arranca con 12
  categorías y crece con las del usuario; la fila muestra hasta 10 palabras clave con
  `TextTrimming` (`CategoriesViewModel.cs:23-25`), así que a partir de cierto punto la
  vista es una pared de texto gris no navegable.
- **Sugerencia:** un `ui:TextBox` de búsqueda por nombre/palabra clave arriba de la lista.

### U15 — El mensaje del reset delega el refresco al usuario
- **Severidad:** UX
- **Archivo:** `ViewModels/SettingsViewModel.cs:127`
- **Descripción:** *"Se borraron los datos. Las pantallas se actualizarán al navegar entre
  secciones."* Es una limitación técnica explicada al usuario en vez de resuelta. Como las
  páginas y sus VM son singletons (`App.xaml.cs:69-84`), el Dashboard sigue mostrando
  balances de datos que ya no existen hasta navegar.
- **Sugerencia:** un evento `DataReset` (o `IMessenger` de CommunityToolkit, que ya está
  referenciado) al que se suscriban los VM para vaciar sus colecciones.

### U16 — "Actualizar" del Dashboard no da feedback
- **Severidad:** UX (menor)
- **Archivo:** `Views/Pages/DashboardPage.xaml:27-33` · `ViewModels/DashboardViewModel.cs:49-50`
- **Descripción:** `RefreshCommand` recarga todo (personas + gastos + partes + ingresos)
  sin deshabilitar el botón ni mostrar progreso. En una base grande el botón parece muerto.
- **Sugerencia:** `[RelayCommand]` con `IsRunning` y un `ui:ProgressRing` o
  `ui:Button.Icon` animado.

### U17 — Escala tipográfica ad hoc
- **Severidad:** UX / diseño
- **Archivo:** transversal (`App.xaml:30-54` y todas las páginas)
- **Descripción:** *(contado por grep)* conviven **13 tamaños de fuente** literales en el
  XAML: 10, 11, 12, 13, 15, 16, 17, 18, 20, 22, 23, 40, 44. Sólo cuatro estilos están
  centralizados (`PageTitle`, `PageSubtitle`, `SectionTitle`, `FieldLabel`); el resto se
  escribe inline. `FontSize="23"` en los KPI y `FontSize="17"` en las tarjetas de Persona
  son valores fuera de cualquier escala.
- **Sugerencia:** definir en `App.xaml` una escala tipo Fluent (12 / 14 / 16 / 20 / 28 /
  40) como `Double` con clave y prohibir los literales inline.

### U18 — Radios de esquina inconsistentes
- **Severidad:** UX / diseño
- **Archivo:** transversal
- **Descripción:** *(contado por grep)* ocho valores distintos: 6, 8, 9, 10, 13, 14, 16,
  23. Los "chips" usan 8 (`ExpenseEditWindow.xaml:76`), 9 (`DashboardPage.xaml:109`),
  10 (`CategoriesPage.xaml:43`) y 14 (`SettingsPage.xaml:90`) sin criterio aparente.
- **Sugerencia:** dos o tres tokens (`RadiusSmall=4`, `RadiusMedium=8`, `RadiusPill`)
  reservando los circulares (6/13/23 = mitad del lado) para avatares.

### U19 — `Opacity` como sistema de jerarquía tipográfica
- **Severidad:** UX / accesibilidad
- **Archivo:** transversal — 47 usos; `SettingsPage.xaml:48` es el peor caso
- **Descripción:** *(contado y calculado)* siete niveles de opacidad distintos
  (0.35, 0.55, 0.6, 0.65, 0.7, 0.75, 0.8). Sobre texto primario en tema claro:
  `0.7` → 6,39:1 (bien), `0.6` → **4,54:1 (justo en el límite)**,
  `0.55` → **3,90:1 (falla AA)**. El caso de 0.55 es además a `FontSize="11"`.
  `Opacity` también atenúa cualquier `Foreground` explícito, así que aplicado sobre
  `TextFillColorSecondaryBrush` (ya atenuado) el resultado empeora.
- **Sugerencia:** reemplazar por `TextFillColorSecondaryBrush` /
  `TextFillColorTertiaryBrush`, que WPF-UI ya define con contraste validado en ambos
  temas, y reservar `Opacity` para elementos decorativos (los iconos de estado vacío al
  0.35 están bien).

### U20 — Sin atajos de teclado ni tecla Supr en las listas
- **Severidad:** UX (menor)
- **Archivo:** `MainWindow.xaml` (sin `InputBindings`), `ExpensesPage.xaml:82-144`
- **Descripción:** no hay `Ctrl+N` para nuevo gasto, ni `Supr` sobre la fila
  seleccionada, ni `Ctrl+F` para el buscador. Para una app de carga repetitiva es fricción
  pura.
- **Sugerencia:** `KeyBinding` a nivel `Page` para `Ctrl+N` → `AddCommand` y
  `Delete` → `DeleteCommand` con `CommandParameter` de la fila seleccionada.

---

# 3. Evaluación específica de `ScrollViewerHelper`

El enfoque funciona y resuelve el problema real (el `Frame` interno del `NavigationView`
mide con altura infinita), pero tiene cuatro aristas concretas:

### S1 — `e.Handled = true` antes de saber si hay algo que desplazar
- **Severidad:** Bug
- **Archivo:** `Helpers/ScrollViewerHelper.cs:90-91`
- **Descripción:** el orden es `e.Handled = true;` y recién después
  `if (scrollViewer.ScrollableHeight <= 0) return;`. Cuando el contenido entra completo,
  el evento se traga igualmente y nunca burbujea. Como es `PreviewMouseWheel` en el
  contenedor, también **intercepta la rueda sobre controles internos con su propio
  scroll**: en el editor de gasto, girar la rueda sobre el `TextBox` de Notas
  (`ExpenseEditWindow.xaml:126-127`, `AcceptsReturn="True"`) desplaza el diálogo entero en
  lugar de las notas.
- **Sugerencia:** mover el `e.Handled = true` después del chequeo, y no marcar como
  manejado si `e.OriginalSource` está dentro de otro `ScrollViewer` que todavía puede
  desplazarse en esa dirección.

### S2 — Desincronización tras arrastrar la barra de scroll
- **Severidad:** UX
- **Archivo:** `Helpers/ScrollViewerHelper.cs:96-98`
- **Descripción:** la tolerancia para descartar el destino acumulado es
  `Math.Abs(current - VerticalOffset) > ViewportHeight`, es decir **un viewport entero**.
  Secuencia reproducible: rueda hacia abajo (destino queda en, digamos, 800) → arrastrar
  la barra hacia arriba 200 px (offset real 600, dentro de la tolerancia) → una muesca de
  rueda → la animación arranca del destino viejo y **salta** ~200 px.
- **Sugerencia:** invalidar el destino en `ScrollChanged` cuando el cambio no vino de la
  animación (bandera `_animating`), en vez de usar una tolerancia por tamaño.

### S3 — Suscripción a `Loaded` que nunca se quita y binding recreado en cada navegación
- **Severidad:** Deuda técnica
- **Archivo:** `Helpers/ScrollViewerHelper.cs:29-45`
- **Descripción:** `OnBindHeightToHostChanged` hace `fe.Loaded += OnElementLoaded` sin
  desuscribir. Como las páginas son singletons y `Loaded` se dispara en **cada**
  navegación, `SetBinding(MaxHeightProperty, ...)` se ejecuta una vez por visita
  (idempotente, pero descarta y recrea el `BindingExpression` cada vez). Si en el futuro
  se registraran páginas como transitorias, quedaría un handler colgado por instancia.
- **Sugerencia:** desuscribir en la primera ejecución (`fe.Loaded -= OnElementLoaded;`)
  o resolver el ancestro en `OnApplyTemplate` / `Initialized`.

### S4 — La animación nunca se limpia
- **Severidad:** Deuda técnica
- **Archivo:** `Helpers/ScrollViewerHelper.cs:104-111`
- **Descripción:** nunca se llama a `BeginAnimation(AnimatedOffsetProperty, null)` al
  terminar, así que el reloj de animación queda vinculado al `ScrollViewer` de forma
  permanente (uno por página visitada). No es una fuga grave (los `ScrollViewer` son
  singletons como las páginas) pero mantiene la propiedad "animada" para siempre.
- **Sugerencia:** `animation.Completed += (_, _) => sv.BeginAnimation(AnimatedOffsetProperty, null);`
  o `FillBehavior = FillBehavior.Stop` con el offset final aplicado a mano.

### S5 — El comportamiento de la rueda no es uniforme entre pantallas
- **Severidad:** UX
- **Archivo:** `ExpensesPage.xaml:82` · `IncomesPage.xaml:63` (sin `FixMouseWheel`)
- **Descripción:** Dashboard, Reportes, Configuración, Personas, Categorías y el editor de
  gasto tienen scroll suave animado de 280 ms; los dos `DataGrid` conservan el scroll
  nativo "a saltos" de 3 filas porque su `ScrollViewer` es interno del `ControlTemplate` y
  no lleva la propiedad adjunta.
- **Sugerencia:** aplicar el mismo helper al `ScrollViewer` interno del `DataGrid`
  (vía `Style` con `EventSetter` en `ScrollViewer` dentro de `DataGrid.Template`) o
  aceptar el scroll nativo en todas partes por coherencia.

### S6 — Alternativa más simple a `BindHeightToHost`
- **Severidad:** Mejora
- **Descripción:** acotar el alto vía `MaxHeight` es un rodeo. Se puede evitar el helper
  entero envolviendo el `ui:NavigationView` en un `Grid` con altura definida y poniendo el
  `ScrollViewer` en `MainWindow` alrededor del `Frame`, o dando al `NavigationFrame` un
  `Style` con `VerticalAlignment="Stretch"`. Vale la pena evaluarlo antes de seguir
  agregando casos especiales al helper.

---

# 4. Mejoras

| ID | Archivo | Mejora |
|---|---|---|
| M1 | `Services/CategorizationService.cs:71-104` | `LearnAsync` no tiene desaprendizaje ni poda. *(verificado)* Con la descripción "Regalo de cumpleanios para Martina Gonzalez" aprende `martina` y `gonzalez` como palabras clave permanentes. Conviene una poda periódica de keywords con `Weight == LearnIncrement` que nunca se refuerzan, y no aprender tokens que sean nombres de personas conocidas (`db.People`). |
| M2 | `Views/Pages/ExpensesPage.xaml:31-77` | Falta un filtro por persona (quién pagó / quién participa). Es la pregunta más natural en gastos compartidos y hoy sólo se puede aproximar con la búsqueda de texto. |
| M3 | `ViewModels/DashboardViewModel.cs:76-82` | Los balances y la liquidación son **siempre históricos completos**, sin selector de período, mientras el resto de la tarjeta habla del mes. Un selector "Mes / Histórico" alinearía la lectura. |
| M4 | `ViewModels/SettingsViewModel.cs:112-128` | No hay respaldo ni exportación/importación de la base. "Borrar todos los datos" es irreversible y `gastos.db` está escondido en `%LocalAppData%`. Un "Exportar copia de seguridad" (copiar el `.db`) antes del borrado es barato. |
| M5 | `Views/Pages/ExpensesPage.xaml` | No hay forma de duplicar un gasto ni de marcar uno como recurrente (alquiler, servicios). Es la carga repetitiva más obvia de esta app. |
| M6 | `Services/SettlementService.cs:51-93` | La liquidación no permite marcar transferencias como "ya pagada": el balance nunca se salda salvo cargando un gasto ficticio. Una tabla `Settlement` con pagos registrados cerraría el ciclo. |
| M7 | `Views/Dialogs/ExpenseEditWindow.xaml:103-106` | En división personalizada sólo se piden pesos. Poder escribir directamente el importe por persona (o el porcentaje) es lo que la gente espera de "personalizada". |
| M8 | `Views/Pages/DashboardPage.xaml:113-118` | La torta no tiene tooltip con importe ni porcentaje configurados explícitamente, y la leyenda a la derecha con 12 categorías desborda el alto de la tarjeta a 940×600. Considerar top-6 + "Otros". |

---

# 5. Deuda técnica

| ID | Archivo:línea | Descripción y sugerencia |
|---|---|---|
| D1 | `App.xaml.cs:36` | `EnsureCreated()` sin migraciones. Cualquier columna nueva en una base ya existente no se aplica y falla en runtime con "no such column". Como mínimo guardar una `SchemaVersion` en `Settings` y comparar al arrancar; idealmente pasar a migraciones EF. |
| D2 | `DashboardViewModel.cs:56-62`, `ExpensesViewModel.cs:59-68`, `ReportsViewModel.cs:70-78` | Cada navegación recarga **todo el histórico** con `Include` de categorías, personas y partes. Sin paginación ni filtro en SQL. A 5.000 gastos son ~15.000 filas materializadas por visita a una página. Filtrar por fecha en la consulta (EF sí traduce `DateTime`) y paginar la grilla. |
| D3 | `ReportsViewModel.cs:70-89` | Trae todo y filtra en memoria con `.ToList()` intermedios (dos listas descartadas por rango). Los `Where` de fecha pueden ir en la consulta sin violar la regla de decimales del CLAUDE.md (la regla es sobre agregaciones `Sum`, no sobre filtros). |
| D4 | `CategorizationService.cs:49` | Carga la tabla completa de `CategoryKeywords` **en cada pulsación** (debounce de 350 ms). Arranca con 145 filas *(verificado)* y crece hasta 6 por gasto guardado. Cachear en memoria con invalidación al guardar, o filtrar por los tokens con `.Where(k => tokens.Contains(k.Keyword))`. |
| D5 | `ExcelExportService.cs:34` | `_moneyFormat` es campo mutable de instancia en un servicio registrado como **singleton** (`App.xaml.cs:63`) y se asigna dentro de `Export`, que corre en `Task.Run`. Hoy hay un solo llamador con guard de `IsBusy`, pero es un estado compartido esperando a que alguien exporte dos períodos a la vez. Pasarlo como parámetro. |
| D6 | `SettingsService.cs:82-91` | `Set` abre contexto y hace `SaveChanges` **síncrono en el hilo de UI**, una vez por cada set de propiedad (y con `UpdateSourceTrigger=PropertyChanged` en el símbolo, una vez por tecla). |
| D7 | `ViewModels/CategoryEditViewModel.cs:12` | `IsSystem` se calcula y expone pero **ningún XAML lo usa** *(verificado por grep)* — código muerto. Se pensó para bloquear/avisar al editar categorías del sistema y quedó a medias. |
| D8 | `Converters/AppConverters.cs:35` vs `:15-22` | `SignedCurrencyConverter` sólo acepta `decimal`/`double`; `CurrencyConverter` acepta además `int`/`long`. Divergencia gratuita entre dos converters gemelos. |
| D9 | `Converters/AppConverters.cs:70-83` | `HexToBrushConverter` crea y congela un `SolidColorBrush` nuevo **por cada llamada**, es decir por cada fila visible y cada re-render. Un `Dictionary<string, Brush>` estático resuelve. |
| D10 | `ViewModels/SettingsViewModel.cs:63` | El VM llama a `App.ApplyTheme` — dependencia directa de la capa de aplicación WPF, imposible de testear. Debería ir detrás de un `IThemeService`. |
| D11 | `Services/DialogService.cs:27-30` | Instancia las ventanas con `new` en vez de resolverlas del contenedor. Funciona, pero los diálogos no pueden recibir dependencias y el servicio es intesteable. |
| D12 | `Data/AppDbContext.cs` | `Person.Name` no tiene índice único (sí lo tienen `Category.Name` e `IncomeType.Name`). Dos personas llamadas "Ana" hacen ambigua la liquidación ("Ana → Ana"). |
| D13 | `Services/SettlementService.cs:27-49` | *(verificado)* Si la colección `people` no contiene a todos los que tienen partes, los balances no suman cero (dio 33,33 en la prueba) y la liquidación queda incompleta **sin ningún aviso**. Hoy los dos llamadores pasan todas las personas, pero el servicio no lo garantiza. Agregar una verificación de que `Σ Balance == 0` y registrar/lanzar si no. |
| D14 | `Data/DbSeeder.cs:152-167` | `EqualSplit`/`ProportionalSplit` duplican la lógica de `SplitCalculator` con **otro** algoritmo de redondeo (`Math.Round` + resta vs resto mayor). Dos fuentes de verdad para la misma regla de negocio. Usar el `ISplitCalculator` real al sembrar. |
| D15 | `Data/AppDbContext.cs:36` + `CategorizationService.cs:85-100` | Índice único `(CategoryId, Keyword)` con patrón check-then-insert sin transacción. Dos guardados concurrentes con la misma palabra rompen con violación de restricción. Poco probable en una app monousuario, pero `IDbContextFactory` invita a la concurrencia. |
| D16 | `Services/ExcelExportService.cs:132` | `ws.SheetView.FreezeRows(1)` en la hoja Resumen congela la fila del **título**, no los encabezados de la tabla de balances (que están en una fila variable). Sin efecto útil. |
| D17 | `Services/ExcelExportService.cs:55` | `BuildMoneyFormat` interpola el símbolo entre comillas sin escapar. `MaxLength="4"` en `SettingsPage.xaml:39` permite escribir `"` y generar un formato de número inválido. |
| D18 | `Models/Expense.cs:27`, `Models/Person.cs:23` | `CreatedAt` se persiste con `DateTime.Now` (hora local, sin offset) y **no se lee en ningún lado**. O se usa para ordenar/auditar, o se saca. |
| D19 | `dist/GastosCompartidos.exe`, `dist/GastosCompartidos.pdb` | Binarios compilados dentro del árbol de fuentes, en una carpeta de OneDrive. Se van a desincronizar de la fuente y confundir a cualquiera que los ejecute. |
| D20 | Todo el proyecto | No hay proyecto de tests. El CLAUDE.md documenta que se usó un proyecto headless temporal y se descartó. `SplitCalculator`, `SettlementService`, `CategorizationService`, `TextNormalizer` y `CurrencyFormatter` son puros o casi puros: un `GastosCompartidos.Tests` con xUnit cubriría los casos borde de este informe en un par de horas y evitaría regresiones en la lógica de dinero. |

---

# 6. Lo que está bien (para no tocarlo)

Vale dejar constancia de lo verificado que **funciona correctamente**, para que un
refactor no lo rompa:

- **`SplitCalculator` reparte exacto.** *(verificado en 11 escenarios)* Las partes suman
  siempre el total: 100 entre 3 → `33,34 + 33,33 + 33,33`; `9.999.999,99` entre 3 →
  `3.333.333,33 × 3`; `0,01` entre 3 → `0,01 + 0 + 0`. Ingresos en 0 en modo proporcional
  caen correctamente a partes iguales.
- **`SettlementService` minimiza transferencias** y opera en centavos, sin arrastre de
  redondeo.
- **La regla de decimales de SQLite se respeta.** *(verificado por grep)* No hay ni un
  solo `Sum` sobre `decimal` traducido a SQL: todos operan sobre listas ya materializadas.
- **`RemoveRange` sobre la navegación de partes no rompe.** *(verificado)* El patrón de
  `ExpensesViewModel.Edit:167-169` sobrevive a `SaveChanges` sin
  `InvalidOperationException` — era un riesgo razonable de sospechar y no se materializa
  en EF Core 8.0.11.
- **`TextNormalizer` maneja bien el español.** *(verificado)* `"Año Nuevo, ñandú! 50%"` →
  `"ano nuevo nandu 50"`; `"Café-Bar #3"` → `"cafe bar 3"`.
- **La categorización acierta.** *(verificado)* "Compra mensual Coto" → Supermercado,
  "Cuota del colegio" → Educación, "Personal celular" → Servicios, y devuelve `null`
  cuando no hay señal en vez de inventar.
- **Un contexto corto por unidad de trabajo**, con `AsNoTracking` en todas las lecturas:
  la disciplina del CLAUDE.md se cumple sin excepciones.
- **Compilación limpia:** 0 advertencias, 0 errores.

---

# 7. Top 10 prioritario

Ordenado por impacto ÷ esfuerzo.

| # | Hallazgo | Severidad | Impacto | Esfuerzo |
|---|---|---|---|---|
| 1 | **B1** — Los campos numéricos parsean en en-US: `1500,50` se guarda como `150050` | Bug crítico | Corrompe datos de dinero en silencio, en el flujo principal de la app | Bajo — `FrameworkElement.LanguageProperty.OverrideMetadata` en `App.OnStartup` es una línea; validar después los 4 campos |
| 2 | **B2** — Los datos de demo vuelven después de "Borrar todos los datos" | Bug crítico | El usuario pierde la confianza en que la app respete lo que le pide | Bajo — una bandera en `Settings` en vez de contar filas |
| 3 | **B3** — "Gastos del mes" incluye fechas futuras | Bug | El KPI principal del Dashboard es incorrecto | Trivial — cota superior en 3 líneas |
| 4 | **U2 + U1 + U3** — Recorte de contenido al ancho mínimo de 940 px (KPI, filtros, grilla) | UX alta | Importes y filtros ilegibles; afecta a cualquiera con pantalla de 1366×768 | Medio — layout adaptable en 3 pantallas, o subir `MinWidth` a ~1200 y documentarlo |
| 5 | **U5 + U6** — Colores hardcodeados sin contraste y gráficos que no siguen el tema | UX alta | El tema oscuro (la mitad de las configuraciones posibles) queda ilegible en errores, balances y ejes | Medio — 4 tokens en `App.xaml` + derivar las pinturas de LiveCharts del tema |
| 6 | **B7 + B11** — Nombres duplicados y FK en uso lanzan errores crudos de SQLite | Bug | Casos de uso triviales rompen con un mensaje técnico en inglés | Bajo — validación previa + `try/catch (DbUpdateException)` |
| 7 | **B4 + B5** — Desbordes y `NaN` en `SplitCalculator` | Bug | Diálogo roto con error técnico; importes absurdos persistibles | Trivial — `double.IsFinite` + tope en `Validate()` |
| 8 | **B14 + B15** — Sin estado de carga y sin manejo de errores en las cargas de página | Bug | Pantallas en blanco o "no hay datos" falsos durante la carga; cualquier fallo de base es un cuadro genérico | Bajo — la propiedad `IsLoading` ya existe, sólo falta enlazarla |
| 9 | **S1 + S2** — El helper de scroll traga la rueda incondicionalmente y salta tras arrastrar la barra | Bug / UX | Regresión perceptible en la interacción que se acaba de arreglar | Bajo — mover una línea y cambiar el criterio de invalidación |
| 10 | **B6 + B8** — El Excel ignora los decimales configurados y el cambio de moneda no refresca | Bug | La configuración parece no tener efecto; pantalla y archivo no coinciden | Bajo — pasar `Decimals` en `ReportData` + un evento de cambio de formato |

**Fuera del top pero de alto retorno:** **D20** (proyecto de tests). Los cinco tipos con
la lógica de dinero son puros y hoy no tienen ninguna red de seguridad; los casos borde
listados en este informe son directamente el primer archivo de tests.

---

*Revisión de solo lectura: no se modificó ningún archivo del proyecto. Las verificaciones
se hicieron con un proyecto de sondeo desechable fuera del árbol de fuentes.*
