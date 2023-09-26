using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Prometheus;

namespace Services;

public class MetricsService : BackgroundService
{
    private readonly MetricServer metricServer = null!;
    private readonly ILogger<MetricsService> logger = null!;

    public MetricsService(IOptions<Options.Metrics> options, ILogger<MetricsService> logger)
    {
        this.logger = logger;

        var port = options.Value.Port!.Value;
        metricServer = new MetricServer(port: port);
    }

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogDebug("Starting metrics server...");
        metricServer.Start();
        logger.LogInformation("Started metrics server");

        return Task.CompletedTask;
    }
}
