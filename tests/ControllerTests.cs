using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;
using FluentAssertions;
using System.Net;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Microsoft.Extensions.DependencyInjection;

namespace Tests;

public class ControllerTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly static string dataLocation = $"{Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)}/data";
    private readonly static string fileName = "checkupdates.txt";
    private readonly static string filePath = $"{dataLocation}/{fileName}";

    private readonly WebApplicationFactory<Program> _factory;

    public ControllerTests(WebApplicationFactory<Program> factory)
    {
        Directory.CreateDirectory(dataLocation);
        File.Create(filePath).Dispose();

        _factory = factory.WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Development");
            builder.ConfigureAppConfiguration((context, config) =>
            {
                config.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["Checkupdates:Directory"] = dataLocation,
                    ["Checkupdates:Filename"] = fileName
                });
            });
        });
    }

    public void Dispose()
    {
        _factory?.Dispose();
        File.Delete(filePath);
    }

    [Fact]
    public async Task Get_Healthz__Responds_Ok()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/healthz");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Content.Headers.ContentType.Should().NotBeNull();
        response.Content.Headers.ContentType?.ToString().Should().Be("text/plain");
        (await response.Content.ReadAsStringAsync()).Should().Be("Healthy");
    }

    [Fact]
    public async Task Get_Data__Empty_File__Responds_Empty()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/data");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        (await response.Content.ReadAsStringAsync()).Should().Be("[]");
    }

    [Fact]
    public async Task Get_Data__Nonempty_File__Responds_Nonempty()
    {
        var checkupdatesService = _factory.Services.GetRequiredService<Services.CheckupdatesService>();
        File.WriteAllText(filePath, "test 1.0.0 -> 1.1.0");
        await checkupdatesService.GetCurrentUpdates(true);

        var client = _factory.CreateClient();

        var response = await client.GetAsync("/data");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        (await response.Content.ReadAsStringAsync()).Should().NotBe("[]");
    }
}
