using System.Reflection;

namespace CoincidentalWordGroupingGame;

public static class BuildDetails
{
    private const string UnknownCommitHash = "unknown";
    private static readonly Lazy<BuildStamp> Stamp = new(CreateBuildStamp);

    public static string CommitHash => Stamp.Value.CommitHash;
    public static string ShortCommitHash => Stamp.Value.ShortCommitHash;

    private static BuildStamp CreateBuildStamp()
    {
        var informationalVersion = typeof(BuildDetails).Assembly
            .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?
            .InformationalVersion;

        if (string.IsNullOrWhiteSpace(informationalVersion))
        {
            return new BuildStamp(UnknownCommitHash, UnknownCommitHash);
        }

        var separatorIndex = informationalVersion.LastIndexOf('+');
        if (separatorIndex < 0 || separatorIndex == informationalVersion.Length - 1)
        {
            return new BuildStamp(UnknownCommitHash, UnknownCommitHash);
        }

        var commitHash = informationalVersion[(separatorIndex + 1)..];
        var shortCommitHash = commitHash.Length <= 7 ? commitHash : commitHash[..7];

        return new BuildStamp(commitHash, shortCommitHash);
    }

    private sealed record BuildStamp(string CommitHash, string ShortCommitHash);
}
