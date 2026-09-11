# Gastos Compartidos — Especificaciones de diseño

**Documento para el diseñador.** Lista todos los recursos visuales personalizables
de la aplicación con sus especificaciones técnicas exactas, para que puedan
entregarse y reemplazarse directamente en el proyecto.

| | |
|---|---|
| Producto | Gastos Compartidos v1.0.0 |
| Plataforma | Windows 10/11 — .NET 8 WPF + WPF-UI 3.0.5 (Fluent Design) |
| Idioma de la interfaz | Español (rioplatense) |
| Conectividad | 100 % offline |
| Documento generado a partir de | código fuente de la app (inventario exhaustivo) |

---

## 1. Contexto

### 1.1 Qué es la app

Aplicación de escritorio para Windows que permite a un grupo de personas
(típicamente 2 a 6 convivientes) registrar gastos compartidos, dividirlos
(en partes iguales, proporcional al ingreso o personalizado), llevar ingresos,
ver un dashboard con gráficos y exportar un informe a Excel.

La app tiene 7 pantallas principales, accesibles desde un rail de navegación
lateral:

| Pantalla | Contenido |
|---|---|
| Dashboard | 4 tarjetas KPI, torta por categoría, balances, liquidación sugerida, gráfico de columnas de 6 meses |
| Gastos | Tarjeta de filtros + tabla (DataGrid) + estado vacío |
| Ingresos | Tarjeta de filtros + tabla (DataGrid) + estado vacío |
| Personas | Grilla de tarjetas con avatar circular de color + estado vacío |
| Categorías | Lista con "chip" cuadrado de color + emoji por categoría |
| Reportes | Filtros de período + tarjeta de exportación a Excel |
| Configuración | Moneda, decimales, tema, división por defecto, tipos de ingreso, datos |

Además hay 4 ventanas de diálogo (editar gasto, ingreso, persona, categoría).

### 1.2 Plataforma y lenguaje visual

- El marco visual es **Fluent Design de Windows 11**, provisto por la librería
  **WPF-UI 3.0.5**. Los controles (botones, campos de texto, combos, tarjetas,
  rail de navegación, barra de título) ya vienen con el aspecto nativo de
  Windows 11: esquinas redondeadas, materiales translúcidos y acento del sistema.
- El objetivo del rediseño **no es reescribir los controles**, sino sustituir
  los recursos personalizables: **iconografía**, **paleta de color / tokens de
  tema**, **tipografía**, **radios y superficies**, y opcionalmente **fondos**.

### 1.3 Unidades: diseñar a 1x

- WPF trabaja en **DIP (device-independent pixels)**: 1 DIP = 1/96 pulgada.
- **1 px de diseño = 1 DIP.** Diseñá todo a escala **1x** (96 DPI).
- Windows escala automáticamente a 125 %, 150 %, 175 %, 200 %… según la
  configuración del monitor. Por eso:
  - Los iconos deben entregarse en **vectorial (SVG)** siempre que sea posible.
  - Si se entregan bitmaps, hacen falta **1x / 2x / 3x** (ver §8).
  - Evitar trazos de menos de 1 px y detalles que se pierdan a 16 px.
- Alinear el dibujo a la **grilla de píxeles enteros** en el tamaño base para
  que los trazos no queden borrosos a 1x.

### 1.4 Temas: claro y oscuro son obligatorios

- La app tiene un selector de tema en Configuración con tres valores:
  **Claro**, **Oscuro** y **Automático (según Windows)**.
- Actualmente el diccionario de temas arranca en `Theme="Light"` y se cambia en
  runtime.
- **Todo recurso entregado debe funcionar en ambos temas.** En la práctica:
  - Los iconos de interfaz son **monocromos y tintables** — se pintan con el
    color de texto del tema, no llevan color propio.
  - Los colores de categoría y persona son **configurables por el usuario** y se
    usan como fondo con glifo/emoji encima: deben tener contraste suficiente en
    ambos temas.
  - Cada token de color de la §5 necesita **dos valores**: claro y oscuro.

### 1.5 Restricción importante sobre color

Los colores de **categorías** y **personas** los elige el usuario final desde una
paleta de 16 colores dentro de la app. El diseñador **no puede asumir** un color
fijo para esos elementos: los iconos de categoría deben funcionar sobre
**cualquiera** de esos 16 fondos (por eso se especifican como glifo blanco, §8.4).

---

## 2. Inventario de iconos

Todos los iconos actuales provienen de la fuente **Fluent System Icons (Regular)**
embebida en WPF-UI (`ui:SymbolIcon Symbol="..."`). Se listan **23 símbolos
distintos** encontrados en el código. Cada uno debe tener su reemplazo.

Convención de nombres de archivo: **kebab-case**, prefijo por grupo
(`nav-`, `act-`, `cat-`, `brand-`).

Columna "Color":
- **Monocromo tintable** → un solo color, se tiñe con el color de texto del tema.
- **Glifo blanco** → se dibuja siempre en blanco, va sobre un fondo de color.
- **Full color** → color propio fijo (solo la marca).

### 2.1 Navegación (rail lateral)

Rail de 225 px de ancho abierto; el icono se muestra a ~20 px. Estado
seleccionado / hover lo maneja WPF-UI (barra indicadora + fondo).

| Archivo sugerido | Símbolo actual | Dónde se usa | Tamaño display | Tamaño diseño | Color | Notas |
|---|---|---|---|---|---|---|
| `nav-dashboard.svg` | `DataPie24` | MainWindow — item "Dashboard" | 20 px | 24×24 | Monocromo tintable | **También** se usa a 16 px en la tarjeta KPI "Cantidad de gastos" (Dashboard). Debe leerse bien a 16 px. |
| `nav-gastos.svg` | `Receipt24` | MainWindow — item "Gastos" | 20 px | 24×24 | Monocromo tintable | **También** a 16 px en KPI "Gastos del mes" y a **40 px** en el estado vacío de Gastos. |
| `nav-ingresos.svg` | `Wallet24` | MainWindow — item "Ingresos" | 20 px | 24×24 | Monocromo tintable | **También** a 16 px en KPI "Ingresos del mes" y a **40 px** en el estado vacío de Ingresos. |
| `nav-personas.svg` | `PeopleTeam24` | MainWindow — item "Personas" | 20 px | 24×24 | Monocromo tintable | **También** a **44 px** en el estado vacío de Personas. |
| `nav-categorias.svg` | `TagMultiple24` | MainWindow — item "Categorías" | 20 px | 24×24 | Monocromo tintable | Uso único. |
| `nav-reportes.svg` | `DocumentTable24` | MainWindow — item "Reportes" (pie del rail) | 20 px | 24×24 | Monocromo tintable | Uso único. |
| `nav-configuracion.svg` | `Settings24` | MainWindow — item "Configuración" (pie del rail) | 20 px | 24×24 | Monocromo tintable | Uso único. |

> **Los cinco iconos que se usan en varios tamaños (`nav-dashboard`,
> `nav-gastos`, `nav-ingresos`, `nav-personas`) deben ser legibles tanto a 16 px
> como a 44 px.** No hace falta entregar versiones ópticas distintas, pero sí
> conviene revisarlos en ambos extremos.

### 2.2 Acciones y botones

| Archivo sugerido | Símbolo actual | Dónde se usa | Tamaño display | Tamaño diseño | Color | Notas |
|---|---|---|---|---|---|---|
| `act-agregar.svg` | `Add24` | Botón primario "Nueva categoría" (Categorías), "Nuevo gasto" (Gastos), "Nuevo ingreso" (Ingresos); botón secundario "Agregar" tipo de ingreso (Configuración) | 16 px | 24×24 | Monocromo tintable | En botón primario se ve en blanco sobre el acento; en secundario, en color de texto. |
| `act-persona-agregar.svg` | `PersonAdd24` | Botón primario "Nueva persona" (Personas) | 16 px | 24×24 | Monocromo tintable | Distinto de `act-agregar`: es persona + signo más. |
| `act-editar.svg` | `Edit24` | Botón secundario "Editar" en Categorías, Gastos, Ingresos, Personas | 16 px | 24×24 | Monocromo tintable | Muy repetido: 4 pantallas. |
| `act-eliminar.svg` | `Delete24` | Botón "Danger" (icono solo) en Categorías, Gastos, Ingresos, Personas; botón "Borrar todos los datos" (Configuración) | 16 px | 24×24 | Monocromo tintable | Se ve **en blanco sobre rojo** (apariencia Danger). Verificar legibilidad invertido. |
| `act-guardar.svg` | `Save24` | Botón primario "Guardar" / "Guardar gasto" en los 4 diálogos de edición | 16 px | 24×24 | Monocromo tintable | Blanco sobre acento. |
| `act-actualizar.svg` | `ArrowClockwise24` | Botón "Actualizar" (Dashboard) | 16 px | 24×24 | Monocromo tintable | Botón estándar (no primario). |
| `act-buscar.svg` | `Search24` | Icono dentro del campo de búsqueda de Gastos e Ingresos | 16 px | 24×24 | Monocromo tintable | Va embebido en el `ui:TextBox`, atenuado. |
| `act-exportar.svg` | `DocumentArrowDown24` | Botón primario "Exportar a Excel" (Reportes) | 16 px | 24×24 | Monocromo tintable | Documento con flecha hacia abajo. |
| `act-carpeta-abrir.svg` | `FolderOpen24` | Botón "Abrir carpeta de datos" (Configuración) | 16 px | 24×24 | Monocromo tintable | — |
| `act-quitar.svg` | `Dismiss24` | Botón transparente "×" para quitar un tipo de ingreso (Configuración) | **13 px** | 24×24 | Monocromo tintable | Tamaño explícito reducido: debe ser una "×" simple, sin detalle fino. |
| `act-info.svg` | `Info24` | Aviso "Todavía no cargaste gastos" (Dashboard) | **22 px** | 24×24 | Monocromo tintable | Informativo, no interactivo. |
| `act-flecha-derecha.svg` | `ArrowRight24` | Separador "A → B" en la liquidación sugerida (Dashboard) | **13 px** | 24×24 | Monocromo tintable | Se muestra al 60 % de opacidad. Muy chico: trazo simple. |
| `act-check.svg` | `Checkmark24` | "¡Las cuentas están saldadas!" (Dashboard) | **15 px** | 24×24 | **Color fijo verde** `#16A34A` | Único icono de UI con color propio codificado. Si cambia el token de éxito (§5), cambia también este. |
| `act-persona.svg` | `Person24` | Avatar dentro del círculo de color de cada persona (Personas) | **22 px** | 24×24 | **Glifo blanco** | Va sobre un círculo de 46 px pintado con el color de la persona (uno de 16). Debe verse bien sobre todos ellos. |
| `act-dinero.svg` | `Money24` | Tarjeta KPI "Gastos totales" (Dashboard) | 16 px | 24×24 | Monocromo tintable | Billetes/moneda. Debe distinguirse claramente de `nav-ingresos` (billetera) y de `nav-gastos` (recibo). |

### 2.3 Categorías

Las 12 categorías del seed se representan hoy con un **emoji** sobre un cuadrado
redondeado de 42×42 px (radio 10 px) pintado con el color de la categoría. El
emoji se dibuja a 20 px.

**Objetivo:** reemplazar los emojis por iconos propios. El usuario puede crear
categorías nuevas y elegirles emoji + color, así que el set de iconos debe ser
un **catálogo elegible**, no un mapeo fijo.

Especificación de color: **glifo blanco** (ver §8.4 para el porqué).

#### 2.3.1 Categorías por defecto (existen en la base al primer arranque)

| Archivo sugerido | Categoría | Emoji actual | Color por defecto | Tamaño display | Tamaño diseño |
|---|---|---|---|---|---|
| `cat-supermercado.svg` | Supermercado | 🛒 | `#16A34A` | 20 px | 24×24 |
| `cat-alquiler.svg` | Alquiler | 🏠 | `#2563EB` | 20 px | 24×24 |
| `cat-servicios.svg` | Servicios | 💡 | `#F59E0B` | 20 px | 24×24 |
| `cat-transporte.svg` | Transporte | 🚗 | `#0D9488` | 20 px | 24×24 |
| `cat-salud.svg` | Salud | 💊 | `#DC2626` | 20 px | 24×24 |
| `cat-restaurantes.svg` | Restaurantes | 🍔 | `#EA580C` | 20 px | 24×24 |
| `cat-entretenimiento.svg` | Entretenimiento | 🎬 | `#7C3AED` | 20 px | 24×24 |
| `cat-hogar.svg` | Hogar | 🛋️ | `#0891B2` | 20 px | 24×24 |
| `cat-ropa.svg` | Ropa | 👕 | `#DB2777` | 20 px | 24×24 |
| `cat-educacion.svg` | Educación | 📚 | `#4F46E5` | 20 px | 24×24 |
| `cat-mascotas.svg` | Mascotas | 🐾 | `#65A30D` | 20 px | 24×24 |
| `cat-otros.svg` | Otros | 📦 | `#6B7280` | 20 px | 24×24 |

#### 2.3.2 Catálogo elegible por el usuario (24 opciones)

El selector de icono de una categoría ofrece hoy estos 24 emojis. Los 12 de
arriba están incluidos; faltan estos 12:

| Archivo sugerido | Emoji actual | Significado sugerido |
|---|---|---|
| `cat-viajes.svg` | ✈️ | Viajes / avión |
| `cat-regalos.svg` | 🎁 | Regalos |
| `cat-tarjeta.svg` | 💳 | Tarjeta de crédito |
| `cat-telefono.svg` | 📱 | Teléfono / celular |
| `cat-combustible.svg` | ⛽ | Nafta / surtidor |
| `cat-hospital.svg` | 🏥 | Hospital / clínica |
| `cat-bebidas.svg` | 🍷 | Bebidas / salidas |
| `cat-trabajo.svg` | 💼 | Trabajo / maletín |
| `cat-gimnasio.svg` | 🏋️ | Gimnasio / deporte |
| `cat-peluqueria.svg` | ✂️ | Peluquería / cuidado personal |
| `cat-estudios.svg` | 🎓 | Estudios / graduación |
| `cat-factura.svg` | 🧾 | Factura / ticket |

> **Nota de implementación:** hoy el modelo guarda un `IconGlyph` (string con el
> emoji). Si se adoptan iconos vectoriales, se pasará a guardar el **nombre del
> archivo** (`cat-supermercado`). El diseñador solo necesita entregar los 24
> archivos con estos nombres.

### 2.4 Marca / aplicación

| Archivo sugerido | Símbolo actual | Dónde se usa | Tamaño display | Tamaño diseño | Color | Notas |
|---|---|---|---|---|---|---|
| `brand-app.svg` | `MoneyCalculator24` | Icono a la izquierda del título en la barra de título de la ventana principal | ~18 px | 24×24 | Monocromo tintable **o** full color | Si es full color, debe leerse sobre el material Mica en claro y en oscuro. Recomendación: versión monocroma para la barra de título y versión full color para el .ico. |
| `brand-app-1024.png` | *(no existe)* | Icono de la aplicación: ejecutable, barra de tareas, menú Inicio, Alt+Tab | ver §3 | 1024×1024 | Full color | **Hoy la app no define ningún `ApplicationIcon`** en el `.csproj`: usa el icono genérico de .NET. Este entregable es nuevo y necesario. |

### 2.5 Resumen del inventario

- **23 símbolos distintos** de interfaz (7 navegación + 15 acciones + 1 marca).
- **24 iconos de categoría** (12 por defecto + 12 del catálogo elegible).
- **1 icono de aplicación** multi-resolución.
- **Total: 48 archivos de icono.**

---

## 3. Icono de la aplicación (.ico)

### 3.1 Qué entrega el diseñador

- **Un PNG máster de 1024×1024 px**, fondo transparente, RGBA de 8 bits
  (`brand-app-1024.png`).
- **Opcional pero recomendado:** el SVG fuente (`brand-app-master.svg`).
- **Opcional:** una versión simplificada para tamaños chicos
  (`brand-app-small.svg`), sin detalle fino, pensada para 16 y 24 px.

Nosotros generamos el `.ico` multi-resolución a partir de ese máster y lo
enlazamos en el `.csproj`.

### 3.2 Resoluciones que contendrá el .ico

| Tamaño | Dónde se ve |
|---|---|
| 16×16 | Barra de título, listas del Explorador en vista detalle |
| 24×24 | Algunos menús del sistema |
| 32×32 | Alt+Tab, barra de tareas a 100 % de escala |
| 48×48 | Explorador, iconos medianos |
| 64×64 | Escalado 125–150 % |
| 128×128 | Iconos grandes |
| 256×256 | Iconos extra grandes, tienda, instalador |

### 3.3 Requisitos de diseño

- **Silueta reconocible a 16 px.** Probar el diseño reducido a 16×16 antes de
  entregarlo: si se vuelve una mancha, simplificar.
- **Márgenes:** dejar ~8 % de aire en cada lado dentro del lienzo de 1024 px
  (el dibujo vivo ocupa ~86 % ≈ 880×880 px centrado).
- **Fondo transparente.** Si el icono lleva una "pastilla" de color de fondo,
  esa pastilla es parte del dibujo, con esquinas redondeadas propias.
- **Sin texto** (no se lee a 16 px), salvo un monograma de 1 carácter.
- Debe funcionar sobre fondos de escritorio claros y oscuros: evitar contornos
  blancos o negros puros que desaparezcan.

---

## 4. Fondos y superficies

### 4.1 Qué se puede personalizar hoy

| Superficie | Estado actual en el código | Qué se puede cambiar | Formato del entregable |
|---|---|---|---|
| **Material de ventana** (principal y los 4 diálogos) | `WindowBackdropType="Mica"` | Mica / Mica Alt (Tabbed) / Acrylic / Sólido | Elegir una opción + si es sólido, hex por tema |
| **Barra de título** | `ui:TitleBar` con `ExtendsContentIntoTitleBar="True"` — transparente, deja ver el material | Color de fondo (o dejarla transparente), color del título | Hex por tema, o "transparente" |
| **Rail de navegación** | `ui:NavigationView`, `OpenPaneLength="225"`, `PaneDisplayMode="Left"` | Fondo del rail, color del indicador de selección, fondo de hover/selección del item | Hex por tema (4 valores) |
| **Tarjetas** (`ui:Card`) | Se usan en las 7 páginas: KPIs, gráficos, filtros, tablas, secciones de Configuración | Fondo, borde, radio, sombra | Hex por tema + radio en px |
| **Fondo de la superficie de página** | Actualmente lo aporta el material de ventana (no hay color propio) | Se puede fijar un color plano detrás del contenido | Hex por tema |
| **Pie de los diálogos** | `Border` con `Padding="22,14"` y `Background="{DynamicResource ControlFillColorSecondaryBrush}"` — barra gris clara con Cancelar + Guardar a la derecha | Fondo, borde superior | Hex por tema |
| **Estados vacíos** | Icono grande al 35 % de opacidad + texto al 60 % de opacidad, centrados | Opacidad, color del icono, se podría agregar una ilustración | Ver §4.3 |
| **Chips / píldoras** | Fondo `ControlFillColorSecondaryBrush`, radios 8 / 9 / 14 px según el chip | Fondo, radio, color de texto | Hex por tema + radio |

### 4.2 Sobre el material Mica

**Recomendación: mantener Mica.** Es el material nativo de Windows 11: toma el
fondo de escritorio del usuario, lo desatura fuertemente y lo tiñe con el color
del tema. Ventajas: se siente nativo, respeta el tema del sistema y es gratis en
rendimiento.

Si se decide cambiar:

| Opción | Efecto | Cuándo conviene |
|---|---|---|
| `Mica` | Fondo de escritorio muy difuminado, estático (no sigue el scroll del wallpaper) | Ventanas de app de larga duración — **actual** |
| `Tabbed` (Mica Alt) | Igual pero más oscuro/contrastado | Apps con pestañas |
| `Acrylic` | Translúcido, difumina lo que hay detrás en tiempo real | Ventanas transitorias, menús |
| `None` + color sólido | Sin material: color plano | Si se quiere una identidad de marca fuerte que no dependa del escritorio |

> **Atención:** con material activo, cualquier color de fondo que se pinte encima
> lo tapa. Si se quiere marca fuerte, hay que desactivar el material.

### 4.3 Imagen de fondo (opcional)

No hay ninguna imagen de fondo hoy. Si se quiere agregar una:

| Aspecto | Especificación |
|---|---|
| Dónde aparecería | (a) detrás del contenido de la ventana completa; (b) solo en el rail de navegación (225 px de ancho); (c) solo en los estados vacíos |
| Formato | **PNG** con transparencia si tiene bordes suaves; **JPG** calidad 85 si es fotográfico y ocupa toda la ventana; **SVG** si es un patrón geométrico (preferido — escala sin peso) |
| Tamaño recomendado — ventana completa | 2560×1664 px (cubre 1200×780 hasta 200 % de escala). Peso objetivo < 400 KB |
| Tamaño recomendado — rail | 450×1560 px (225×780 a 2x) |
| Tamaño recomendado — estado vacío | 480×360 px a 1x, más 2x y 3x, o SVG |
| Comportamiento de escalado | La ventana es redimensionable (mín. 940×600, sin máximo). La imagen debe funcionar como `UniformToFill` recortando por los bordes: **no** poner elementos importantes cerca del borde |
| Versiones | **Una para tema claro y otra para tema oscuro** |

**Consideraciones de legibilidad (críticas):**

- Sobre la imagen van tablas densas, números y gráficos. Si la imagen tiene
  contraste alto, el texto se vuelve ilegible.
- Regla: la imagen de fondo debe tener un **rango de luminancia acotado**
  (variación total < 15 %) o bien ir con una **capa de velo** encima
  (blanco al 70–85 % en tema claro, negro al 60–75 % en tema oscuro).
- El contraste texto/fondo debe cumplir **WCAG AA: 4.5:1** para texto normal y
  **3:1** para texto de 18 px o mayor, medido en el punto **más desfavorable**
  de la imagen.
- Preferir patrones sutiles, degradados suaves o texturas de bajo contraste sobre
  fotografías.

---

## 5. Tokens de tema

Tabla a completar por el diseñador. La columna "Actual" contiene el valor que
está hoy en el código; cuando dice *WPF-UI* significa que lo define la librería
(valor Fluent estándar de Windows 11) y no está fijado en nuestro código.

### 5.1 Acento

| Token | Actual (claro) | Actual (oscuro) | Propuesto claro | Propuesto oscuro |
|---|---|---|---|---|
| `acento` | *WPF-UI* (`SystemAccentColorPrimaryBrush`) | *WPF-UI* | | |
| `acento-hover` | *WPF-UI* | *WPF-UI* | | |
| `acento-pressed` | *WPF-UI* | *WPF-UI* | | |
| `texto-sobre-acento` | `#FFFFFF` (implícito) | `#FFFFFF` | | |
| `acento-texto` (`AccentTextFillColorPrimaryBrush`) | *WPF-UI* — usado en el anillo de selección de los swatches de color/emoji | *WPF-UI* | | |

### 5.2 Fondos y superficies

| Token | Actual (claro) | Actual (oscuro) | Propuesto claro | Propuesto oscuro |
|---|---|---|---|---|
| `fondo-ventana` | Material **Mica** (sin color propio) | Material **Mica** | | |
| `superficie-tarjeta` (`ui:Card`) | *WPF-UI* (`CardBackgroundFillColorDefaultBrush`) | *WPF-UI* | | |
| `superficie-secundaria` (`ControlFillColorSecondaryBrush`) | *WPF-UI* — pies de diálogo, chips, badges (10 usos) | *WPF-UI* | | |
| `fondo-rail-navegacion` | *WPF-UI* | *WPF-UI* | | |
| `fondo-barra-titulo` | Transparente (hereda Mica) | Transparente | | |

### 5.3 Texto

| Token | Actual (claro) | Actual (oscuro) | Propuesto claro | Propuesto oscuro |
|---|---|---|---|---|
| `texto-primario` (`TextFillColorPrimaryBrush`) | *WPF-UI* — títulos de página y de sección | *WPF-UI* | | |
| `texto-secundario` (`TextFillColorSecondaryBrush`) | *WPF-UI* — subtítulos de página | *WPF-UI* | | |
| `texto-terciario` | Se simula con `Opacity` 0.6–0.75 sobre el texto primario | ídem | | |
| `texto-deshabilitado` | *WPF-UI* (`TextFillColorDisabledBrush`) | *WPF-UI* | | |

> **Deuda técnica a resolver con el diseño:** la jerarquía terciaria hoy se hace
> con opacidad (`Opacity="0.6"`, `0.65`, `0.7`, `0.75`, `0.8`, `0.35`) en lugar de
> tokens de color. Conviene que el diseñador defina **un solo** valor de
> texto-terciario y un solo valor de texto-atenuado (estados vacíos), y los
> reemplazamos en el código.

### 5.4 Bordes

| Token | Actual (claro) | Actual (oscuro) | Propuesto claro | Propuesto oscuro |
|---|---|---|---|---|
| `borde-default` (`ControlStrokeColorDefaultBrush`) | *WPF-UI* — separador entre filas de la lista de Categorías | *WPF-UI* | | |
| `borde-sutil` | *WPF-UI* (`ControlStrokeColorSecondary`) | *WPF-UI* | | |
| `borde-tarjeta` | *WPF-UI* (`CardStrokeColorDefaultBrush`) | *WPF-UI* | | |

### 5.5 Semánticos

| Token | Actual (claro) | Actual (oscuro) | Propuesto claro | Propuesto oscuro | Dónde se usa |
|---|---|---|---|---|---|
| `exito` | `#16A34A` | `#16A34A` *(mismo)* | | | Balance positivo, check "cuentas saldadas", balances en el Excel |
| `peligro` | `#DC2626` | `#DC2626` *(mismo)* | | | Balance negativo, texto de error en los 4 diálogos, botones Danger |
| `advertencia` | *(no existe)* | *(no existe)* | | | Token nuevo, a definir |
| `info` | *(no existe)* | *(no existe)* | | | Token nuevo — el aviso "Todavía no cargaste gastos" hoy usa color de texto neutro |
| `neutro` (balance = 0) | `#64748B` | `#64748B` *(mismo)* | | | Balance en cero |

> **Importante:** hoy `exito`, `peligro` y `neutro` están **hardcodeados con el
> mismo valor para ambos temas** (en `Converters/AppConverters.cs`,
> `Views/Dialogs/*.xaml` y `Views/Pages/DashboardPage.xaml`). Sobre fondo oscuro,
> `#16A34A` y `#DC2626` quedan con contraste bajo. **Se necesita una variante
> clara de cada uno para el tema oscuro** (típicamente subir luminosidad y bajar
> saturación).

### 5.6 Foco

| Token | Actual (claro) | Actual (oscuro) | Propuesto claro | Propuesto oscuro |
|---|---|---|---|---|
| `anillo-de-foco` | *WPF-UI* (`FocusStrokeColorOuter`) — anillo estándar de Windows 11 | *WPF-UI* | | |
| `grosor-anillo-de-foco` | 2 px (estándar Fluent) | 2 px | | |

> El anillo de foco es un requisito de accesibilidad: debe ser visible al navegar
> con teclado sobre **cualquier** superficie (tarjeta, pie de diálogo, rail,
> botón primario, botón Danger).

### 5.7 Paleta de datos (data-viz) — 12 colores

Hoy la torta del Dashboard **no usa una paleta propia**: pinta cada porción con
el **color de la categoría** elegido por el usuario. Los 12 colores de abajo son
los de las 12 categorías por defecto, que en la práctica funcionan como paleta
categórica.

| # | Actual | Categoría asociada | Propuesto claro | Propuesto oscuro |
|---|---|---|---|---|
| 1 | `#16A34A` | Supermercado | | |
| 2 | `#2563EB` | Alquiler | | |
| 3 | `#F59E0B` | Servicios | | |
| 4 | `#0D9488` | Transporte | | |
| 5 | `#DC2626` | Salud | | |
| 6 | `#EA580C` | Restaurantes | | |
| 7 | `#7C3AED` | Entretenimiento | | |
| 8 | `#0891B2` | Hogar | | |
| 9 | `#DB2777` | Ropa | | |
| 10 | `#4F46E5` | Educación | | |
| 11 | `#65A30D` | Mascotas | | |
| 12 | `#6B7280` | Otros *(color de respaldo cuando falta el color)* | | |

Requisitos de la paleta (ver también §9):

- Los 12 deben ser **distinguibles entre sí** cuando aparecen juntos en la torta.
- Deben funcionar como **fondo con glifo blanco encima** (contraste ≥ 3:1 con
  blanco), porque el mismo color pinta el chip de 42×42 px de la categoría.
- Deben ser **aptos para daltonismo** (deuteranopía y protanopía sobre todo).
- Deben verse bien sobre superficie clara **y** oscura.

### 5.8 Paleta de personas — 16 colores

Es la paleta que el usuario ve al elegir el color de una persona o de una
categoría (`Helpers/Palette.cs`). Incluye los 12 de arriba más 4.

| # | Actual | | # | Actual |
|---|---|---|---|---|
| 1 | `#2563EB` | | 9 | `#EA580C` |
| 2 | `#DB2777` | | 10 | `#4F46E5` |
| 3 | `#16A34A` | | 11 | `#65A30D` |
| 4 | `#F59E0B` | | 12 | `#9333EA` |
| 5 | `#7C3AED` | | 13 | `#0EA5E9` |
| 6 | `#0D9488` | | 14 | `#E11D48` |
| 7 | `#DC2626` | | 15 | `#059669` |
| 8 | `#0891B2` | | 16 | `#6B7280` |

Estos colores se muestran como **círculos de 26 px** en el selector y como
**avatar circular de 46 px** en la tarjeta de persona, con el icono `Person24`
en **blanco** de 22 px encima.

**Requisito duro:** los 16 colores deben tener **contraste ≥ 3:1 contra blanco
puro**, porque siempre llevan un glifo blanco encima. Revisar especialmente
`#F59E0B` (ámbar), que es el más claro del set.

### 5.9 Colores del informe de Excel

El export a Excel (`Services/ExcelExportService.cs`) tiene su propia paleta.
No depende del tema (Excel no tiene tema oscuro en el archivo).

| Uso | Actual | Propuesto |
|---|---|---|
| Fondo de encabezado de tabla | `#2563EB` | |
| Texto de encabezado de tabla | `#FFFFFF` | |
| Fondo de fila de totales | `#E5EDFF` | |
| Color de título de hoja | `#1E293B` | |
| Texto secundario (subtítulos) | `#64748B` | |
| Balance positivo | `#16A34A` | |
| Balance negativo | `#DC2626` | |

### 5.10 Colores de los gráficos

| Uso | Actual | Propuesto |
|---|---|---|
| Relleno de la serie de columnas ("Gastos de los últimos 6 meses") | `#2563EB` | |
| Color de respaldo de porción sin categoría | `#6B7280` | |
| Líneas de la grilla del eje Y | Negro al 8 % (`rgba(0,0,0,0.078)`) | |
| Contorno de las porciones de la torta | Sin contorno (`Stroke = null`) | |

> **Problema conocido:** la grilla del eje Y es negra fija. En tema oscuro
> desaparece. Necesita un valor por tema.

---

## 6. Tipografía

### 6.1 Fuente actual

- **Segoe UI Variable**, provista por WPF-UI / Windows 11 (con fallback a
  **Segoe UI** en Windows 10). No está declarada explícitamente en el código:
  se hereda del tema Fluent.
- **Excepción:** la ruta de la carpeta de datos en Configuración usa
  **Consolas** a 12 px (monoespaciada).

### 6.2 Si se entrega una fuente propia

| Requisito | Especificación |
|---|---|
| Formato | **.ttf** o **.otf** (WPF soporta ambos; se embeben como recurso del proyecto) |
| Pesos necesarios | **Regular (400)**, **Medium (500)**, **SemiBold (600)**, **Bold (700)** — los cuatro se usan hoy |
| Cursivas | No se usan. No hacen falta |
| Cobertura de caracteres | **Latin-1 completo** — la UI es en español: `á é í ó ú ü ñ Ñ ¿ ¡ « »`. Verificar `«»` (se usan en textos de ayuda) |
| Símbolos de moneda | `$` obligatorio. Deseable: `€ £ ¥ R$ ₡ ₲ ₱` (la moneda es configurable por el usuario) |
| Signos | `+ − % · •` (el bullet `•` se usa en la lista de Reportes) |
| Licencia | Debe permitir **embeber en aplicación de escritorio distribuida** |
| Monoespaciada | Opcional. Si no se entrega, se mantiene Consolas para la ruta de archivo |

### 6.3 Cifras tabulares — requisito importante

La app es una calculadora de gastos: muestra **columnas de importes alineados a
la derecha** en tablas (Gastos, Ingresos), en los balances del Dashboard y en las
tarjetas KPI.

- La fuente **debe tener cifras tabulares** (`tnum`, ancho fijo por dígito), o
  bien sus cifras por defecto deben ser tabulares.
- Sin esto, los importes "bailan" al actualizarse y las columnas no alinean.
- Deseable también: **cifras de estilo revestido** (lining), no de estilo antiguo
  (old-style), para que los números tengan todos la misma altura.

### 6.4 Escala tipográfica actual (type ramp)

Estilos con nombre definidos en `App.xaml`:

| Estilo | Tamaño | Peso | Color | Margen | Dónde se usa |
|---|---|---|---|---|---|
| `PageTitle` | 26 px | Bold | `texto-primario` | 0,0,0,2 | Título de las 7 páginas |
| `SectionTitle` | 16 px | SemiBold | `texto-primario` | 2,0,0,10 | Títulos de sección dentro de tarjetas (11 usos) |
| `PageSubtitle` | 13 px | Regular | `texto-secundario` | 0,0,0,18 | Bajada bajo el título de página |
| `FieldLabel` | 12 px | Regular | Opacidad 0.7 | 0,10,0,4 | Etiqueta sobre cada campo de formulario (19 usos) |

Tamaños en línea (no tienen estilo con nombre):

| Tamaño | Peso | Dónde |
|---|---|---|
| 23 px | Bold | Valores de las 4 tarjetas KPI del Dashboard |
| 20 px | Regular | Emoji de categoría (chip de 42 px) y del selector de emoji |
| 18 px | Regular | Valores de las 4 métricas de Reportes |
| 17 px | Bold | Nombre de la persona (tarjeta de Personas) |
| 16 px | SemiBold | Contador de resultados en Gastos e Ingresos |
| 15 px | SemiBold | Nombre de la categoría en la lista |
| 15 px | — | Icono de check "cuentas saldadas" |
| 14 px | Regular | **Tamaño base heredado** — cuerpo de texto, controles, tablas |
| 13 px | Regular | Subtítulos de página; icono `Dismiss24`; flecha de liquidación |
| 12 px | Regular | Etiquetas de campo, textos auxiliares, ruta de datos (Consolas), etiquetas de ejes de los gráficos |
| 11 px | Regular | Textos muy auxiliares: "Pagó $X" en balances, chip de alcance de la torta, hints de ayuda |
| 10 px | Regular | Badges "Sistema" e "Inactiva" |

> **Recomendación:** el diseñador debería consolidar esta escala. Hay 12 tamaños
> distintos entre 10 y 26 px, varios de ellos separados por 1 px, lo cual no es
> perceptible ni mantenible. Una escala de 6–7 pasos sería suficiente.

---

## 7. Forma, espaciado y elevación

### 7.1 Radios de esquina actuales

| Componente | Radio | Dónde |
|---|---|---|
| Ventana | Redondeada del sistema (`WindowCornerPreference="Round"` en los diálogos) | 8 px, lo aplica Windows 11 |
| `ui:Card` | *WPF-UI* (Fluent estándar, 8 px) | Todas las páginas |
| Chip cuadrado de categoría | **10 px** sobre 42×42 px | Lista de Categorías |
| Tarjeta de vista previa del reparto | **8 px** | Diálogo de Gasto |
| Badge "Sistema" / "Inactiva" | **8 px** | Categorías, Personas |
| Chip de alcance de la torta ("Este mes") | **9 px** | Dashboard |
| Píldora de tipo de ingreso | **14 px** | Configuración |
| Anillo de selección de swatch | **16 px** | Selectores de color y emoji |
| Círculo de swatch de color | **13 px** sobre 26×26 px (círculo perfecto) | Selectores |
| Avatar de persona | **23 px** sobre 46×46 px (círculo perfecto) | Personas |
| Punto de color de persona | **6 px** sobre 12×12 px (círculo perfecto) | Dashboard, diálogo de Gasto |
| Barras del gráfico de columnas | **Rx=5, Ry=5** | Dashboard |
| Botones, campos, combos | *WPF-UI* (Fluent estándar, 4 px) | Todos |

> **A definir por el diseñador:** una escala de radios coherente. Hoy hay
> 8/9/10/14/16 conviviendo. Sugerencia de escala: `xs 4 · sm 6 · md 8 · lg 12 ·
> pill 999`.

### 7.2 Bordes

| Elemento | Grosor | Color |
|---|---|---|
| Separador entre filas de la lista de Categorías | **0,0,0,1** (solo abajo) | `borde-default` |
| Anillo de selección de swatch | **2 px** | `acento-texto` (transparente si no está seleccionado) |
| DataGrid (Gastos, Ingresos) | **0** (sin borde propio, va dentro de una tarjeta) | — |
| `ui:Card` | *WPF-UI* (1 px) | `borde-tarjeta` |

### 7.3 Sombras

- **Actualmente: no hay ninguna sombra explícita.** La profundidad la da el
  material Mica y los bordes sutiles de las tarjetas — que es el enfoque
  correcto en Fluent.
- **Recomendación: mantenerlo así.** Fluent usa borde + material, no
  `box-shadow`. Las sombras difusas de estilo Material Design se ven fuera de
  lugar en Windows 11 y penalizan el rendimiento en WPF.
- Si aun así se quiere elevación, especificar: desplazamiento X/Y, radio de
  difuminado, color y opacidad, **por tema** (una sombra negra al 20 % es
  invisible en tema oscuro).

### 7.4 Espaciado

| Token | Valor actual | Dónde |
|---|---|---|
| `PagePadding` | **28,24,28,24** (izq, arriba, der, abajo) | Margen de contenido de las 7 páginas |
| Padding de tarjeta (default) | *WPF-UI* | Mayoría de las tarjetas |
| Padding de tarjeta de filtros | **14** uniforme | Gastos, Ingresos |
| Padding de tarjeta contenedora de lista | **4** uniforme | Categorías (la lista va al ras) |
| Padding de tarjeta de vista previa | **14** uniforme | Diálogo de Gasto |
| Padding del contenido de diálogo | **22** horizontal (`22,6,22,12` en Gasto) | Los 4 diálogos |
| Padding del pie de diálogo | **22,14** | Los 4 diálogos |
| Padding de fila de categoría | **12,10** | Categorías |
| Padding de botón de icono solo | **7** (tablas) · **8** (Categorías) · **9** (Personas) · **3** (quitar chip) | Varias |
| Separación entre tarjetas | **12–16 px** | Dashboard, Configuración |
| Separación entre tarjetas de persona | **14 px** | Personas |

> **A definir:** una escala de espaciado. Hoy conviven 2/3/4/5/6/7/8/9/10/12/14/
> 16/18/22/24/26/28 px. Sugerencia: base de 4 px → `4 · 8 · 12 · 16 · 24 · 32`.

### 7.5 Dimensiones de ventana

| Ventana | Ancho | Alto | Mínimo | Redimensionable |
|---|---|---|---|---|
| Principal | 1200 | 780 | 940 × 600 | Sí |
| Editar gasto | 580 | 760 | 520 × 600 | Sí |
| Editar categoría | 470 | Automático | — | No |
| Editar persona | 430 | Automático | — | No |
| Editar ingreso | 430 | Automático | — | No |

Anchos de contenido fijos: Reportes **760 px**, Configuración **780 px**,
tarjeta de persona **300 px**, rail de navegación **225 px**.

### 7.6 Alturas y tamaños de control

| Control | Valor |
|---|---|
| Botones, campos de texto, combos | *WPF-UI* (Fluent estándar: 32 px de alto) |
| Campo de notas (multilínea) | mín. **44 px** |
| Campo de palabras clave (multilínea) | mín. **60 px** |
| Chip cuadrado de categoría | **42 × 42 px** |
| Avatar de persona | **46 × 46 px** |
| Swatch de color | **26 × 26 px** |
| Swatch de emoji | **26 × 30 px** |
| Punto de color pequeño | **12 × 12 px** |
| Anillo de progreso (exportando) | **26 × 26 px** |
| Torta del Dashboard | mín. **300 px** de alto |
| Gráfico de columnas | mín. **260 px** de alto |

---

## 8. Especificaciones técnicas de los iconos

### 8.1 Formato preferido: SVG

| Requisito | Especificación |
|---|---|
| Lienzo | **24 × 24 px** |
| Atributo | `viewBox="0 0 24 24"` — obligatorio y exacto |
| Área segura | **2 px de margen** en los cuatro lados → **dibujo vivo de 20 × 20 px** centrado |
| Grosor de trazo | **2 px** (a escala 24) para el estilo lineal. Consistente en todo el set |
| Terminaciones | Extremos y uniones **redondeados** (`round`), coherentes en todo el set |
| Trazos | **Convertidos a contorno** (`Object > Path > Outline Stroke` / *expandir*). El SVG final debe contener solo elementos `<path>` con `fill`, **sin** atributos `stroke` |
| Color | **Un solo color**: `fill="currentColor"` (preferido) o `fill="#000000"`. Nunca colores múltiples ni degradados |
| Fondo | **Transparente.** Sin `<rect>` de fondo, ni siquiera invisible |
| Texto | **Sin elementos `<text>`.** Cualquier letra debe estar convertida a contorno |
| Prohibido | Máscaras, `clipPath` complejos, filtros, degradados, `<image>` embebida, capas ocultas, metadatos del editor |
| Optimización | Pasar por SVGO o "Guardar como SVG optimizado". Peso objetivo: **< 2 KB** por archivo |
| Precisión | Máximo 2 decimales en las coordenadas |
| Alineación | Alinear a la grilla de píxeles enteros donde sea posible |

Ejemplo de estructura válida:

```svg
<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 24" fill="currentColor">
  <path d="M4 6h16v2H4zM4 11h16v2H4zM4 16h10v2H4z"/>
</svg>
```

### 8.2 Alternativa: PNG transparente

Si no se puede entregar SVG, se aceptan PNG en tres densidades:

| Densidad | Tamaño | Sufijo del archivo |
|---|---|---|
| 1x | 24 × 24 px | `act-guardar.png` |
| 2x | 48 × 48 px | `act-guardar@2x.png` |
| 3x | 72 × 72 px | `act-guardar@3x.png` |

- PNG-24 con canal alfa (RGBA de 8 bits).
- **Fondo totalmente transparente.**
- Los iconos monocromos deben ser **blancos con alfa** o **negros con alfa** (para
  poder teñirse en runtime); si vienen en negro sólido no se pueden teñir para el
  tema oscuro.
- El 1x debe estar **retocado a mano**, no ser un downscale automático del 3x.

> **Limitación:** PNG no se tiñe tan bien como SVG en WPF. Además, los iconos que
> se usan a 40–44 px (estados vacíos) necesitarían un 4x adicional (96 px). **Se
> recomienda fuertemente el SVG.**

### 8.3 Alternativa: fuente de iconos (.ttf)

Es el mecanismo que usa la app hoy (los `SymbolIcon` de WPF-UI vienen de una
fuente de iconos). Si el diseñador prefiere este camino:

| Requisito | Especificación |
|---|---|
| Formato | **.ttf** (preferido en WPF) o `.otf` |
| Codepoints | Zona de uso privado: **U+E000 – U+F8FF** |
| Entregable adicional | Un **mapa de codepoints** en JSON o CSV: `nombre-del-icono, U+E001` para los 47 glifos |
| Alto de em | 1000 o 2048 unidades |
| Métricas | Los glifos deben estar diseñados sobre una caja de **24 × 24** escalada al em; alineados a la línea base de forma que un `FontSize="16"` dé un icono de 16 px |
| Ventajas | Se tiñen perfecto con `Foreground`, escalan sin pérdida, un solo archivo |
| Desventajas | Un solo color obligatorio; los iconos de categoría full color no entran |
| Licencia | Debe permitir embeber en aplicación de escritorio |

### 8.4 Iconos de categoría: glifo blanco (y por qué)

**Especificación:** los 24 iconos de categoría se dibujan **completamente en
blanco** (`#FFFFFF`), sin fondo propio.

**Por qué no llevan color propio:**

1. El **usuario elige el color de cada categoría** desde una paleta de 16 (y
   puede cambiarlo cuando quiera). Un icono con color fijo chocaría con el color
   elegido por el usuario.
2. El icono **siempre** se dibuja encima de un cuadrado redondeado de 42×42 px
   (radio 10 px) pintado con ese color. Es decir: el color **ya está** en el
   contenedor; el icono solo aporta la forma.
3. El mismo icono aparece en el desplegable de filtro de Gastos, donde también se
   necesita que funcione sobre el color de la categoría.
4. Un glifo blanco garantiza contraste contra los 16 colores de la paleta, que
   están todos calibrados para tener ≥ 3:1 contra blanco.

**Consecuencias de diseño:**

- El icono debe funcionar como **silueta sólida**: preferir formas rellenas o de
  trazo grueso, no líneas finas que se pierdan sobre color saturado.
- **No usar blanco como color de "hueco"** dentro del icono: los huecos deben ser
  transparentes para que se vea el color de fondo a través.
- Área segura: el dibujo vivo de 20×20 px debe caber cómodo dentro del cuadrado
  de 42 px (queda 11 px de aire por lado a tamaño de display de 20 px).

**Entrega:** SVG con `fill="currentColor"` (nosotros lo pintamos de blanco), o
PNG blanco con alfa.

---

## 9. Gráficos

La app usa **LiveCharts2** con dos gráficos en el Dashboard.

### 9.1 Torta "Gastos por categoría"

| Aspecto | Actual | A definir |
|---|---|---|
| Colores de las porciones | El color de cada categoría (elegido por el usuario) | Ver §5.7 — los 12 colores por defecto |
| Contorno de porción | Ninguno | ¿Agregar un contorno de 2 px del color de la superficie para separar porciones adyacentes? |
| Leyenda | A la derecha | Posición y estilo |
| Estado sin datos | Texto centrado "Sin gastos para mostrar" al 60 % de opacidad | |
| Alto mínimo | 300 px | |

### 9.2 Columnas "Gastos de los últimos 6 meses"

| Aspecto | Actual | A definir |
|---|---|---|
| Color de la serie | `#2563EB` (azul, serie única) | Color de serie simple, por tema |
| Radio de barra | **Rx = 5, Ry = 5** (esquinas redondeadas arriba y abajo) | Confirmar o cambiar |
| Grilla | **Sí**, solo horizontal (eje Y), negro al 8 % · el eje X **no** tiene grilla | Definir color por tema — hoy es negro fijo e invisible en tema oscuro |
| Etiquetas de eje | 12 px, meses abreviados capitalizados ("Ene", "Feb"…) | |
| Formato del eje Y | Símbolo de moneda + número con separador de miles, sin decimales | |
| Tooltip | Arriba de la barra | |
| Zoom | Desactivado | |
| Alto mínimo | 260 px | |

### 9.3 Requisitos de la paleta categórica de 12

| Requisito | Detalle |
|---|---|
| Distinguibilidad | Los 12 colores deben ser diferenciables **entre sí** en porciones adyacentes de la torta, incluso porciones chicas (< 5 %) |
| Daltonismo | Verificar con simuladores de **deuteranopía**, **protanopía** y **tritanopía**. Los pares críticos hoy: `#16A34A` verde vs `#65A30D` lima; `#DC2626` rojo vs `#EA580C` naranja; `#2563EB` azul vs `#4F46E5` índigo |
| Sobre fondo claro | Contraste ≥ 3:1 contra la superficie de tarjeta clara |
| Sobre fondo oscuro | Contraste ≥ 3:1 contra la superficie de tarjeta oscura. Puede requerir **variantes por tema** |
| Contra blanco | ≥ 3:1 — obligatorio porque el mismo color lleva un glifo blanco encima (§8.4) |
| Diferenciación no cromática | Idealmente los 12 también se distinguen por **luminancia**, no solo por matiz (así funcionan también en escala de grises / impresión en B&N del informe) |
| Orden | El orden importa: la torta asigna colores por categoría, y las categorías más usadas (Supermercado, Alquiler, Servicios) deberían tener los colores más distintivos |

---

## 10. Entrega

### 10.1 Estructura de archivos

Un archivo por icono, en un ZIP con esta estructura:

```
gastos-compartidos-diseno.zip
├── iconos/
│   ├── svg/
│   │   ├── nav-dashboard.svg
│   │   ├── nav-gastos.svg
│   │   ├── nav-ingresos.svg
│   │   ├── nav-personas.svg
│   │   ├── nav-categorias.svg
│   │   ├── nav-reportes.svg
│   │   ├── nav-configuracion.svg
│   │   ├── act-agregar.svg
│   │   ├── act-persona-agregar.svg
│   │   ├── act-editar.svg
│   │   ├── act-eliminar.svg
│   │   ├── act-guardar.svg
│   │   ├── act-actualizar.svg
│   │   ├── act-buscar.svg
│   │   ├── act-exportar.svg
│   │   ├── act-carpeta-abrir.svg
│   │   ├── act-quitar.svg
│   │   ├── act-info.svg
│   │   ├── act-flecha-derecha.svg
│   │   ├── act-check.svg
│   │   ├── act-persona.svg
│   │   ├── act-dinero.svg
│   │   └── cat-*.svg            (24 archivos)
│   └── png/                     (solo si no hay SVG: 1x, @2x, @3x)
├── app-icon/
│   ├── brand-app-1024.png
│   ├── brand-app-master.svg     (opcional)
│   └── brand-app-small.svg      (opcional)
├── tipografia/                  (opcional)
│   ├── *.ttf / *.otf
│   └── LICENCIA.txt
├── fondos/                      (opcional)
│   ├── fondo-claro.png
│   └── fondo-oscuro.png
└── tokens.md                    (o .json / .xlsx — la tabla de la §5 completa)
```

### 10.2 Reglas de nombres

- **kebab-case**, todo en minúsculas.
- **Sin tildes ni ñ** en los nombres de archivo (`cat-educacion.svg`, no
  `cat-educación.svg`).
- Sin espacios, sin guiones bajos, sin números de versión en el nombre
  (`act-guardar.svg`, no `act-guardar-v2-final.svg`).
- Los nombres de esta especificación son **exactos**: si cambian, hay que
  cambiarlos también en el código.

### 10.3 Checklist final

Para tildar antes de entregar.

**Iconos de interfaz (23)**

- [ ] Los 7 iconos de navegación entregados con los nombres exactos
- [ ] Los 15 iconos de acción entregados con los nombres exactos
- [ ] El icono de marca de la barra de título entregado
- [ ] Todos en SVG con `viewBox="0 0 24 24"`
- [ ] Todos con área segura de 2 px (dibujo vivo 20×20)
- [ ] Todos con trazos convertidos a contorno (sin atributo `stroke`)
- [ ] Todos en un solo color (`currentColor` o `#000`)
- [ ] Todos con fondo transparente y sin elementos `<text>`
- [ ] Todos optimizados (< 2 KB cada uno)
- [ ] Los que se usan a 16 px verificados a 16 px (`act-*`, KPIs)
- [ ] Los que se usan a 13 px verificados a 13 px (`act-quitar`, `act-flecha-derecha`)
- [ ] Los que se usan a 40–44 px verificados a ese tamaño (`nav-gastos`, `nav-ingresos`, `nav-personas`)
- [ ] `act-eliminar` verificado en blanco sobre fondo rojo
- [ ] `act-guardar` y `act-agregar` verificados en blanco sobre el acento
- [ ] `act-dinero`, `nav-ingresos` y `nav-gastos` son claramente distinguibles entre sí

**Iconos de categoría (24)**

- [ ] Los 12 por defecto entregados
- [ ] Los 12 del catálogo elegible entregados
- [ ] Todos funcionan como silueta blanca sólida
- [ ] Verificados sobre los 16 colores de la paleta de personas
- [ ] Verificados a 20 px de tamaño de display

**Icono de aplicación**

- [ ] `brand-app-1024.png` entregado, 1024×1024, RGBA, fondo transparente
- [ ] Verificado reducido a 16×16 (sigue siendo reconocible)
- [ ] Verificado sobre fondo de escritorio claro y oscuro
- [ ] Sin texto (o un solo carácter)
- [ ] Márgenes de ~8 % respetados

**Tokens de tema**

- [ ] Acento + hover + pressed + texto-sobre-acento, para claro y oscuro
- [ ] Fondo de ventana, superficie de tarjeta, superficie secundaria, para claro y oscuro
- [ ] Texto primario / secundario / terciario / deshabilitado, para claro y oscuro
- [ ] Borde default y borde sutil, para claro y oscuro
- [ ] Semánticos éxito / peligro / advertencia / info, **con variante para tema oscuro**
- [ ] Anillo de foco definido y verificado sobre todas las superficies
- [ ] Paleta data-viz de 12 colores, verificada contra daltonismo
- [ ] Paleta de personas de 16 colores, todos con ≥ 3:1 contra blanco
- [ ] Colores del informe de Excel definidos
- [ ] Color de la grilla del gráfico definido **por tema**

**Tipografía**

- [ ] Fuente entregada en .ttf/.otf con los 4 pesos (Regular, Medium, SemiBold, Bold)
- [ ] Licencia de embebido en escritorio verificada y adjuntada
- [ ] Cifras tabulares confirmadas
- [ ] Cobertura de `á é í ó ú ü ñ Ñ ¿ ¡ « »` verificada
- [ ] Símbolo `$` y otros símbolos de moneda verificados
- [ ] Escala tipográfica consolidada propuesta

**Forma y espaciado**

- [ ] Escala de radios definida
- [ ] Escala de espaciado definida
- [ ] Decisión sobre sombras (sí/no; si sí, specs por tema)
- [ ] Decisión sobre el material de ventana (Mica / Acrylic / sólido)

**Fondos (si aplica)**

- [ ] Versión clara y versión oscura
- [ ] Contraste de texto ≥ 4.5:1 verificado en el punto más desfavorable
- [ ] Verificado a 940×600 (mínimo) y a pantalla completa
- [ ] Peso < 400 KB

**General**

- [ ] Todos los nombres de archivo en kebab-case, sin tildes
- [ ] Todo verificado en **tema claro y tema oscuro**
- [ ] Todo verificado a **100 %, 150 % y 200 %** de escala de Windows
- [ ] ZIP armado con la estructura de la §10.1

---

## Apéndice — Archivos fuente de referencia

Para consultar los valores originales:

| Qué | Archivo |
|---|---|
| Estilos tipográficos, `PagePadding`, plantillas de swatch | `App.xaml` |
| Barra de título, rail de navegación, tamaño de ventana, material | `MainWindow.xaml` |
| Colores semánticos (éxito / peligro / neutro) | `Converters/AppConverters.cs` |
| Categorías por defecto: nombre, emoji, color | `Data/DbSeeder.cs` |
| Paleta de 16 colores y catálogo de 24 emojis | `Helpers/Palette.cs` |
| Colores y paints de los gráficos | `ViewModels/DashboardViewModel.cs` |
| Colores del informe de Excel | `Services/ExcelExportService.cs` |
| Iconos, tamaños y radios de cada pantalla | `Views/Pages/*.xaml` |
| Diálogos: material, tamaños, pie | `Views/Dialogs/*.xaml` |
