using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using GastosCompartidos.Models;
using GastosCompartidos.Services;

namespace GastosCompartidos.ViewModels;

/// <summary>Una fila de participante dentro del editor de gasto.</summary>
public partial class ParticipantRow : ObservableObject
{
    public Person Person { get; }
    public string Name => Person.Name;
    public string ColorHex => Person.ColorHex;
    public decimal Income => Person.MonthlyIncome;

    [ObservableProperty] private bool _isIncluded = true;
    [ObservableProperty] private double _weight = 1;
    [ObservableProperty] private decimal _shareAmount;

    public ParticipantRow(Person person) => Person = person;
}

/// <summary>Edición/alta de un gasto con cálculo de división en vivo.</summary>
public partial class ExpenseEditViewModel : ObservableObject
{
    private readonly ISplitCalculator _calc;
    private readonly ICategorizationService _categorizer;

    private bool _applyingSuggestion;
    private CancellationTokenSource? _suggestCts;

    public int Id { get; }
    public string Title { get; }
    public ObservableCollection<Category> Categories { get; }
    public IReadOnlyList<Person> PeopleSource { get; }
    public ObservableCollection<ParticipantRow> Participants { get; } = new();

    [ObservableProperty] private DateTime _date = DateTime.Today;
    [ObservableProperty] private string _description = "";
    [ObservableProperty] private decimal _amount;
    [ObservableProperty] private Category? _selectedCategory;
    [ObservableProperty] private Person? _paidBy;
    [ObservableProperty] private SplitType _splitType = SplitType.Equal;
    [ObservableProperty] private string? _notes;
    [ObservableProperty] private string? _error;
    [ObservableProperty] private decimal _previewTotal;
    [ObservableProperty] private string? _suggestionInfo;

    public bool CategoryManuallySet { get; private set; }
    public bool IsCustom => SplitType == SplitType.Custom;

    public ExpenseEditViewModel(
        Expense? existing,
        IEnumerable<Person> people,
        IEnumerable<Category> categories,
        ISplitCalculator calc,
        ICategorizationService categorizer,
        SplitType defaultSplit)
    {
        _calc = calc;
        _categorizer = categorizer;
        Categories = new ObservableCollection<Category>(categories);
        PeopleSource = people.ToList();

        if (existing is null)
        {
            Title = "Nuevo gasto";
            SplitType = defaultSplit;
            PaidBy = PeopleSource.FirstOrDefault(p => p.IsActive) ?? PeopleSource.FirstOrDefault();
            foreach (var p in PeopleSource)
                AddRow(p, included: p.IsActive, weight: 1);
        }
        else
        {
            Id = existing.Id;
            Title = "Editar gasto";
            Date = existing.Date;
            Description = existing.Description;
            Amount = existing.Amount;
            Notes = existing.Notes;
            SplitType = existing.SplitType;
            CategoryManuallySet = true;
            SelectedCategory = Categories.FirstOrDefault(c => c.Id == existing.CategoryId);
            PaidBy = PeopleSource.FirstOrDefault(p => p.Id == existing.PaidByPersonId);

            var shareByPerson = existing.Shares.ToDictionary(s => s.PersonId);
            foreach (var p in PeopleSource)
            {
                bool included = shareByPerson.ContainsKey(p.Id);
                double weight = included && shareByPerson[p.Id].Weight > 0 ? shareByPerson[p.Id].Weight : 1;
                AddRow(p, included, weight);
            }
        }

        Recompute();
    }

    private void AddRow(Person p, bool included, double weight)
    {
        var row = new ParticipantRow(p) { IsIncluded = included, Weight = weight <= 0 ? 1 : weight };
        row.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName is nameof(ParticipantRow.IsIncluded) or nameof(ParticipantRow.Weight))
                Recompute();
        };
        Participants.Add(row);
    }

    partial void OnAmountChanged(decimal value) => Recompute();

    partial void OnSplitTypeChanged(SplitType value)
    {
        OnPropertyChanged(nameof(IsCustom));
        Recompute();
    }

    partial void OnSelectedCategoryChanged(Category? value)
    {
        if (!_applyingSuggestion) CategoryManuallySet = true;
    }

    partial void OnDescriptionChanged(string value) => TrySuggestCategory(value);

    private void Recompute()
    {
        var input = Participants
            .Select(r => new SplitParticipant(r.Person.Id, r.IsIncluded, r.Weight, r.Person.MonthlyIncome))
            .ToList();

        var shares = _calc.Calculate(Amount, SplitType, input);
        var map = shares.ToDictionary(s => s.PersonId, s => s.Amount);

        foreach (var r in Participants)
            r.ShareAmount = r.IsIncluded ? map.GetValueOrDefault(r.Person.Id) : 0m;

        PreviewTotal = Participants.Sum(r => r.ShareAmount);
    }

    private async void TrySuggestCategory(string description)
    {
        if (CategoryManuallySet) return;

        string text = description?.Trim() ?? "";
        if (text.Length < 3) return;

        _suggestCts?.Cancel();
        var cts = _suggestCts = new CancellationTokenSource();
        try
        {
            await Task.Delay(350, cts.Token);
            int? catId = await _categorizer.SuggestCategoryIdAsync(text, cts.Token);
            if (cts.Token.IsCancellationRequested || catId is null) return;

            var cat = Categories.FirstOrDefault(c => c.Id == catId.Value);
            if (cat is null) return;

            _applyingSuggestion = true;
            SelectedCategory = cat;
            _applyingSuggestion = false;
            SuggestionInfo = $"✨ Categoría sugerida: {cat.Name}";
        }
        catch (OperationCanceledException) { /* descartado por una entrada más nueva */ }
    }

    public bool Validate()
    {
        if (string.IsNullOrWhiteSpace(Description)) { Error = "Ingresá una descripción."; return false; }
        if (Amount <= 0) { Error = "El monto debe ser mayor a 0."; return false; }
        if (Amount > SplitCalculator.MaxAmount)
        {
            Error = $"El monto es demasiado grande (máximo {SplitCalculator.MaxAmount:N0}). Revisá si sobra algún cero.";
            return false;
        }
        if (SelectedCategory is null) { Error = "Elegí una categoría."; return false; }
        if (PaidBy is null) { Error = "Indicá quién pagó."; return false; }
        if (!Participants.Any(r => r.IsIncluded)) { Error = "Elegí al menos un participante."; return false; }
        if (SplitType == SplitType.Custom && Participants.Where(r => r.IsIncluded).Sum(r => r.Weight) <= 0)
        {
            Error = "En división personalizada, asigná pesos mayores a 0.";
            return false;
        }
        Error = null;
        return true;
    }

    /// <summary>Devuelve las partes finales calculadas para persistir.</summary>
    public IReadOnlyList<(int PersonId, decimal Amount, double Weight)> GetShares()
    {
        var input = Participants
            .Select(r => new SplitParticipant(r.Person.Id, r.IsIncluded, r.Weight, r.Person.MonthlyIncome))
            .ToList();

        return _calc.Calculate(Amount, SplitType, input)
            .Select(s => (s.PersonId, s.Amount, s.Weight))
            .ToList();
    }
}
