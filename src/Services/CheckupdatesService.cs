using System.Text.RegularExpressions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Models;

namespace Services;

public class CheckupdatesService : BackgroundService
{
    private readonly FileSystemWatcher watcher = null!;
    private const string versionRegex = @"^(?:.*:)?(?<major>\d+)(?:\.(?<minor>\d+))?(?:\.(?<patch>\d+))?((?:-|.|\+)(?<prerelease>.+))?$";
    private HashSet<Update> updates = new();

    private readonly ILogger<CheckupdatesService> logger = null!;
    public event EventHandler<UpdatesChangedEventArgs>? UpdatesChanged;

    public CheckupdatesService(IOptions<Options.Checkupdates> options, ILogger<CheckupdatesService> logger)
    {
        this.logger = logger;

        string directory = options.Value.Directory;
        string filename = options.Value.Filename;
        logger.LogInformation("Creating file watcher for {Directory}/{Filename}...", directory, filename);
        watcher = new(directory, filename);
    }

    public HashSet<Update> GetCurrentUpdates()
    {
        return updates;
    }

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        string file = Path.Combine(watcher.Path, watcher.Filter);

        logger.LogInformation("Watching file {FilePath} for changes...", file);
        watcher.Changed += OnFileChanged;
        watcher.NotifyFilter = NotifyFilters.LastWrite;
        watcher.EnableRaisingEvents = true;

        return Task.CompletedTask;
    }

    protected virtual void OnUpdatesChanged(UpdatesChangedEventArgs e)
    {
        EventHandler<UpdatesChangedEventArgs>? handler = UpdatesChanged;
        if (handler != null)
        {
            handler(this, e);
        }
    }

    private void OnFileChanged(object sender, FileSystemEventArgs e)
    {
        logger.LogInformation("File changed ({ChangeType})", e.ChangeType);

        var updates = File.ReadLines(e.FullPath)
            .Select(GetChangeParameters)
            .ToHashSet();

        OnUpdatesChanged(new(updates));
    }

    private Update GetChangeParameters(string updateLine)
    {
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
            Updates = updates;
        }

        public HashSet<Update> Updates { get; }
    }
}
