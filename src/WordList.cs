using System.Text.Json.Serialization;

namespace CoincidentalWordGroupingGame;

public sealed record Puzzle
{
    [JsonPropertyName("startingGroups")]
    public required string[][] StartingGroups { get; init; }

    [JsonPropertyName("groups")]
    public required Dictionary<string, WordGroup> Groups { get; init; }

    public IReadOnlyList<PuzzleGroup> GetOrderedGroups() =>
        Groups
            .Select(group => new PuzzleGroup(group.Key, group.Value.Level, group.Value.Members))
            .OrderBy(group => group.Level)
            .ThenBy(group => group.Category, StringComparer.Ordinal)
            .ToArray();
}

public sealed record WordGroup(
    [property: JsonPropertyName("level")]
    int Level,

    [property: JsonPropertyName("members")]
    string[] Members);

public sealed record PuzzleGroup(string Category, int Level, IReadOnlyList<string> Members);

public sealed record BoardTile(int Id, string Word);

public sealed record RevealedGroup(string Category, int Level, IReadOnlyList<string> Members, bool SolvedByPlayer);

public enum GuessOutcome
{
    Solved,
    OneAway,
    Incorrect,
    NeedsFourWords,
    SelectionFull,
    GameOver
}

public enum GameMessageKind
{
    Info,
    Success,
    Warning,
    Error
}

public sealed record GameMessage(GameMessageKind Kind, string Text);

public sealed record GuessResult(GuessOutcome Outcome, GameMessage Message, PuzzleGroup? Group = null);
