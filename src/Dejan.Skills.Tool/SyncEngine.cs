using System.IO.Compression;
using System.Net.Http.Headers;
using System.Text.Json;

namespace Dejan.Skills.Tool;

internal static class SyncEngine
{
    public static async Task ExecuteAsync(SyncOptions options, bool writeManifest)
    {
        using var snapshot = await SourceSnapshot.OpenAsync(options);
        var catalog = ContentCatalog.Discover(snapshot.RootPath);
        var plan = SyncPlan.Create(snapshot.RootPath, options, catalog);

        Console.WriteLine($"Source: {options.SourcePath ?? options.Repository.ToString()}");
        if (options.SourcePath is null)
        {
            Console.WriteLine($"Ref: {options.RefName}");
        }

        Console.WriteLine($"Target: {options.TargetPath}");
        Console.WriteLine($"Platforms: {string.Join(", ", options.Platforms.Select(static platform => platform.Name))}");
        Console.WriteLine();

        if (plan.Warnings.Count > 0)
        {
            foreach (var warning in plan.Warnings)
            {
                Console.WriteLine($"warning: {warning}");
            }

            Console.WriteLine();
        }

        Console.WriteLine("Planned content:");
        foreach (var item in plan.SelectedSkills)
        {
            Console.WriteLine($"- skill  {item}");
        }

        foreach (var item in plan.SelectedPrompts)
        {
            Console.WriteLine($"- prompt {item}");
        }

        Console.WriteLine();
        Console.WriteLine($"Files to copy: {plan.Files.Count}");

        var newManagedFiles = plan.GetManagedFiles(options.TargetPath);
        var staleManagedFiles = Array.Empty<string>();
        if (options.PruneRemoved)
        {
            staleManagedFiles = BuildStaleManagedFiles(options.ExistingManifest, newManagedFiles);
            Console.WriteLine($"Files to prune: {staleManagedFiles.Length}");
        }

        if (options.DryRun)
        {
            Console.WriteLine("Dry run completed. No files were written.");
            return;
        }

        plan.ValidateOverwritePolicy(options.Force);
        plan.Copy(options.Force);

        if (options.PruneRemoved)
        {
            var deleted = DeleteManagedFiles(options.TargetPath, staleManagedFiles);
            Console.WriteLine($"Pruned stale files: {deleted}");
        }

        if (writeManifest)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(options.ManifestPath)!);
            File.WriteAllText(
                options.ManifestPath,
                JsonSerializer.Serialize(options.ToManifest(newManagedFiles), SyncManifestJsonContext.Default.SyncManifest));
            Console.WriteLine($"Manifest written: {options.ManifestPath}");
        }

        Console.WriteLine("Sync completed.");
    }

    private static string[] BuildStaleManagedFiles(SyncManifest? existingManifest, IReadOnlyList<string> newManagedFiles)
    {
        var previouslyManaged = existingManifest?.ManagedFiles ?? Array.Empty<string>();
        return previouslyManaged
            .Except(newManagedFiles, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    private static int DeleteManagedFiles(string targetRoot, IEnumerable<string> staleManagedFiles)
    {
        var deleted = 0;
        var normalizedRoot = Path.GetFullPath(targetRoot);

        foreach (var relativePath in staleManagedFiles)
        {
            if (string.IsNullOrWhiteSpace(relativePath))
            {
                continue;
            }

            var candidate = Path.GetFullPath(Path.Combine(normalizedRoot, relativePath));
            if (!candidate.StartsWith(normalizedRoot, StringComparison.Ordinal))
            {
                continue;
            }

            if (!File.Exists(candidate))
            {
                continue;
            }

            File.Delete(candidate);
            deleted++;
            DeleteEmptyParents(candidate, normalizedRoot);
        }

        return deleted;
    }

    private static void DeleteEmptyParents(string filePath, string targetRoot)
    {
        var current = Directory.GetParent(filePath);
        while (current is not null && current.FullName.StartsWith(targetRoot, StringComparison.Ordinal))
        {
            if (Directory.EnumerateFileSystemEntries(current.FullName).Any())
            {
                break;
            }

            var path = current.FullName;
            current = current.Parent;
            Directory.Delete(path);
        }
    }
}

internal static class SourceSnapshot
{
    public static Task<ISourceSnapshot> OpenAsync(SyncOptions options)
    {
        if (!string.IsNullOrWhiteSpace(options.SourcePath))
        {
            return Task.FromResult<ISourceSnapshot>(LocalSourceSnapshot.Open(options.SourcePath));
        }

        return GitHubArchiveSnapshot.DownloadAsync(options.Repository, options.RefName);
    }
}

internal interface ISourceSnapshot : IDisposable
{
    string RootPath { get; }
}

internal sealed class LocalSourceSnapshot : ISourceSnapshot
{
    private LocalSourceSnapshot(string rootPath)
    {
        RootPath = rootPath;
    }

    public string RootPath { get; }

    public static LocalSourceSnapshot Open(string rootPath)
    {
        var fullPath = Path.GetFullPath(rootPath);
        if (!Directory.Exists(fullPath))
        {
            throw new CommandLineException($"Source path '{fullPath}' does not exist.");
        }

        return new LocalSourceSnapshot(fullPath);
    }

    public void Dispose()
    {
    }
}

internal sealed class GitHubArchiveSnapshot : ISourceSnapshot
{
    private readonly string _tempRoot;

    private GitHubArchiveSnapshot(string tempRoot, string rootPath)
    {
        _tempRoot = tempRoot;
        RootPath = rootPath;
    }

    public string RootPath { get; }

    public static async Task<ISourceSnapshot> DownloadAsync(GitHubRepository repository, string refName)
    {
        using var httpClient = new HttpClient();
        httpClient.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("dejan-skills", "0.1.0"));

        var token = Environment.GetEnvironmentVariable("GITHUB_TOKEN");
        if (!string.IsNullOrWhiteSpace(token))
        {
            httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        }

        var tempRoot = Path.Combine(Path.GetTempPath(), $"dejan-skills-{Guid.NewGuid():N}");
        var zipPath = Path.Combine(tempRoot, "source.zip");
        var extractPath = Path.Combine(tempRoot, "extract");

        Directory.CreateDirectory(tempRoot);
        Directory.CreateDirectory(extractPath);

        var url = $"https://api.github.com/repos/{repository.Owner}/{repository.Name}/zipball/{Uri.EscapeDataString(refName)}";
        using var response = await httpClient.GetAsync(url, HttpCompletionOption.ResponseHeadersRead);
        if (response.StatusCode is System.Net.HttpStatusCode.NotFound or System.Net.HttpStatusCode.Unauthorized)
        {
            Directory.Delete(tempRoot, recursive: true);
            throw new CommandLineException(
                $"Unable to download '{repository}' at ref '{refName}'. If the repository is private, set GITHUB_TOKEN with read access or use --source-path with a local clone.");
        }

        response.EnsureSuccessStatusCode();

        await using (var fileStream = File.Create(zipPath))
        {
            await response.Content.CopyToAsync(fileStream);
        }

        ZipFile.ExtractToDirectory(zipPath, extractPath);

        var rootPath = Directory.GetDirectories(extractPath).SingleOrDefault();
        if (rootPath is null)
        {
            Directory.Delete(tempRoot, recursive: true);
            throw new CommandLineException("Downloaded archive did not contain a repository root directory.");
        }

        return new GitHubArchiveSnapshot(tempRoot, rootPath);
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempRoot))
        {
            Directory.Delete(_tempRoot, recursive: true);
        }
    }
}

internal sealed record ContentCatalog(IReadOnlyList<string> Skills, IReadOnlyList<string> Prompts)
{
    public static ContentCatalog Discover(string repoRoot)
    {
        var skillsRoot = Path.Combine(repoRoot, ".github", "skills");
        var promptsRoot = Path.Combine(repoRoot, ".github", "prompts");

        var skills = Directory.Exists(skillsRoot)
            ? Directory.GetDirectories(skillsRoot)
                .Select(Path.GetFileName)
                .Where(static name => !string.IsNullOrWhiteSpace(name))
                .Cast<string>()
                .ToArray()
            : Array.Empty<string>();

        var prompts = Directory.Exists(promptsRoot)
            ? Directory.GetFiles(promptsRoot, "*.prompt.md", SearchOption.TopDirectoryOnly)
                .Select(static path =>
                {
                    var fileName = Path.GetFileName(path);
                    return fileName[..^".prompt.md".Length];
                })
                .ToArray()
            : Array.Empty<string>();

        return new ContentCatalog(skills, prompts);
    }
}

internal sealed class SyncPlan
{
    private SyncPlan(
        IReadOnlyList<string> selectedSkills,
        IReadOnlyList<string> selectedPrompts,
        IReadOnlyList<CopyOperation> files,
        IReadOnlyList<string> warnings)
    {
        SelectedSkills = selectedSkills;
        SelectedPrompts = selectedPrompts;
        Files = files;
        Warnings = warnings;
    }

    public IReadOnlyList<string> SelectedSkills { get; }

    public IReadOnlyList<string> SelectedPrompts { get; }

    public IReadOnlyList<CopyOperation> Files { get; }

    public IReadOnlyList<string> Warnings { get; }

    public static SyncPlan Create(string repoRoot, SyncOptions options, ContentCatalog catalog)
    {
        Directory.CreateDirectory(options.TargetPath);

        var selectedSkills = options.IncludeSkills
            ? SelectItems(catalog.Skills, options.Skills, "skill")
            : Array.Empty<string>();

        var selectedPrompts = options.IncludePrompts
            ? SelectItems(catalog.Prompts, options.Prompts, "prompt")
            : Array.Empty<string>();

        var warnings = new List<string>();
        if (selectedPrompts.Count > 0 && selectedSkills.Count == 0)
        {
            warnings.Add("Prompts in this repo reference matching skills. Copying prompts without skills may leave broken relative links.");
        }

        var files = new List<CopyOperation>();

        foreach (var platform in options.Platforms)
        {
            foreach (var skill in selectedSkills)
            {
                var sourceDir = Path.Combine(repoRoot, ".github", "skills", skill);
                var targetDir = Path.Combine(options.TargetPath, platform.RootFolder, "skills", skill);
                AddDirectoryOperations(files, sourceDir, targetDir);
            }

            foreach (var prompt in selectedPrompts)
            {
                var sourcePath = Path.Combine(repoRoot, ".github", "prompts", prompt + ".prompt.md");
                var targetPath = Path.Combine(options.TargetPath, platform.RootFolder, "prompts", prompt + ".prompt.md");
                files.Add(new CopyOperation(sourcePath, targetPath));
            }
        }

        return new SyncPlan(selectedSkills, selectedPrompts, files, warnings);
    }

    public void ValidateOverwritePolicy(bool force)
    {
        if (force)
        {
            return;
        }

        var collisions = Files
            .Where(static file => File.Exists(file.DestinationPath))
            .Take(10)
            .Select(static file => file.DestinationPath)
            .ToArray();

        if (collisions.Length == 0)
        {
            return;
        }

        var sample = string.Join(Environment.NewLine, collisions.Select(static path => $"- {path}"));
        throw new CommandLineException(
            $"Existing files would be overwritten. Re-run with '--force' to replace them.{Environment.NewLine}{sample}");
    }

    public void Copy(bool force)
    {
        foreach (var file in Files)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(file.DestinationPath)!);
            File.Copy(file.SourcePath, file.DestinationPath, overwrite: force);
        }
    }

    public string[] GetManagedFiles(string targetRoot)
    {
        return Files
            .Select(file => Path.GetRelativePath(targetRoot, file.DestinationPath))
            .Select(static relativePath => relativePath.Replace('\\', '/'))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    private static IReadOnlyList<string> SelectItems(IReadOnlyList<string> available, IReadOnlyList<string> requested, string label)
    {
        if (requested.Count == 0)
        {
            return available;
        }

        var availableSet = new HashSet<string>(available, StringComparer.OrdinalIgnoreCase);
        var missing = requested.Where(item => !availableSet.Contains(item)).ToArray();
        if (missing.Length > 0)
        {
            throw new CommandLineException($"Unknown {label} selection(s): {string.Join(", ", missing)}.");
        }

        return requested;
    }

    private static void AddDirectoryOperations(ICollection<CopyOperation> files, string sourceDir, string targetDir)
    {
        foreach (var sourcePath in Directory.GetFiles(sourceDir, "*", SearchOption.AllDirectories))
        {
            var relativePath = Path.GetRelativePath(sourceDir, sourcePath);
            var destinationPath = Path.Combine(targetDir, relativePath);
            files.Add(new CopyOperation(sourcePath, destinationPath));
        }
    }
}

internal sealed record CopyOperation(string SourcePath, string DestinationPath);