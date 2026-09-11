namespace GastosCompartidos.Models;

/// <summary>
/// Par clave-valor para guardar la configuración de la aplicación
/// (moneda, tema, tipo de división por defecto, etc.).
/// </summary>
public class AppSetting
{
    public int Id { get; set; }

    public string Key { get; set; } = string.Empty;

    public string Value { get; set; } = string.Empty;
}
