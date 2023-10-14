using System.Net;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Options;

namespace Services;

public class JsonService : BackgroundService
{
    private readonly HttpListener listener = new HttpListener();
    private Task? listenTask;

    private readonly IOptions<Json> options = null!;
    private readonly CheckupdatesService checkupdatesService = null!;
    private readonly ILogger<JsonService> logger = null!;

    public JsonService(IOptions<Options.Json> options, CheckupdatesService checkupdatesService, ILogger<JsonService> logger)
    {
        this.options = options;
        this.checkupdatesService = checkupdatesService;
        this.logger = logger;
    }

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var port = options.Value.Port!.Value;
        string uriPrefix = $"http://localhost:{port}/";
        logger.LogInformation("Starting HTTP listener on {UriPrefix}...", uriPrefix);
        listener.Prefixes.Add(uriPrefix);
        listener.Start();

        listenTask = Task.Factory.StartNew(() => StartListen(stoppingToken), stoppingToken);

        return Task.CompletedTask;
    }

    private void StartListen(CancellationToken stoppingToken)
    {
        logger.LogDebug("Listening for HTTP requests...");

        while (!stoppingToken.IsCancellationRequested)
        {
            var task = listener.GetContextAsync().ContinueWith(OnContext);
            logger.LogDebug("Waiting for HTTP request...");
            task.Wait(stoppingToken);
        }

        //TODO: Figure out why this is never reached
        logger.LogInformation("Stopped listening for HTTP requests");
    }

    private async Task OnContext(Task<HttpListenerContext> contextTask)
    {
        logger.LogDebug("Handling HTTP request...");

        var updates = checkupdatesService.GetCurrentUpdates();
        var json = JsonSerializer.Serialize(updates);

        var context = await contextTask;

        context.Response.ContentType = "application/json";
        context.Response.ContentLength64 = Encoding.UTF8.GetByteCount(json);
        context.Response.OutputStream.Write(Encoding.UTF8.GetBytes(json));
        context.Response.OutputStream.Close();

        logger.LogInformation("Handled HTTP request");
    }
}
