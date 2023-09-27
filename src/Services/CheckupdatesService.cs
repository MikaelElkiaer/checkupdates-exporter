using System.Text.RegularExpressions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Prometheus;

namespace Services;

public class CheckupdatesService : BackgroundService
{
    private readonly string[] metricLabels = new[] { "name", "level" };
    private readonly Gauge gauge = null!;
    private readonly FileSystemWatcher watcher = null!;
    private readonly string versionRegex = null!;

    private readonly ILogger<CheckupdatesService> logger = null!;

    public CheckupdatesService(IOptions<Options.Checkupdates> options, ILogger<CheckupdatesService> logger)
    {
        this.logger = logger;

        gauge = Metrics.CreateGauge(
            "checkupdates_available",
            "Available updates to applied",
            labelNames: metricLabels
        );
        string directory = options.Value.Directory;
        string filename = options.Value.Filename;
        logger.LogInformation("Creating file watcher for {Directory}/{Filename}...", directory, filename);
        watcher = new(directory, filename);

        versionRegex = options.Value.VersionRegex;
    }

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        string file = Path.Combine(watcher.Path, watcher.Filter);

        logger.LogInformation("Setting gauges for initial state...");
        SetGauges(file);

        logger.LogInformation("Watching file {FilePath} for changes...", file);
        watcher.Changed += OnChanged;
        watcher.NotifyFilter = NotifyFilters.LastWrite;
        watcher.EnableRaisingEvents = true;

        return Task.CompletedTask;
    }

    private void SetGauges(string filePath)
    {
        var updates = File.ReadLines(filePath)
            .Select(GetChangeParameters)
            .ToHashSet();

        IEnumerable<string[]> obsoleteGauges = gauge.GetAllLabelValues().Where(g => !updates.Contains(ToTuple(g)));
        foreach (var g in obsoleteGauges)
            this.gauge.WithLabels(g).Remove();
        logger.LogInformation("Removed {ObsoleteCount} obsolete gauge values", obsoleteGauges.Count());

        foreach (var u in updates)
            this.gauge.WithLabels(ToArray(u)).Set(1);
        logger.LogInformation("Set {UpdateCount} gauges", updates.Count);
    }

    private string[] ToArray((string Name, string Level) u) =>
        new string[] { u.Name, u.Level };

    private (string Name, string Level) GetChangeParameters(string updateLine)
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
                return (name, level);

        return (name, "Unknown");
    }

    private (string Name, string Level) ToTuple(string[] values) =>
    (
        values[0], values[1]
    );

    private void OnChanged(object sender, FileSystemEventArgs e)
    {
        logger.LogInformation("File changed ({ChangeType}) - setting gauges...", e.ChangeType);

        SetGauges(e.FullPath);
    }
}
