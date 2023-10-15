using System.Net;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Options;

namespace Services;

public class HttpService : BackgroundService
{
    private readonly HttpListener listener = new HttpListener();
    private Task? listenTask;

    private readonly IOptions<Http> options = null!;
    private readonly CheckupdatesService checkupdatesService = null!;
    private readonly ILogger<HttpService> logger = null!;

    public HttpService(IOptions<Options.Http> options, CheckupdatesService checkupdatesService, ILogger<HttpService> logger)
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
        var context = await contextTask;
        try
        {
            var method = new HttpMethod(context.Request.HttpMethod);
            if (method != HttpMethod.Get)
            {
                logger.LogWarning("Received HTTP request with method {Method}, expected GET", method);
                context.Response.StatusCode = 405;
                return;
            }

            string? path = context.Request.Url?.AbsolutePath.ToLowerInvariant();
            logger.LogDebug("Handling HTTP request {Method} {Path}...", method, path);
            switch (path)
            {
                case "/data":
                    ReturnData(context);
                    break;
                case "/health":
                    ReturnHealth(context);
                    break;
                default:
                    context.Response.StatusCode = 404;
                    context.Response.Close();
                    break;
            }

            logger.LogInformation("Handled HTTP request {Method} {Path}", method, path);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error handling HTTP request");
            ReturnError(context);
        }
    }

    private void ReturnData(HttpListenerContext context)
    {
        var updates = checkupdatesService.GetCurrentUpdates();
        var json = JsonSerializer.Serialize(updates);

        context.Response.ContentType = "application/json";
        context.Response.ContentLength64 = Encoding.UTF8.GetByteCount(json);
        context.Response.OutputStream.Write(Encoding.UTF8.GetBytes(json));
        context.Response.OutputStream.Close();
    }

    private static void ReturnHealth(HttpListenerContext context)
    {
        var body = Encoding.UTF8.GetBytes("OK");
        context.Response.ContentType = "text/plain";
        context.Response.ContentLength64 = body.Length;
        context.Response.OutputStream.Write(body);
        context.Response.OutputStream.Close();
    }

    private static void ReturnError(HttpListenerContext context)
    {
        var body = Encoding.UTF8.GetBytes("InternalServerError");
        context.Response.StatusCode = 500;
        context.Response.ContentType = "text/plain";
        context.Response.ContentLength64 = body.Length;
        context.Response.OutputStream.Write(body);
        context.Response.OutputStream.Close();
    }
}
