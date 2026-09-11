using GastosCompartidos.Data;
using GastosCompartidos.Helpers;
using GastosCompartidos.Models;
using Microsoft.EntityFrameworkCore;

namespace GastosCompartidos.Services;

public interface ISettingsService
{
    string CurrencySymbol { get; set; }
    int Decimals { get; set; }
    AppThemeMode Theme { get; set; }
    SplitType DefaultSplitType { get; set; }

    /// <summary>Lee la configuración desde la base y la aplica al formateador de moneda.</summary>
    void Load();
}

/// <summary>
/// Persiste la configuración en la tabla de pares clave-valor y la mantiene en
/// memoria para acceso rápido.
/// </summary>
public class SettingsService : ISettingsService
{
    private const string KeyCurrency = "CurrencySymbol";
    private const string KeyDecimals = "Decimals";
    private const string KeyTheme = "Theme";
    private const string KeyDefaultSplit = "DefaultSplitType";

    private readonly IDbContextFactory<AppDbContext> _factory;

    private string _currencySymbol = "$";
    private int _decimals = 2;
    private AppThemeMode _theme = AppThemeMode.System;
    private SplitType _defaultSplit = SplitType.Equal;

    public SettingsService(IDbContextFactory<AppDbContext> factory) => _factory = factory;

    public string CurrencySymbol
    {
        get => _currencySymbol;
        set { _currencySymbol = string.IsNullOrWhiteSpace(value) ? "$" : value.Trim(); Set(KeyCurrency, _currencySymbol); ApplyToFormatter(); }
    }

    public int Decimals
    {
        get => _decimals;
        set { _decimals = Math.Clamp(value, 0, 4); Set(KeyDecimals, _decimals.ToString()); ApplyToFormatter(); }
    }

    public AppThemeMode Theme
    {
        get => _theme;
        set { _theme = value; Set(KeyTheme, value.ToString()); }
    }

    public SplitType DefaultSplitType
    {
        get => _defaultSplit;
        set { _defaultSplit = value; Set(KeyDefaultSplit, value.ToString()); }
    }

    public void Load()
    {
        using var db = _factory.CreateDbContext();
        var all = db.Settings.AsNoTracking().ToDictionary(s => s.Key, s => s.Value);

        if (all.TryGetValue(KeyCurrency, out var cur) && !string.IsNullOrWhiteSpace(cur)) _currencySymbol = cur;
        if (all.TryGetValue(KeyDecimals, out var dec) && int.TryParse(dec, out var d)) _decimals = Math.Clamp(d, 0, 4);
        if (all.TryGetValue(KeyTheme, out var th) && Enum.TryParse<AppThemeMode>(th, out var theme)) _theme = theme;
        if (all.TryGetValue(KeyDefaultSplit, out var sp) && Enum.TryParse<SplitType>(sp, out var split)) _defaultSplit = split;

        ApplyToFormatter();
    }

    private void ApplyToFormatter()
    {
        CurrencyFormatter.Symbol = _currencySymbol;
        CurrencyFormatter.Decimals = _decimals;
    }

    private void Set(string key, string value)
    {
        using var db = _factory.CreateDbContext();
        var setting = db.Settings.FirstOrDefault(s => s.Key == key);
        if (setting is null)
            db.Settings.Add(new AppSetting { Key = key, Value = value });
        else
            setting.Value = value;
        db.SaveChanges();
    }
}
