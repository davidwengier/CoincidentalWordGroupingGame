using System.Reflection;
using System.Text.Json;

namespace CoincidentalWordGroupingGame;

public sealed class PuzzleCatalog
{
    private readonly IReadOnlyList<Puzzle> _puzzles;

    private PuzzleCatalog(IReadOnlyList<Puzzle> puzzles)
    {
        ArgumentNullException.ThrowIfNull(puzzles);

        if (puzzles.Count == 0)
        {
            throw new ArgumentException("At least one puzzle is required.", nameof(puzzles));
        }

        _puzzles = puzzles;
    }

    public int Count => _puzzles.Count;

    public static PuzzleCatalog LoadFromAssembly(Assembly assembly)
    {
        ArgumentNullException.ThrowIfNull(assembly);

        var resourceName = $"{assembly.GetName().Name}.wordlist.json";
        using var stream = assembly.GetManifestResourceStream(resourceName)
            ?? throw new InvalidOperationException($"Embedded resource '{resourceName}' was not found.");

        var puzzles = JsonSerializer.Deserialize<List<Puzzle>>(stream)
            ?? throw new InvalidOperationException("The embedded word list could not be deserialized.");

        ValidatePuzzles(puzzles);

        return new PuzzleCatalog(puzzles);
    }

    public WordGroupingGame CreateRandomGame(Random? random = null)
    {
        var source = random ?? Random.Shared;
        var index = source.Next(_puzzles.Count);

        return new WordGroupingGame(_puzzles[index], source);
    }

    private static void ValidatePuzzles(IReadOnlyList<Puzzle> puzzles)
    {
        if (puzzles.Count == 0)
        {
            throw new InvalidOperationException("The word list does not contain any puzzles.");
        }

        foreach (var puzzle in puzzles)
        {
            ValidatePuzzle(puzzle);
        }
    }

    private static void ValidatePuzzle(Puzzle puzzle)
    {
        if (puzzle.StartingGroups.Length != 4 || puzzle.StartingGroups.Any(group => group.Length != 4))
        {
            throw new InvalidOperationException("Each puzzle must define a 4x4 starting board.");
        }

        if (puzzle.Groups.Count != 4)
        {
            throw new InvalidOperationException("Each puzzle must define exactly four groups.");
        }

        var startingWords = puzzle.StartingGroups.SelectMany(group => group).ToArray();
        if (startingWords.Length != 16)
        {
            throw new InvalidOperationException("Each puzzle must contain 16 starting words.");
        }

        var groupedWords = puzzle.Groups.Values.SelectMany(group => group.Members).ToArray();
        if (groupedWords.Length != 16)
        {
            throw new InvalidOperationException("Each puzzle group set must contain 16 words.");
        }

        if (!HaveMatchingWordCounts(startingWords, groupedWords))
        {
            throw new InvalidOperationException("Starting groups and solution groups must contain the same words.");
        }

        var levels = puzzle.Groups.Values
            .Select(group => group.Level)
            .OrderBy(level => level)
            .ToArray();

        if (!levels.SequenceEqual([0, 1, 2, 3]))
        {
            throw new InvalidOperationException("Each puzzle must contain one group at each level from 0 to 3.");
        }
    }

    private static bool HaveMatchingWordCounts(IEnumerable<string> left, IEnumerable<string> right)
    {
        var leftCounts = CountWords(left);
        var rightCounts = CountWords(right);

        return leftCounts.Count == rightCounts.Count &&
               leftCounts.All(pair => rightCounts.TryGetValue(pair.Key, out var count) && count == pair.Value);
    }

    private static Dictionary<string, int> CountWords(IEnumerable<string> words) =>
        words
            .GroupBy(word => word, StringComparer.Ordinal)
            .ToDictionary(group => group.Key, group => group.Count(), StringComparer.Ordinal);
}
