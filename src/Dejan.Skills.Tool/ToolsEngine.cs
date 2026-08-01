using System.Diagnostics;

namespace Dejan.Skills.Tool;

internal sealed record ToolsOptions(
    string? SourcePath,
    GitHubRepository Repository,
    string RefName,
    IReadOnlyList<string> Targets,
    IReadOnlyList<string> ToolNames,
    bool Force,
    bool DryRun)
{
    public static ToolsOptions FromArguments(ParsedArguments parser)
    {
        var targets = parser.GetCsv("target");
        if (targets.Count == 0)
        {
            targets = new[] { Directory.GetCurrentDirectory() };
        }

        return new ToolsOptions(
            parser.GetString("source-path") is { Length: > 0 } sourcePath ? Path.GetFullPath(sourcePath) : null,
            GitHubRepository.Parse(parser.GetString("repo") ?? "dejandjenic/skills"),
            parser.GetString("ref") ?? "main",
            targets.Select(Path.GetFullPath).ToArray(),
            parser.GetCsv("tools"),
            parser.Contains("force"),
            parser.Contains("dry-run"));
    }
}

internal static class ToolsEngine
{
    public static async Task<int> ExecuteAsync(ToolsOptions options)
    {
        using var snapshot = await SourceSnapshot.OpenAsync(options.SourcePath, options.Repository, options.RefName);
        var bundles = ToolBundleCatalog.Discover(snapshot.RootPath, options.ToolNames);

        Console.WriteLine($"Source: {options.SourcePath ?? options.Repository.ToString()}");
        if (options.SourcePath is null)
        {
            Console.WriteLine($"Ref: {options.RefName}");
        }

        if (bundles.Count == 0)
        {
            Console.WriteLine("No tool bundles found.");
            return 0;
        }

        Console.WriteLine($"Tools: {string.Join(", ", bundles.Select(static bundle => bundle.Name))}");
        Console.WriteLine($"Targets: {string.Join(", ", options.Targets)}");
        Console.WriteLine();

        if (options.DryRun)
        {
            foreach (var target in options.Targets)
            {
                foreach (var bundle in bundles)
                {
                    if (bundle.SetupScriptPath is not null)
                    {
                        Console.WriteLine($"[dry-run] {target}: would run '{bundle.Name}/setup.sh'");
                    }

                    if (bundle.PostCommitScriptPath is not null)
                    {
                        Console.WriteLine($"[dry-run] {target}: would install '{bundle.Name}/post-commit.sh' as .githooks/post-commit and set core.hooksPath");
                    }

                    if (bundle.GitignorePath is not null)
                    {
                        Console.WriteLine($"[dry-run] {target}: would add missing '{bundle.Name}/gitignore.txt' entries to .gitignore");
                    }
                }
            }

            Console.WriteLine();
            Console.WriteLine("Dry run completed. No commands were executed.");
            return 0;
        }

        var results = new List<(string Target, string Bundle, bool Success)>();

        foreach (var target in options.Targets)
        {
            if (!Directory.Exists(target))
            {
                Console.Error.WriteLine($"[{target}] target directory does not exist, skipping.");
                results.Add((target, "-", false));
                continue;
            }

            foreach (var bundle in bundles)
            {
                var success = await RunBundleAsync(bundle, target, options.Force);
                results.Add((target, bundle.Name, success));

                if (!success)
                {
                    Console.Error.WriteLine($"[{target}] stopping remaining tools for this target due to failure.");
                    break;
                }
            }
        }

        Console.WriteLine();
        Console.WriteLine("Summary:");
        foreach (var (target, bundle, success) in results)
        {
            Console.WriteLine($"- [{(success ? "OK" : "FAIL")}] {target} :: {bundle}");
        }

        return results.Any(static result => !result.Success) ? 1 : 0;
    }

    private static async Task<bool> RunBundleAsync(ToolBundle bundle, string target, bool force)
    {
        try
        {
            if (bundle.SetupScriptPath is not null)
            {
                Console.WriteLine($"[{target}] {bundle.Name}: running setup.sh");
                var exitCode = await RunBashScriptAsync(bundle.SetupScriptPath, target);
                if (exitCode != 0)
                {
                    Console.Error.WriteLine($"[{target}] {bundle.Name}: setup.sh exited with code {exitCode}");
                    return false;
                }
            }

            if (bundle.GitignorePath is not null)
            {
                await MergeGitignoreAsync(bundle, target);
            }

            if (bundle.PostCommitScriptPath is not null)
            {
                await InstallHookAsync(bundle, target, force);
            }

            return true;
        }
        catch (CommandLineException exception)
        {
            Console.Error.WriteLine($"[{target}] {bundle.Name}: {exception.Message}");
            return false;
        }
    }

    private static async Task<int> RunBashScriptAsync(string scriptPath, string workingDirectory)
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = ResolveBashExecutable(),
            WorkingDirectory = workingDirectory,
            UseShellExecute = false,
        };
        // bash.exe (MSYS) re-splits the raw Windows command line using POSIX rules, which
        // consumes backslashes as escapes. Pass a forward-slash path so it survives intact.
        startInfo.ArgumentList.Add(scriptPath.Replace('\\', '/'));
        startInfo.Environment["PYTHONIOENCODING"] = "utf-8";
        startInfo.Environment["PYTHONUTF8"] = "1";

        using var process = new Process { StartInfo = startInfo };
        try
        {
            process.Start();
        }
        catch (System.ComponentModel.Win32Exception)
        {
            throw new CommandLineException("Unable to locate 'bash' on PATH. Install Git for Windows (or ensure bash is on PATH) and try again.");
        }

        await process.WaitForExitAsync();
        return process.ExitCode;
    }

    private static string? _resolvedBashPath;

    private static string ResolveBashExecutable()
    {
        if (!OperatingSystem.IsWindows())
        {
            return "bash";
        }

        if (_resolvedBashPath is not null)
        {
            return _resolvedBashPath;
        }

        // On Windows, an unqualified "bash" can resolve to the WSL launcher stub in
        // System32 instead of Git Bash, because Win32 process creation searches
        // System32 before PATH-listed directories. The WSL stub cannot open Windows
        // drive paths (it needs /mnt/d/... instead of D:/...), so it must be skipped
        // explicitly in favor of a real POSIX bash (Git Bash, Cygwin, etc).
        var systemRoot = Environment.GetEnvironmentVariable("SystemRoot") ?? @"C:\Windows";
        var excludedPrefixes = new[]
        {
            Path.Combine(systemRoot, "System32"),
            Path.Combine(systemRoot, "SysWOW64"),
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Microsoft", "WindowsApps"),
        };

        var pathEntries = (Environment.GetEnvironmentVariable("PATH") ?? string.Empty)
            .Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries);

        foreach (var directory in pathEntries)
        {
            var candidate = Path.Combine(directory.Trim(), "bash.exe");
            if (!File.Exists(candidate))
            {
                continue;
            }

            if (excludedPrefixes.Any(prefix => candidate.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)))
            {
                continue;
            }

            _resolvedBashPath = candidate;
            return candidate;
        }

        throw new CommandLineException(
            "Unable to locate a usable 'bash' on PATH (only a WSL launcher stub was found, which cannot resolve Windows paths). Install Git for Windows and ensure its bin directory is on PATH.");
    }

    private static async Task InstallHookAsync(ToolBundle bundle, string target, bool force)
    {
        if (!IsGitRepository(target))
        {
            throw new CommandLineException($"'{target}' is not a git repository; skipping hook install for '{bundle.Name}'.");
        }

        var content = File.ReadAllText(bundle.PostCommitScriptPath!).Replace("\r\n", "\n");
        var hooksDir = Path.Combine(target, ".githooks");
        var hookPath = Path.Combine(hooksDir, "post-commit");

        if (File.Exists(hookPath) && !force)
        {
            var existing = File.ReadAllText(hookPath).Replace("\r\n", "\n");
            if (!string.Equals(existing, content, StringComparison.Ordinal))
            {
                throw new CommandLineException($"'{hookPath}' already exists with different content. Re-run with '--force' to replace it.");
            }
        }

        Directory.CreateDirectory(hooksDir);
        await File.WriteAllTextAsync(hookPath, content);

        if (!OperatingSystem.IsWindows())
        {
            File.SetUnixFileMode(
                hookPath,
                UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute
                | UnixFileMode.GroupRead | UnixFileMode.GroupExecute
                | UnixFileMode.OtherRead | UnixFileMode.OtherExecute);
        }

        var currentHooksPath = await TryGetGitConfigAsync(target, "core.hooksPath");
        if (currentHooksPath is not null
            && !string.Equals(currentHooksPath.Trim(), ".githooks", StringComparison.Ordinal)
            && !force)
        {
            throw new CommandLineException(
                $"'{target}' already has core.hooksPath set to '{currentHooksPath.Trim()}'. Re-run with '--force' to override.");
        }

        await RunGitAsync(target, "add", "--", ".githooks/post-commit");
        await RunGitAsync(target, "update-index", "--chmod=+x", "--", ".githooks/post-commit");
        await RunGitAsync(target, "config", "core.hooksPath", ".githooks");

        Console.WriteLine($"[{target}] {bundle.Name}: hook installed at {hookPath}");
    }

    private static async Task MergeGitignoreAsync(ToolBundle bundle, string target)
    {
        var entries = (await File.ReadAllLinesAsync(bundle.GitignorePath!))
            .Select(static line => line.TrimEnd())
            .Where(static line => line.Length > 0)
            .ToArray();

        if (entries.Length == 0)
        {
            return;
        }

        var gitignorePath = Path.Combine(target, ".gitignore");
        var existingLines = File.Exists(gitignorePath)
            ? (await File.ReadAllLinesAsync(gitignorePath)).Select(static line => line.TrimEnd()).ToList()
            : new List<string>();

        var existingSet = new HashSet<string>(existingLines, StringComparer.Ordinal);
        var missing = entries.Where(entry => !existingSet.Contains(entry)).ToArray();

        if (missing.Length == 0)
        {
            return;
        }

        var updatedLines = new List<string>(existingLines);
        if (updatedLines.Count > 0)
        {
            updatedLines.Add(string.Empty);
        }

        updatedLines.Add($"# {bundle.Name}");
        updatedLines.AddRange(missing);

        await File.WriteAllTextAsync(gitignorePath, string.Join('\n', updatedLines) + "\n");

        Console.WriteLine($"[{target}] {bundle.Name}: added {missing.Length} entr{(missing.Length == 1 ? "y" : "ies")} to .gitignore");
    }

    private static bool IsGitRepository(string path)
    {
        var gitPath = Path.Combine(path, ".git");
        return Directory.Exists(gitPath) || File.Exists(gitPath);
    }

    private static async Task RunGitAsync(string workingDirectory, params string[] args)
    {
        var (exitCode, _, stderr) = await RunProcessCaptureAsync("git", workingDirectory, args);
        if (exitCode != 0)
        {
            throw new CommandLineException($"'git {string.Join(' ', args)}' failed in '{workingDirectory}': {stderr.Trim()}");
        }
    }

    private static async Task<string?> TryGetGitConfigAsync(string workingDirectory, string key)
    {
        var (exitCode, stdout, _) = await RunProcessCaptureAsync("git", workingDirectory, new[] { "config", "--get", key });
        return exitCode == 0 ? stdout : null;
    }

    private static async Task<(int ExitCode, string Stdout, string Stderr)> RunProcessCaptureAsync(
        string fileName,
        string workingDirectory,
        IReadOnlyList<string> args)
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = fileName,
            WorkingDirectory = workingDirectory,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
        };

        foreach (var arg in args)
        {
            startInfo.ArgumentList.Add(arg);
        }

        using var process = new Process { StartInfo = startInfo };

        try
        {
            process.Start();
        }
        catch (System.ComponentModel.Win32Exception)
        {
            throw new CommandLineException($"Unable to locate '{fileName}' on PATH.");
        }

        var stdoutTask = process.StandardOutput.ReadToEndAsync();
        var stderrTask = process.StandardError.ReadToEndAsync();
        await process.WaitForExitAsync();

        return (process.ExitCode, await stdoutTask, await stderrTask);
    }
}
