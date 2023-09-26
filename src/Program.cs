using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

HostApplicationBuilder builder = Host.CreateApplicationBuilder(args);
builder.Services.AddHostedService<Services.MetricsService>();
builder.Services.AddHostedService<Services.CheckupdatesService>();

builder.Services.Configure<Options.Checkupdates>(builder.Configuration.GetSection(nameof(Options.Checkupdates)));
builder.Services.Configure<Options.Metrics>(builder.Configuration.GetSection(nameof(Options.Metrics)));

var host = builder.Build();
await host.RunAsync();
