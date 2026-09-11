using GastosCompartidos.Models;

namespace GastosCompartidos.Services;

/// <summary>Datos de entrada de un participante para el cálculo de división.</summary>
public record SplitParticipant(int PersonId, bool Included, double Weight, decimal Income);

/// <summary>Resultado: cuánto le toca a cada persona.</summary>
public record SplitResult(int PersonId, decimal Amount, double Weight);

public interface ISplitCalculator
{
    /// <summary>
    /// Calcula la parte que le corresponde a cada participante según el tipo de
    /// división. Garantiza que la suma de las partes sea exactamente el total
    /// (reparte los centavos sobrantes por el método del resto mayor).
    /// Devuelve una lista vacía si el monto supera <see cref="SplitCalculator.MaxAmount"/>.
    /// </summary>
    IReadOnlyList<SplitResult> Calculate(decimal totalAmount, SplitType splitType, IReadOnlyList<SplitParticipant> participants);
}

public class SplitCalculator : ISplitCalculator
{
    /// <summary>
    /// Monto máximo admitido: por encima, el reparto en centavos desbordaría Int64.
    /// La validación del editor de gasto usa este mismo tope.
    /// </summary>
    public const decimal MaxAmount = 999_999_999_999m;

    public IReadOnlyList<SplitResult> Calculate(decimal totalAmount, SplitType splitType, IReadOnlyList<SplitParticipant> participants)
    {
        var included = participants.Where(p => p.Included).ToList();
        var result = new List<SplitResult>(included.Count);

        if (included.Count == 0 || totalAmount > MaxAmount)
            return result;

        // Pesos según el tipo de división.
        var weights = new double[included.Count];
        for (int i = 0; i < included.Count; i++)
        {
            weights[i] = splitType switch
            {
                SplitType.Equal => 1.0,
                SplitType.Proportional => (double)included[i].Income,
                SplitType.Custom => included[i].Weight,
                _ => 1.0
            };
            // NaN e infinitos no son pesos válidos (NaN < 0 es false, así que hay que descartarlos aparte).
            if (!double.IsFinite(weights[i]) || weights[i] < 0) weights[i] = 0;
        }

        double sumW = weights.Sum();
        if (!double.IsFinite(sumW) || sumW <= 0) // sin pesos válidos -> partes iguales
        {
            for (int i = 0; i < weights.Length; i++) weights[i] = 1.0;
            sumW = weights.Length;
        }

        if (totalAmount <= 0)
        {
            for (int i = 0; i < included.Count; i++)
                result.Add(new SplitResult(included[i].PersonId, 0m, weights[i]));
            return result;
        }

        // Reparto en centavos con método del resto mayor (las partes suman el total exacto).
        long totalCents = (long)Math.Round(totalAmount * 100m, MidpointRounding.AwayFromZero);
        var cents = new long[included.Count];
        var frac = new double[included.Count];
        long allocated = 0;

        for (int i = 0; i < included.Count; i++)
        {
            double exact = totalCents * weights[i] / sumW;
            long floor = (long)Math.Floor(exact);
            cents[i] = floor;
            frac[i] = exact - floor;
            allocated += floor;
        }

        long remainder = totalCents - allocated;
        foreach (int idx in Enumerable.Range(0, included.Count)
                     .OrderByDescending(i => frac[i])
                     .ThenByDescending(i => weights[i])
                     .Take((int)Math.Max(0, remainder)))
        {
            cents[idx] += 1;
        }

        for (int i = 0; i < included.Count; i++)
            result.Add(new SplitResult(included[i].PersonId, cents[i] / 100m, weights[i]));

        return result;
    }
}
