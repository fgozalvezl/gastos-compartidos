using CommunityToolkit.Mvvm.ComponentModel;
using GastosCompartidos.Helpers;
using GastosCompartidos.Models;

namespace GastosCompartidos.ViewModels;

/// <summary>Edición/alta de una persona (participante).</summary>
public partial class PersonEditViewModel : ObservableObject
{
    public int Id { get; }
    public string Title { get; }
    public string[] ColorOptions => Palette.Colors;

    [ObservableProperty] private string _name = "";
    [ObservableProperty] private decimal _monthlyIncome;
    [ObservableProperty] private string _colorHex = "#2563EB";
    [ObservableProperty] private bool _isActive = true;
    [ObservableProperty] private string? _error;

    public PersonEditViewModel(Person? existing)
    {
        if (existing is null)
        {
            Title = "Nueva persona";
            ColorHex = Palette.Colors[0];
        }
        else
        {
            Id = existing.Id;
            Title = "Editar persona";
            Name = existing.Name;
            MonthlyIncome = existing.MonthlyIncome;
            ColorHex = existing.ColorHex;
            IsActive = existing.IsActive;
        }
    }

    public bool Validate()
    {
        if (string.IsNullOrWhiteSpace(Name)) { Error = "Ingresá un nombre."; return false; }
        if (MonthlyIncome < 0) { Error = "El ingreso no puede ser negativo."; return false; }
        Error = null;
        return true;
    }
}
