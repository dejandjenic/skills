using System.Text.Json;
using System.Text.Json.Serialization;

namespace Dejan.Skills.Tool;

internal static class Program
{
    public static async Task<int> Main(string[] args)
    {
        try
        {
            return await CommandLineApp.RunAsync(args);
        }
        catch (CommandLineException exception)
        {
            Console.Error.WriteLine($"error: {exception.Message}");
            return 1;
        }
        catch (HttpRequestException exception)
        {
            Console.Error.WriteLine($"network error: {exception.Message}");
            return 1;
        }
        catch (Exception exception)
        {
            Console.Error.WriteLine($"unexpected error: {exception.Message}");
            return 1;
        }
    }
}

internal static class CommandLineApp
{
    public static async Task<int> RunAsync(string[] args)
    {
        if (args.Length == 0 || IsHelp(args[0]))
        {
            PrintUsage();
            return 0;
        }

        var command = args[0].ToLowerInvariant();
        var parser = ArgumentParser.Parse(args.Skip(1));

        return command switch
        {
            "list" => await ListAsync(parser),
            "init" => await InitAsync(parser),
            "bootstrap" => await BootstrapAsync(parser),
            "update" => await UpdateAsync(parser),
            _ => throw new CommandLineException($"Unknown command '{args[0]}'.")
        };
    }

    private static async Task<int> ListAsync(ParsedArguments parser)
    {
        var repo = GitHubRepository.Parse(parser.GetString("repo") ?? "dejandjenic/skills");
        var refName = parser.GetString("ref") ?? "main";
        var sourcePath = parser.GetString("source-path");

        using ISourceSnapshot snapshot = !string.IsNullOrWhiteSpace(sourcePath)
            ? LocalSourceSnapshot.Open(sourcePath)
            : await GitHubArchiveSnapshot.DownloadAsync(repo, refName);
        var catalog = ContentCatalog.Discover(snapshot.RootPath);

        Console.WriteLine($"Source: {sourcePath ?? $"{repo.Owner}/{repo.Name}"}");
        if (string.IsNullOrWhiteSpace(sourcePath))
        {
            Console.WriteLine($"Ref: {refName}");
        }

        Console.WriteLine();

        Console.WriteLine("Skills:");
        foreach (var skill in catalog.Skills.OrderBy(static item => item, StringComparer.OrdinalIgnoreCase))
        {
            Console.WriteLine($"- {skill}");
        }

        Console.WriteLine();
        Console.WriteLine("Prompts:");
        foreach (var prompt in catalog.Prompts.OrderBy(static item => item, StringComparer.OrdinalIgnoreCase))
        {
            Console.WriteLine($"- {prompt}");
        }

        return 0;
    }

    private static async Task<int> InitAsync(ParsedArguments parser)
    {
        var options = SyncOptions.FromArguments(parser, SyncCommand.Init);
        await SyncEngine.ExecuteAsync(options, writeManifest: true);
        return 0;
    }

    private static async Task<int> BootstrapAsync(ParsedArguments parser)
    {
        var options = SyncOptions.FromArguments(parser, SyncCommand.Bootstrap);
        await SyncEngine.ExecuteAsync(options, writeManifest: true);
        return 0;
    }

    private static async Task<int> UpdateAsync(ParsedArguments parser)
    {
        var options = SyncOptions.FromArguments(parser, SyncCommand.Update);
        await SyncEngine.ExecuteAsync(options, writeManifest: true);
        return 0;
    }

    private static bool IsHelp(string arg)
    {
        return arg is "-h" or "--help" or "help";
    }

    private static void PrintUsage()
    {
        Console.WriteLine("dejan-skills");
        Console.WriteLine();
        Console.WriteLine("Usage:");
        Console.WriteLine("  dejan-skills list [--repo <owner/repo|url>] [--ref <branch|tag|sha>]");
        Console.WriteLine("  dejan-skills init [options]");
        Console.WriteLine("  dejan-skills bootstrap [options]");
        Console.WriteLine("  dejan-skills update [options]");
        Console.WriteLine();
        Console.WriteLine("Options:");
        Console.WriteLine("  --target <path>         Target repository path. Default: current directory.");
        Console.WriteLine("  --source-path <path>    Read content from a local repo checkout instead of GitHub.");
        Console.WriteLine("  --repo <owner/repo>     Source GitHub repo. Default: dejandjenic/skills.");
        Console.WriteLine("  --ref <name>            Source git ref. Default: main.");
        Console.WriteLine("  --platforms <items>     Comma-separated: github,claude,opencode. Default: all.");
        Console.WriteLine("  --include <items>       Comma-separated: skills,prompts. Default: skills,prompts.");
        Console.WriteLine("  --skills <list>         Comma-separated skill folder names.");
        Console.WriteLine("  --prompts <list>        Comma-separated prompt names without .prompt.md.");
        Console.WriteLine("  --with-skills           Bootstrap preset: include skills in addition to prompts.");
        Console.WriteLine("  --no-prune              Update preset: do not remove stale previously managed files.");
        Console.WriteLine("  --force                 Overwrite existing files.");
        Console.WriteLine("  --dry-run               Show what would be copied without writing files.");
        Console.WriteLine();
        Console.WriteLine("Examples:");
        Console.WriteLine("  dejan-skills list");
        Console.WriteLine("  dejan-skills list --repo https://github.com/dejandjenic/skills");
        Console.WriteLine("  dejan-skills init --target ../my-repo");
        Console.WriteLine("  dejan-skills init --target ../my-repo --platforms github,claude,opencode");
        Console.WriteLine("  dejan-skills bootstrap");
        Console.WriteLine("  dejan-skills bootstrap --with-skills");
        Console.WriteLine("  dejan-skills init --target ../my-repo --source-path ../skills-clone --repo https://github.com/dejandjenic/skills");
        Console.WriteLine("  dejan-skills init --skills dejan-workflow-coding-assistant --prompts dejan.workflow-coding-assistant");
        Console.WriteLine("  dejan-skills update --target ../my-repo --force");
    }
}

internal sealed class CommandLineException : Exception
{
    public CommandLineException(string message)
        : base(message)
    {
    }
}

internal sealed class ParsedArguments
{
    private readonly Dictionary<string, List<string>> _options;

    public ParsedArguments(Dictionary<string, List<string>> options)
    {
        _options = options;
    }

    public bool Contains(string key)
    {
        return _options.ContainsKey(key);
    }

    public string? GetString(string key)
    {
        return _options.TryGetValue(key, out var values) ? values.LastOrDefault() : null;
    }

    public IReadOnlyList<string> GetCsv(string key)
    {
        if (!_options.TryGetValue(key, out var values))
        {
            return Array.Empty<string>();
        }

        return values
            .SelectMany(static value => value.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }
}

internal static class ArgumentParser
{
    private static readonly HashSet<string> FlagOptions = new(StringComparer.OrdinalIgnoreCase)
    {
        "force",
        "dry-run",
        "with-skills",
        "no-prune"
    };

    public static ParsedArguments Parse(IEnumerable<string> args)
    {
        var tokens = args.ToArray();
        var options = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);

        for (var index = 0; index < tokens.Length; index++)
        {
            var current = tokens[index];
            if (!current.StartsWith("--", StringComparison.Ordinal))
            {
                throw new CommandLineException($"Unexpected argument '{current}'.");
            }

            var token = current[2..];
            string key;
            string value;

            var equalsIndex = token.IndexOf('=');
            if (equalsIndex >= 0)
            {
                key = token[..equalsIndex];
                value = token[(equalsIndex + 1)..];
            }
            else
            {
                key = token;
                var hasSeparateValue = index + 1 < tokens.Length && !tokens[index + 1].StartsWith("--", StringComparison.Ordinal);
                if (hasSeparateValue)
                {
                    value = tokens[index + 1];
                    index++;
                }
                else
                {
                    if (!FlagOptions.Contains(key))
                    {
                        throw new CommandLineException($"Missing value for '--{key}'.");
                    }

                    value = "true";
                }
            }

            if (!options.TryGetValue(key, out var values))
            {
                values = new List<string>();
                options[key] = values;
            }

            values.Add(value);
        }

        return new ParsedArguments(options);
    }
}

internal enum SyncCommand
{
    Init,
    Bootstrap,
    Update
}

internal sealed record SyncOptions(
    string? SourcePath,
    GitHubRepository Repository,
    string RefName,
    string TargetPath,
    IReadOnlyList<PlatformTarget> Platforms,
    bool IncludeSkills,
    bool IncludePrompts,
    IReadOnlyList<string> Skills,
    IReadOnlyList<string> Prompts,
    bool PruneRemoved,
    SyncManifest? ExistingManifest,
    bool Force,
    bool DryRun)
{
    private const string ManifestRelativePath = ".github/dejan-skills.json";

    public static SyncOptions FromArguments(ParsedArguments parser, SyncCommand command)
    {
        var targetPath = Path.GetFullPath(parser.GetString("target") ?? Directory.GetCurrentDirectory());
        var manifestPath = Path.Combine(targetPath, ManifestRelativePath);
        var allowManifest = command == SyncCommand.Update;

        SyncManifest? manifest = null;
        if (allowManifest && File.Exists(manifestPath))
        {
            manifest = JsonSerializer.Deserialize<SyncManifest>(File.ReadAllText(manifestPath), SyncManifestJsonContext.Default.SyncManifest)
                ?? throw new CommandLineException($"Manifest at '{manifestPath}' is invalid.");
        }

        if (allowManifest && manifest is null)
        {
            throw new CommandLineException($"Manifest not found at '{manifestPath}'. Run 'dejan-skills init' first or provide a manifest-aware target path.");
        }

        var repoText = parser.GetString("repo") ?? manifest?.Source.Repository ?? "dejandjenic/skills";
        var refName = parser.GetString("ref") ?? manifest?.Source.Ref ?? "main";
        var include = parser.Contains("include")
            ? parser.GetCsv("include")
            : BuildDefaultInclude(command, manifest, parser.Contains("with-skills"));
        var platforms = parser.Contains("platforms")
            ? parser.GetCsv("platforms")
            : manifest?.Content.Platforms ?? PlatformTarget.AllNames;

        var invalidIncludes = include
            .Where(static item => !item.Equals("skills", StringComparison.OrdinalIgnoreCase)
                && !item.Equals("prompts", StringComparison.OrdinalIgnoreCase))
            .ToArray();

        if (invalidIncludes.Length > 0)
        {
            throw new CommandLineException($"Unknown include selection(s): {string.Join(", ", invalidIncludes)}. Supported values are 'skills' and 'prompts'.");
        }

        var invalidPlatforms = platforms
            .Where(item => !PlatformTarget.TryParse(item, out _))
            .ToArray();

        if (invalidPlatforms.Length > 0)
        {
            throw new CommandLineException($"Unknown platform selection(s): {string.Join(", ", invalidPlatforms)}. Supported values are 'github', 'claude', and 'opencode'.");
        }

        var parsedPlatforms = platforms
            .Select(item => PlatformTarget.Parse(item))
            .Distinct()
            .ToArray();

        var skillFilters = parser.Contains("skills")
            ? parser.GetCsv("skills")
            : manifest?.Content.Skills ?? Array.Empty<string>();

        var promptFilters = parser.Contains("prompts")
            ? parser.GetCsv("prompts")
            : manifest?.Content.Prompts ?? Array.Empty<string>();

        return new SyncOptions(
            parser.GetString("source-path") is { Length: > 0 } sourcePath ? Path.GetFullPath(sourcePath) : null,
            GitHubRepository.Parse(repoText),
            refName,
            targetPath,
            parsedPlatforms,
            include.Contains("skills", StringComparer.OrdinalIgnoreCase),
            include.Contains("prompts", StringComparer.OrdinalIgnoreCase),
            skillFilters,
            promptFilters,
            command == SyncCommand.Update && !parser.Contains("no-prune"),
            manifest,
            parser.Contains("force"),
            parser.Contains("dry-run"));
    }

    public string ManifestPath => Path.Combine(TargetPath, ManifestRelativePath);

    public SyncManifest ToManifest(string[] managedFiles)
    {
        return new SyncManifest(
            1,
            new SyncSource(Repository.ToString(), RefName),
                new SyncContent(Platforms.Select(static platform => platform.Name).ToArray(), IncludeSkills, IncludePrompts, Skills.ToArray(), Prompts.ToArray()),
            DateTimeOffset.UtcNow,
            managedFiles);
    }

    private static IReadOnlyList<string> BuildDefaultInclude(SyncCommand command, SyncManifest? manifest, bool bootstrapWithSkills)
    {
        if (command == SyncCommand.Bootstrap)
        {
            return bootstrapWithSkills
                ? new[] { "skills", "prompts" }
                : new[] { "prompts" };
        }

        if (manifest is null)
        {
            return new[] { "skills", "prompts" };
        }

        var include = new List<string>();
        if (manifest.Content.IncludeSkills)
        {
            include.Add("skills");
        }

        if (manifest.Content.IncludePrompts)
        {
            include.Add("prompts");
        }

        return include;
    }
}

internal readonly record struct PlatformTarget(string Name, string RootFolder)
{
    public static readonly PlatformTarget GitHub = new("github", ".github");
    public static readonly PlatformTarget Claude = new("claude", ".claude");
    public static readonly PlatformTarget OpenCode = new("opencode", ".opencode");

    public static IReadOnlyList<string> AllNames { get; } = new[] { GitHub.Name, Claude.Name, OpenCode.Name };

    public static bool TryParse(string value, out PlatformTarget platform)
    {
        switch (value.Trim().ToLowerInvariant())
        {
            case "github":
                platform = GitHub;
                return true;
            case "claude":
                platform = Claude;
                return true;
            case "opencode":
                platform = OpenCode;
                return true;
            default:
                platform = default;
                return false;
        }
    }

    public static PlatformTarget Parse(string value)
    {
        return TryParse(value, out var platform)
            ? platform
            : throw new CommandLineException($"Unknown platform '{value}'.");
    }
}

internal sealed record GitHubRepository(string Owner, string Name)
{
    public static GitHubRepository Parse(string input)
    {
        if (Uri.TryCreate(input, UriKind.Absolute, out var uri) && uri.Host.Contains("github.com", StringComparison.OrdinalIgnoreCase))
        {
            var segments = uri.AbsolutePath.Trim('/').Split('/', StringSplitOptions.RemoveEmptyEntries);
            if (segments.Length < 2)
            {
                throw new CommandLineException($"GitHub repository URL '{input}' is missing owner or name.");
            }

            return new GitHubRepository(segments[0], TrimGitSuffix(segments[1]));
        }

        var parts = input.Split('/', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (parts.Length != 2)
        {
            throw new CommandLineException($"Repository '{input}' must be an 'owner/repo' value or a GitHub URL.");
        }

        return new GitHubRepository(parts[0], TrimGitSuffix(parts[1]));
    }

    public override string ToString()
    {
        return $"{Owner}/{Name}";
    }

    private static string TrimGitSuffix(string value)
    {
        return value.EndsWith(".git", StringComparison.OrdinalIgnoreCase)
            ? value[..^4]
            : value;
    }
}

internal sealed record SyncManifest(int SchemaVersion, SyncSource Source, SyncContent Content, DateTimeOffset CreatedAtUtc, string[]? ManagedFiles = null);

internal sealed record SyncSource(string Repository, string Ref);

internal sealed record SyncContent(string[] Platforms, bool IncludeSkills, bool IncludePrompts, string[] Skills, string[] Prompts);

[JsonSerializable(typeof(SyncManifest))]
[JsonSourceGenerationOptions(WriteIndented = true)]
internal partial class SyncManifestJsonContext : JsonSerializerContext;