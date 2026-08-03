using System;
using System.Collections.Generic;
using System.Linq;

[Serializable]
public class OrderingConstraint
{
    public string type;
    public string first;
    public string second;
    public string text;

    public bool IsSatisfied(IReadOnlyList<string> order)
    {
        int firstIndex = IndexOf(order, first);
        int secondIndex = IndexOf(order, second);
        if (firstIndex < 0 || secondIndex < 0)
        {
            return false;
        }

        return type == "immediately_before"
            ? secondIndex == firstIndex + 1
            : firstIndex < secondIndex;
    }

    private static int IndexOf(IReadOnlyList<string> values, string target)
    {
        for (int i = 0; i < values.Count; i++)
        {
            if (string.Equals(values[i], target, StringComparison.Ordinal))
            {
                return i;
            }
        }
        return -1;
    }
}

public sealed class ProceduralOrderingPuzzle
{
    public string PuzzleId { get; }
    public string[] Symbols { get; }
    public OrderingConstraint[] Constraints { get; }
    public string[] Solution { get; }
    public int Seed { get; }

    public ProceduralOrderingPuzzle(
        string puzzleId,
        string[] symbols,
        OrderingConstraint[] constraints,
        string[] solution,
        int seed)
    {
        PuzzleId = puzzleId;
        Symbols = symbols;
        Constraints = constraints;
        Solution = solution;
        Seed = seed;
    }

    public bool IsCorrect(IReadOnlyList<string> attempt)
    {
        return attempt != null && attempt.SequenceEqual(Solution);
    }
}

public static class ProceduralOrderingPuzzleGenerator
{
    private static readonly string[] SymbolPool =
    {
        "flower", "stone", "lantern", "carrot", "watering can",
        "mushroom", "acorn", "feather", "leaf", "berry"
    };

    public static ProceduralOrderingPuzzle Generate(int symbolCount, int seed)
    {
        if (symbolCount < 3 || symbolCount > 5)
        {
            throw new ArgumentOutOfRangeException(nameof(symbolCount), "Ordering puzzles support 3 to 5 symbols.");
        }

        Random random = new Random(seed);
        string[] symbols = Shuffle(SymbolPool, random).Take(symbolCount).ToArray();
        string[] solution = Shuffle(symbols, random);
        List<OrderingConstraint> constraints = new List<OrderingConstraint>();

        // Adjacent facts are derived from the already-valid hidden solution. Together they
        // form a complete chain, so the generated puzzle always has exactly one solution.
        for (int i = 0; i < solution.Length - 1; i++)
        {
            bool useImmediateRule = random.NextDouble() < 0.5;
            constraints.Add(new OrderingConstraint
            {
                type = useImmediateRule ? "immediately_before" : "before",
                first = solution[i],
                second = solution[i + 1],
                text = useImmediateRule
                    ? $"The {solution[i]} is immediately before the {solution[i + 1]}."
                    : $"The {solution[i]} comes before the {solution[i + 1]}."
            });
        }

        constraints = Shuffle(constraints.ToArray(), random).ToList();
        int solutionCount = CountSolutions(symbols, constraints, 2);
        if (solutionCount != 1)
        {
            throw new InvalidOperationException($"Generated puzzle must have one solution, but had {solutionCount}.");
        }

        return new ProceduralOrderingPuzzle(
            $"ordering-{seed:x8}",
            Shuffle(symbols, random),
            constraints.ToArray(),
            solution,
            seed);
    }

    public static int CountSolutions(
        IReadOnlyList<string> symbols,
        IReadOnlyList<OrderingConstraint> constraints,
        int stopAfter = int.MaxValue)
    {
        int count = 0;
        string[] working = symbols.ToArray();
        CountPermutations(working, 0, constraints, stopAfter, ref count);
        return count;
    }

    private static void CountPermutations(
        string[] values,
        int index,
        IReadOnlyList<OrderingConstraint> constraints,
        int stopAfter,
        ref int count)
    {
        if (count >= stopAfter)
        {
            return;
        }

        if (index == values.Length)
        {
            if (constraints.All(constraint => constraint.IsSatisfied(values)))
            {
                count += 1;
            }
            return;
        }

        for (int i = index; i < values.Length; i++)
        {
            Swap(values, index, i);
            CountPermutations(values, index + 1, constraints, stopAfter, ref count);
            Swap(values, index, i);
        }
    }

    private static T[] Shuffle<T>(IEnumerable<T> source, Random random)
    {
        T[] values = source.ToArray();
        for (int i = values.Length - 1; i > 0; i--)
        {
            int swapIndex = random.Next(i + 1);
            T value = values[i];
            values[i] = values[swapIndex];
            values[swapIndex] = value;
        }
        return values;
    }

    private static void Swap<T>(T[] values, int first, int second)
    {
        T value = values[first];
        values[first] = values[second];
        values[second] = value;
    }
}
