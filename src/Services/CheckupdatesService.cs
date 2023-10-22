using System.Text.RegularExpressions;
using Microsoft.Extensions.Options;
using Models;

namespace Services;

public class CheckupdatesService : BackgroundService
{
    private readonly FileSystemWatcher watcher = null!;
    private const string versionRegex = @"^(?:.*:)?(?<major>\d+)(?:\.(?<minor>\d+))?(?:\.(?<patch>\d+))?((?:-|.|\+)(?<prerelease>.+))?$";
    private readonly string filePath = null!;
    private readonly Task startupTask = null!;
    private HashSet<Update> updates = new();

    private readonly ILogger<CheckupdatesService> logger = null!;
    public event EventHandler<UpdatesChangedEventArgs>? UpdatesChanged;

    public CheckupdatesService(IOptions<Options.Checkupdates> options, ILogger<CheckupdatesService> logger)
    {
        this.logger = logger;

        string directory = options.Value.Directory;
        string filename = options.Value.Filename;
        filePath = Path.Combine(directory, filename);
        logger.LogInformation("Creating file watcher for {FilePath}...", filePath);
        watcher = new(directory, filename);

        startupTask = Task.Run(Startup);
    }

    public async Task<HashSet<Update>> GetCurrentUpdates(bool forceUpdate = false)
    {
        await startupTask;

        if (forceUpdate)
            DoUpdate(filePath);

        return new(updates, updates.Comparer);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await startupTask;
    }

    protected virtual void OnUpdatesChanged(UpdatesChangedEventArgs e)
    {
        EventHandler<UpdatesChangedEventArgs>? handler = UpdatesChanged;
        if (handler != null)
        {
            handler(this, e);
        }
    }

    private void Startup()
    {
        if (File.Exists(filePath))
        {
            logger.LogInformation("File {FilePath} exists, doing initial update...", filePath);
            DoUpdate(filePath);
        }
        else
            logger.LogWarning("File {FilePath} does not exist, skipping initial update...", filePath);

        logger.LogInformation("Watching file {FilePath} for changes...", filePath);
        watcher.Changed += OnFileChanged;
        watcher.NotifyFilter = NotifyFilters.LastWrite;
        watcher.EnableRaisingEvents = true;
    }

    private void OnFileChanged(object sender, FileSystemEventArgs e)
    {
        logger.LogDebug("File {FilePath} changed ({ChangeType})", e.FullPath, e.ChangeType);

        DoUpdate(e.FullPath);
        logger.LogInformation("File {FilePath} updated, {UpdateCount} updates found", e.FullPath, updates.Count);

        OnUpdatesChanged(new(updates));
    }

    private void DoUpdate(string filePath)
    {
        var changes = File.ReadLines(filePath)
            .Select(GetChangeParameters)
            .ToHashSet();

        logger.LogDebug("Found {UpdateCount} updates", changes.Count);

        lock (updates)
            updates = changes;
    }

    private Update GetChangeParameters(string updateLine)
    {
        logger.LogTrace("Parsing line: {Line}", updateLine);

        var split = updateLine.Split(' ');
        var name = split[0];
        var beforeVersion = split[1];
        // split[2] is "->"
        var afterVersion = split[3];

        var bv = Regex.Match(beforeVersion, versionRegex);
        var av = Regex.Match(afterVersion, versionRegex);

        foreach (var level in new[] { "major", "minor", "patch", "prerelease" })
            if (bv.Groups[level]?.Value != av.Groups[level]?.Value)
                return new(name, level);

        return new(name, "Unknown");
    }

    public class UpdatesChangedEventArgs : EventArgs
    {
        public UpdatesChangedEventArgs(HashSet<Update> updates)
        {
            Updates = new(updates, updates.Comparer);
        }

        public HashSet<Update> Updates { get; }
    }
}
