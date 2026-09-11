# 💸 Gastos Compartidos

Aplicación de escritorio para Windows que permite a 2 o más personas dividir
gastos de forma justa, con un diseño elegante estilo Windows 11.

Hecha con **.NET 8 + WPF**, funciona **100 % offline** y guarda los datos
localmente en tu PC.

## ✨ Características

- **Personas y participantes**: cada una con su color e *ingreso de referencia*.
- **División de gastos** de 3 formas:
  - **Partes iguales**
  - **Proporcional al ingreso** de cada persona
  - **Personalizada** (con pesos definidos por vos)
  - …con reparto exacto de centavos (las partes siempre suman el total).
- **Categorías inteligentes**: al escribir la descripción de un gasto, la app
  sugiere la categoría según palabras clave **y aprende de tus correcciones**.
- **Dashboard** con gráfico de torta por categoría, evolución mensual y KPIs.
- **Liquidación automática**: calcula "quién le debe a quién" con la menor
  cantidad de transferencias.
- **Ingresos** por persona y tipo (sueldo, freelance, etc.).
- **Exportación a Excel** con informe detallado (Resumen, Gastos con la parte de
  cada persona, Por categoría e Ingresos) — sin necesidad de tener Office.
- **Tema claro / oscuro** (o automático según Windows) y **moneda configurable**.

## ▶️ Cómo ejecutar

Necesitás el **SDK de .NET 8** (ya instalado en este equipo).

```powershell
# Desde la carpeta del proyecto
dotnet run
```

O ejecutá directamente el binario ya compilado:

```
bin\Debug\net8.0-windows\GastosCompartidos.exe
```

## 📦 Generar un ejecutable distribuible

Para crear un `.exe` autónomo (no requiere .NET instalado en la otra PC):

```powershell
dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true
```

El ejecutable queda en `bin\Release\net8.0-windows\win-x64\publish\`.

> Si la PC de destino ya tiene el runtime de .NET 8 Desktop, podés usar
> `--self-contained false` para un archivo mucho más liviano.

## 💾 ¿Dónde se guardan los datos?

En una base SQLite local:

```
%LocalAppData%\GastosCompartidos\gastos.db
```

Podés abrir esa carpeta desde **Configuración → Abrir carpeta de datos**.
La primera vez se cargan datos de ejemplo (Ana y Bruno); podés borrarlos desde
**Configuración → Borrar todos los datos**.

## 🧱 Arquitectura

Patrón **MVVM**. Estructura del código:

| Carpeta        | Contenido                                                        |
|----------------|------------------------------------------------------------------|
| `Models/`      | Entidades (Persona, Gasto, ParteGasto, Categoría, Ingreso, …)    |
| `Data/`        | `AppDbContext` (EF Core + SQLite) y datos iniciales (`DbSeeder`) |
| `Services/`    | Cálculo de división, liquidación, categorización, export Excel   |
| `ViewModels/`  | Lógica de presentación (CommunityToolkit.Mvvm)                   |
| `Views/`       | Páginas y diálogos (XAML)                                        |
| `Converters/`  | Conversores de la interfaz                                       |
| `Helpers/`     | Utilidades (normalización de texto, formato de moneda, paletas)  |

### Librerías

- [WPF-UI](https://github.com/lepoco/wpfui) — diseño Fluent / Windows 11
- [LiveCharts2](https://livecharts.dev/) — gráficos
- [Entity Framework Core](https://learn.microsoft.com/ef/core/) + SQLite — datos
- [ClosedXML](https://github.com/ClosedXML/ClosedXML) — exportación a Excel
- [CommunityToolkit.Mvvm](https://learn.microsoft.com/dotnet/communitytoolkit/mvvm/) — MVVM
