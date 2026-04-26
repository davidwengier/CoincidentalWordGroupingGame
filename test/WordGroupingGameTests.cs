using CoincidentalWordGroupingGame;

namespace CoincidentalWordGroupingGame.Tests;

public class WordGroupingGameTests
{
    [Fact]
    public void SubmitGuess_WithMatchingGroup_SolvesTheGroup()
    {
        var game = CreateGame();

        var result = SubmitGuess(game, ["RED", "BLUE", "GREEN", "YELLOW"]);

        Assert.Equal(GuessOutcome.Solved, result.Outcome);
        Assert.Equal(GameMessageKind.Success, game.CurrentMessage.Kind);
        Assert.Single(game.RevealedGroups);
        Assert.Equal("COLORS", game.RevealedGroups[0].Category);
        Assert.Equal(12, game.RemainingTiles.Count);
        Assert.False(game.IsOver);
    }

    [Fact]
    public void SubmitGuess_WithOneAwayGuess_UsesMistakeAndClearsSelection()
    {
        var game = CreateGame();

        var result = SubmitGuess(game, ["RED", "BLUE", "GREEN", "CIRCLE"]);

        Assert.Equal(GuessOutcome.OneAway, result.Outcome);
        Assert.Equal(GameMessageKind.Warning, game.CurrentMessage.Kind);
        Assert.Equal(WordGroupingGame.MaxMistakes - 1, game.MistakesRemaining);
        Assert.Empty(game.SelectedTileIds);
        Assert.Contains("One away", game.CurrentMessage.Text);
    }

    [Fact]
    public void SubmitGuess_WithFourMisses_RevealsAllGroupsAfterLoss()
    {
        var game = CreateGame();
        var incorrectGuess = new[] { "RED", "BLUE", "CIRCLE", "SQUARE" };

        for (var i = 0; i < WordGroupingGame.MaxMistakes; i++)
        {
            SubmitGuess(game, incorrectGuess);
        }

        Assert.True(game.IsLost);
        Assert.Equal(GameMessageKind.Error, game.CurrentMessage.Kind);
        Assert.Equal(4, game.RevealedGroups.Count);
        Assert.All(game.RevealedGroups, group => Assert.False(group.SolvedByPlayer));
    }

    [Fact]
    public void SubmitGuess_WithDuplicateWords_RemovesOnlyTheSelectedOccurrences()
    {
        var game = new WordGroupingGame(CreateDuplicateWordPuzzle(), new Random(1234));

        var result = SubmitGuess(game, ["WALTZ", "TANGO", "SALSA", "JAZZ"]);

        Assert.Equal(GuessOutcome.Solved, result.Outcome);
        Assert.Equal(1, game.RemainingTiles.Count(tile => tile.Word == "SALSA"));
        Assert.Equal(1, game.RemainingTiles.Count(tile => tile.Word == "JAZZ"));
    }

    [Fact]
    public void LoadFromAssembly_LoadsEmbeddedWordList()
    {
        var catalog = PuzzleCatalog.LoadFromAssembly(typeof(PuzzleCatalog).Assembly);

        var game = catalog.CreateRandomGame(new Random(1234));

        Assert.True(catalog.Count > 0);
        Assert.Equal(16, game.RemainingTiles.Count);
        Assert.Equal(4, game.Puzzle.Groups.Count);
    }

    private static WordGroupingGame CreateGame() => new(CreatePuzzle(), new Random(1234));

    private static Puzzle CreatePuzzle() =>
        new()
        {
            StartingGroups =
            [
                ["RED", "CIRCLE", "LION", "MARS"],
                ["BLUE", "SQUARE", "TIGER", "VENUS"],
                ["GREEN", "TRIANGLE", "BEAR", "EARTH"],
                ["YELLOW", "HEXAGON", "WOLF", "JUPITER"]
            ],
            Groups = new Dictionary<string, WordGroup>
            {
                ["COLORS"] = new WordGroup(0, ["RED", "BLUE", "GREEN", "YELLOW"]),
                ["SHAPES"] = new WordGroup(1, ["CIRCLE", "SQUARE", "TRIANGLE", "HEXAGON"]),
                ["ANIMALS"] = new WordGroup(2, ["LION", "TIGER", "BEAR", "WOLF"]),
                ["PLANETS"] = new WordGroup(3, ["MARS", "VENUS", "EARTH", "JUPITER"])
            }
        };

    private static Puzzle CreateDuplicateWordPuzzle() =>
        new()
        {
            StartingGroups =
            [
                ["WALTZ", "NOTE", "BBQ", "FOOT"],
                ["TANGO", "BEAT", "SALSA", "MILE"],
                ["SALSA", "SCALE", "MAYO", "SCALE"],
                ["JAZZ", "JAZZ", "SOY", "YARD"]
            ],
            Groups = new Dictionary<string, WordGroup>
            {
                ["TYPES OF DANCES"] = new WordGroup(0, ["WALTZ", "TANGO", "SALSA", "JAZZ"]),
                ["MUSICAL TERMS"] = new WordGroup(1, ["NOTE", "BEAT", "SCALE", "JAZZ"]),
                ["TYPES OF SAUCES"] = new WordGroup(2, ["BBQ", "SALSA", "MAYO", "SOY"]),
                ["MEASUREMENT TERMS"] = new WordGroup(3, ["FOOT", "MILE", "SCALE", "YARD"])
            }
        };

    private static GuessResult SubmitGuess(WordGroupingGame game, IEnumerable<string> words)
    {
        foreach (var word in words)
        {
            var tile = game.RemainingTiles.First(tile => tile.Word == word && !game.IsSelected(tile.Id));
            game.ToggleSelection(tile.Id);
        }

        return game.SubmitGuess();
    }
}
