using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

HostApplicationBuilder builder = Host.CreateApplicationBuilder(args);
builder.Services.AddHostedService<Services.CheckupdatesService>();
builder.Services.AddSingleton<Services.CheckupdatesService>();
builder.Services.AddHostedService<Services.HttpService>();
builder.Services.AddHostedService<Services.PrometheusService>();

builder.Services.Configure<Options.Checkupdates>(builder.Configuration.GetSection(nameof(Options.Checkupdates)));
builder.Services.Configure<Options.Http>(builder.Configuration.GetSection(nameof(Options.Http)));
builder.Services.Configure<Options.Prometheus>(builder.Configuration.GetSection(nameof(Options.Prometheus)));

var host = builder.Build();
await host.RunAsync();
