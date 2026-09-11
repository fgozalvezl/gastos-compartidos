using System.Globalization;
using System.IO;
using System.Windows;
using System.Windows.Markup;
using System.Windows.Threading;
using GastosCompartidos.Data;
using GastosCompartidos.Models;
using GastosCompartidos.Services;
using GastosCompartidos.ViewModels;
using GastosCompartidos.Views.Dialogs;
using GastosCompartidos.Views.Pages;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Win32;
using Wpf.Ui.Appearance;

namespace GastosCompartidos;

public partial class App : Application
{
    /// <summary>Contenedor de servicios accesible desde toda la aplicación.</summary>
    public static IServiceProvider Services { get; private set; } = null!;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        // Los Binding convierten con FrameworkElement.Language (en-US por defecto), no con
        // CurrentCulture: sin esto un monto escrito como "1500,50" se guarda como 150050.
        // Debe ejecutarse antes de crear cualquier FrameworkElement.
        FrameworkElement.LanguageProperty.OverrideMetadata(
            typeof(FrameworkElement),
            new FrameworkPropertyMetadata(
                XmlLanguage.GetLanguage(CultureInfo.CurrentCulture.IetfLanguageTag)));

        DispatcherUnhandledException += OnUnhandledException;

        var services = new ServiceCollection();
        ConfigureServices(services);
        Services = services.BuildServiceProvider();

        // Crear y poblar la base de datos si hace falta.
        var factory = Services.GetRequiredService<IDbContextFactory<AppDbContext>>();
        using (var db = factory.CreateDbContext())
        {
            db.Database.EnsureCreated();
            DbSeeder.Seed(db);
        }

        // Cargar configuración y aplicar tema.
        var settings = Services.GetRequiredService<ISettingsService>();
        settings.Load();
        ApplyTheme(settings.Theme);

        var main = Services.GetRequiredService<MainWindow>();
        MainWindow = main;
        main.Show();
    }

    private static void ConfigureServices(IServiceCollection services)
    {
        string dir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "GastosCompartidos");
        Directory.CreateDirectory(dir);
        string dbPath = Path.Combine(dir, "gastos.db");

        services.AddDbContextFactory<AppDbContext>(o => o.UseSqlite($"Data Source={dbPath}"));

        // Servicios
        services.AddSingleton<ISplitCalculator, SplitCalculator>();
        services.AddSingleton<ISettlementService, SettlementService>();
        services.AddSingleton<IExcelExportService, ExcelExportService>();
        services.AddSingleton<ICategorizationService, CategorizationService>();
        services.AddSingleton<ISettingsService, SettingsService>();
        services.AddSingleton<IDialogService, DialogService>();

        // ViewModels de página
        services.AddSingleton<DashboardViewModel>();
        services.AddSingleton<ExpensesViewModel>();
        services.AddSingleton<PeopleViewModel>();
        services.AddSingleton<CategoriesViewModel>();
        services.AddSingleton<IncomesViewModel>();
        services.AddSingleton<ReportsViewModel>();
        services.AddSingleton<SettingsViewModel>();

        // Páginas
        services.AddSingleton<DashboardPage>();
        services.AddSingleton<ExpensesPage>();
        services.AddSingleton<PeoplePage>();
        services.AddSingleton<CategoriesPage>();
        services.AddSingleton<IncomesPage>();
        services.AddSingleton<ReportsPage>();
        services.AddSingleton<SettingsPage>();

        services.AddSingleton<MainWindow>();
    }

    /// <summary>Aplica el tema claro/oscuro (o el del sistema).</summary>
    public static void ApplyTheme(AppThemeMode mode)
    {
        ApplicationTheme target = mode switch
        {
            AppThemeMode.Light => ApplicationTheme.Light,
            AppThemeMode.Dark => ApplicationTheme.Dark,
            _ => SystemUsesLightTheme() ? ApplicationTheme.Light : ApplicationTheme.Dark
        };
        ApplicationThemeManager.Apply(target, updateAccent: true);
    }

    private static bool SystemUsesLightTheme()
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(
                @"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize");
            return key?.GetValue("AppsUseLightTheme") is int v ? v != 0 : true;
        }
        catch
        {
            return true;
        }
    }

    private void OnUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
    {
        MessageBox.Show(
            $"Ocurrió un error inesperado:\n\n{e.Exception.Message}",
            "Gastos Compartidos",
            MessageBoxButton.OK,
            MessageBoxImage.Error);
        e.Handled = true;
    }
}
