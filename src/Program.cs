﻿using Prometheus;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddHealthChecks();

builder.Services.AddSingleton<Services.CheckupdatesService>();
builder.Services.AddHostedService<Services.CheckupdatesService>(p => p.GetRequiredService<Services.CheckupdatesService>());
builder.Services.AddHostedService<Services.PrometheusService>();

builder.Services.Configure<Options.Checkupdates>(builder.Configuration.GetSection(nameof(Options.Checkupdates)));

var app = builder.Build();

app.UseRouting();
if (app.Environment.IsProduction())
    app.UseHttpsRedirection();
else
    app.UseDeveloperExceptionPage();

app.UseEndpoints(e =>
{
    e.MapControllers();
    e.MapHealthChecks("/healthz");
    e.MapMetrics();
});

await app.RunAsync();

// Make available to tests
public partial class Program { }
