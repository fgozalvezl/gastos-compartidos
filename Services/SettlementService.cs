using GastosCompartidos.Models;

namespace GastosCompartidos.Services;

/// <summary>Balance de una persona: cuánto pagó, cuánto le correspondía y la diferencia.</summary>
public record PersonBalance(int PersonId, string Name, string ColorHex, decimal Paid, decimal Owed)
{
    /// <summary>Positivo: le deben (pagó de más). Negativo: debe.</summary>
    public decimal Balance => Paid - Owed;
}

/// <summary>Una transferencia sugerida para saldar cuentas.</summary>
public record SettlementTransfer(int FromPersonId, string FromName, int ToPersonId, string ToName, decimal Amount);

public interface ISettlementService
{
    IReadOnlyList<PersonBalance> CalculateBalances(IEnumerable<Person> people, IEnumerable<Expense> expenses);
    IReadOnlyList<SettlementTransfer> CalculateSettlement(IEnumerable<PersonBalance> balances);
}

/// <summary>
/// Calcula los balances por persona y la liquidación con la menor cantidad de
/// transferencias posible (algoritmo voraz: el que más debe le paga al que más le deben).
/// </summary>
public class SettlementService : ISettlementService
{
    public IReadOnlyList<PersonBalance> CalculateBalances(IEnumerable<Person> people, IEnumerable<Expense> expenses)
    {
        var expenseList = expenses.ToList();

        var paid = expenseList
            .GroupBy(e => e.PaidByPersonId)
            .ToDictionary(g => g.Key, g => g.Sum(e => e.Amount));

        var owed = expenseList
            .SelectMany(e => e.Shares)
            .GroupBy(s => s.PersonId)
            .ToDictionary(g => g.Key, g => g.Sum(s => s.Amount));

        var balances = new List<PersonBalance>();
        foreach (var p in people)
        {
            decimal paidByP = paid.GetValueOrDefault(p.Id);
            decimal owedByP = owed.GetValueOrDefault(p.Id);
            balances.Add(new PersonBalance(p.Id, p.Name, p.ColorHex, paidByP, owedByP));
        }

        return balances;
    }

    public IReadOnlyList<SettlementTransfer> CalculateSettlement(IEnumerable<PersonBalance> balances)
    {
        // Listas mutables de deudores y acreedores (en centavos para evitar errores de redondeo).
        var creditors = balances
            .Where(b => b.Balance > 0)
            .Select(b => new MutableBalance(b.PersonId, b.Name, (long)Math.Round(b.Balance * 100m)))
            .Where(b => b.Cents > 0)
            .OrderByDescending(b => b.Cents)
            .ToList();

        var debtors = balances
            .Where(b => b.Balance < 0)
            .Select(b => new MutableBalance(b.PersonId, b.Name, (long)Math.Round(-b.Balance * 100m)))
            .Where(b => b.Cents > 0)
            .OrderByDescending(b => b.Cents)
            .ToList();

        var transfers = new List<SettlementTransfer>();

        int ci = 0, di = 0;
        while (ci < creditors.Count && di < debtors.Count)
        {
            var creditor = creditors[ci];
            var debtor = debtors[di];

            long amount = Math.Min(creditor.Cents, debtor.Cents);
            if (amount > 0)
            {
                transfers.Add(new SettlementTransfer(
                    debtor.PersonId, debtor.Name,
                    creditor.PersonId, creditor.Name,
                    amount / 100m));

                creditor.Cents -= amount;
                debtor.Cents -= amount;
            }

            if (creditor.Cents == 0) ci++;
            if (debtor.Cents == 0) di++;
        }

        return transfers;
    }

    private sealed class MutableBalance(int personId, string name, long cents)
    {
        public int PersonId { get; } = personId;
        public string Name { get; } = name;
        public long Cents { get; set; } = cents;
    }
}
