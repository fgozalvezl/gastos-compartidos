using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using GastosCompartidos.Models;

namespace GastosCompartidos.ViewModels;

/// <summary>Edición/alta de un ingreso.</summary>
public partial class IncomeEditViewModel : ObservableObject
{
    public int Id { get; }
    public string Title { get; }
    public ObservableCollection<Person> People { get; }
    public ObservableCollection<IncomeType> IncomeTypes { get; }

    [ObservableProperty] private DateTime _date = DateTime.Today;
    [ObservableProperty] private Person? _selectedPerson;
    [ObservableProperty] private IncomeType? _selectedType;
    [ObservableProperty] private decimal _amount;
    [ObservableProperty] private string? _description;
    [ObservableProperty] private string? _error;

    public IncomeEditViewModel(Income? existing, IEnumerable<Person> people, IEnumerable<IncomeType> types)
    {
        People = new ObservableCollection<Person>(people);
        IncomeTypes = new ObservableCollection<IncomeType>(types);

        if (existing is null)
        {
            Title = "Nuevo ingreso";
            SelectedPerson = People.FirstOrDefault();
            SelectedType = IncomeTypes.FirstOrDefault();
        }
        else
        {
            Id = existing.Id;
            Title = "Editar ingreso";
            Date = existing.Date;
            Amount = existing.Amount;
            Description = existing.Description;
            SelectedPerson = People.FirstOrDefault(p => p.Id == existing.PersonId);
            SelectedType = IncomeTypes.FirstOrDefault(t => t.Id == existing.IncomeTypeId);
        }
    }

    public bool Validate()
    {
        if (SelectedPerson is null) { Error = "Elegí una persona."; return false; }
        if (SelectedType is null) { Error = "Elegí un tipo de ingreso."; return false; }
        if (Amount <= 0) { Error = "El monto debe ser mayor a 0."; return false; }
        Error = null;
        return true;
    }
}
