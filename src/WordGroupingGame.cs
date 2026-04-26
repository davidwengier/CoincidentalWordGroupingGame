namespace CoincidentalWordGroupingGame;

public sealed class WordGroupingGame
{
    public const int MaxMistakes = 4;

    private readonly Random _random;
    private readonly List<PuzzleGroup> _orderedGroups;
    private readonly List<RevealedGroup> _solvedGroups;
    private readonly List<BoardTile> _remainingTiles;
    private readonly HashSet<int> _selectedTileIds;
    private readonly HashSet<string> _solvedCategories;

    public WordGroupingGame(Puzzle puzzle, Random? random = null)
    {
        Puzzle = puzzle ?? throw new ArgumentNullException(nameof(puzzle));
        _random = random ?? Random.Shared;
        _orderedGroups = puzzle.GetOrderedGroups().ToList();
        _solvedGroups = [];
        _remainingTiles = puzzle.StartingGroups
            .SelectMany(group => group)
            .Select((word, index) => new BoardTile(index, word))
            .ToList();
        _selectedTileIds = [];
        _solvedCategories = new HashSet<string>(StringComparer.Ordinal);

        MistakesRemaining = MaxMistakes;
        CurrentMessage = Info("Find groups of four words that share a connection.");
    }

    public Puzzle Puzzle { get; }

    public int MistakesRemaining { get; private set; }

    public GameMessage CurrentMessage { get; private set; }

    public IReadOnlyList<BoardTile> RemainingTiles => _remainingTiles;

    public IReadOnlyCollection<int> SelectedTileIds => _selectedTileIds;

    public IReadOnlyList<RevealedGroup> RevealedGroups
    {
        get
        {
            if (!IsLost)
            {
                return _solvedGroups;
            }

            var revealedGroups = new List<RevealedGroup>(_solvedGroups);

            foreach (var group in _orderedGroups.Where(group => !_solvedCategories.Contains(group.Category)))
            {
                revealedGroups.Add(new RevealedGroup(group.Category, group.Level, group.Members, false));
            }

            return revealedGroups;
        }
    }

    public int SelectedWordCount => _selectedTileIds.Count;

    public bool CanClearSelection => !IsOver && _selectedTileIds.Count > 0;

    public bool CanShuffle => !IsOver && _remainingTiles.Count > 1;

    public bool CanSubmit => !IsOver && _selectedTileIds.Count == 4;

    public bool IsLost => !IsWon && MistakesRemaining == 0;

    public bool IsOver => IsWon || IsLost;

    public bool IsWon => _solvedGroups.Count == _orderedGroups.Count;

    public bool IsSelected(int tileId) => _selectedTileIds.Contains(tileId);

    public void ClearSelection()
    {
        if (!CanClearSelection)
        {
            return;
        }

        _selectedTileIds.Clear();
        CurrentMessage = Info("Selection cleared.");
    }

    public void ShuffleRemaining()
    {
        if (!CanShuffle)
        {
            return;
        }

        for (var i = _remainingTiles.Count - 1; i > 0; i--)
        {
            var swapIndex = _random.Next(i + 1);
            (_remainingTiles[i], _remainingTiles[swapIndex]) = (_remainingTiles[swapIndex], _remainingTiles[i]);
        }

        CurrentMessage = Info("Board shuffled.");
    }

    public GuessResult SubmitGuess()
    {
        if (IsOver)
        {
            return UpdateResult(GuessOutcome.GameOver, Info("Start a new game to play again."));
        }

        if (_selectedTileIds.Count != 4)
        {
            return UpdateResult(GuessOutcome.NeedsFourWords, Warning("Select exactly four words before submitting."));
        }

        var selectedTiles = _remainingTiles
            .Where(tile => _selectedTileIds.Contains(tile.Id))
            .ToArray();

        var matchedGroup = _orderedGroups.FirstOrDefault(group =>
            !_solvedCategories.Contains(group.Category) &&
            SelectedTilesMatchGroup(selectedTiles, group));

        if (matchedGroup is not null)
        {
            return SolveGroup(matchedGroup, selectedTiles);
        }

        MistakesRemaining--;
        _selectedTileIds.Clear();

        if (IsOneAway(selectedTiles))
        {
            if (IsLost)
            {
                return UpdateResult(GuessOutcome.OneAway, Error("One away. That was your last mistake."));
            }

            return UpdateResult(GuessOutcome.OneAway, Warning($"One away. {FormatMistakesRemaining()}"));
        }

        if (IsLost)
        {
            return UpdateResult(GuessOutcome.Incorrect, Error("No mistakes remaining. The remaining groups are revealed below."));
        }

        return UpdateResult(GuessOutcome.Incorrect, Error($"Not a group. {FormatMistakesRemaining()}"));
    }

    public GameMessage ToggleSelection(int tileId)
    {
        if (IsOver || !_remainingTiles.Any(tile => tile.Id == tileId))
        {
            return CurrentMessage;
        }

        if (_selectedTileIds.Remove(tileId))
        {
            if (_selectedTileIds.Count == 0)
            {
                CurrentMessage = Info("Find groups of four words that share a connection.");
            }

            return CurrentMessage;
        }

        if (_selectedTileIds.Count == 4)
        {
            CurrentMessage = Warning("You can only select four words at a time.");
            return CurrentMessage;
        }

        _selectedTileIds.Add(tileId);

        if (_selectedTileIds.Count == 4)
        {
            CurrentMessage = Info("Submit your guess when you're ready.");
        }

        return CurrentMessage;
    }

    private static GameMessage Error(string text) => new(GameMessageKind.Error, text);

    private static GameMessage Info(string text) => new(GameMessageKind.Info, text);

    private static GameMessage Success(string text) => new(GameMessageKind.Success, text);

    private static GameMessage Warning(string text) => new(GameMessageKind.Warning, text);

    private string FormatMistakesRemaining() =>
        MistakesRemaining == 1
            ? "1 mistake remaining."
            : $"{MistakesRemaining} mistakes remaining.";

    private bool IsOneAway(IReadOnlyCollection<BoardTile> selectedTiles) =>
        _orderedGroups.Any(group =>
            !_solvedCategories.Contains(group.Category) &&
            CountSharedWords(selectedTiles, group) == 3);

    private static Dictionary<string, int> CountWords(IEnumerable<string> words) =>
        words
            .GroupBy(word => word, StringComparer.Ordinal)
            .ToDictionary(group => group.Key, group => group.Count(), StringComparer.Ordinal);

    private static int CountSharedWords(IReadOnlyCollection<BoardTile> selectedTiles, PuzzleGroup group)
    {
        var selectedWordCounts = CountWords(selectedTiles.Select(tile => tile.Word));
        var groupWordCounts = CountWords(group.Members);

        return groupWordCounts.Sum(pair =>
            Math.Min(pair.Value, selectedWordCounts.GetValueOrDefault(pair.Key)));
    }

    private static bool SelectedTilesMatchGroup(IReadOnlyCollection<BoardTile> selectedTiles, PuzzleGroup group)
    {
        if (selectedTiles.Count != group.Members.Count)
        {
            return false;
        }

        return CountSharedWords(selectedTiles, group) == group.Members.Count;
    }

    private GuessResult SolveGroup(PuzzleGroup group, IReadOnlyCollection<BoardTile> selectedTiles)
    {
        _solvedCategories.Add(group.Category);
        _solvedGroups.Add(new RevealedGroup(group.Category, group.Level, group.Members, true));
        var selectedTileIds = selectedTiles.Select(tile => tile.Id).ToHashSet();
        _remainingTiles.RemoveAll(tile => selectedTileIds.Contains(tile.Id));
        _selectedTileIds.Clear();

        return IsWon
            ? UpdateResult(GuessOutcome.Solved, Success("You found all four groups."), group)
            : UpdateResult(GuessOutcome.Solved, Success($"Solved: {group.Category}."), group);
    }

    private GuessResult UpdateResult(GuessOutcome outcome, GameMessage message, PuzzleGroup? group = null)
    {
        CurrentMessage = message;
        return new GuessResult(outcome, message, group);
    }
}
