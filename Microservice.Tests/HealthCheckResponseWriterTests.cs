using System.Text.Json;
using Microservice.Health;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Microservice.Tests;

public class HealthCheckResponseWriterTests
{
    [Fact]
    public async Task WriteAsync_WritesExpectedJsonPayload()
    {
        var context = new DefaultHttpContext();
        await using var body = new MemoryStream();
        context.Response.Body = body;

        var report = new HealthReport(
            new Dictionary<string, HealthReportEntry>
            {
                ["sqlite"] = new(
                    HealthStatus.Healthy,
                    "SQLite is reachable.",
                    TimeSpan.FromMilliseconds(12),
                    exception: null,
                    data: new Dictionary<string, object>())
            },
            TimeSpan.FromMilliseconds(12));

        await HealthCheckResponseWriter.WriteAsync(context, report);

        body.Position = 0;
        var json = await new StreamReader(body).ReadToEndAsync();
        using var document = JsonDocument.Parse(json);

        Assert.Equal("application/json", context.Response.ContentType);
        Assert.Equal("Healthy", document.RootElement.GetProperty("status").GetString());

        var checks = document.RootElement.GetProperty("checks");
        Assert.Equal(1, checks.GetArrayLength());
        Assert.Equal("sqlite", checks[0].GetProperty("name").GetString());
        Assert.Equal("Healthy", checks[0].GetProperty("status").GetString());
        Assert.Equal("SQLite is reachable.", checks[0].GetProperty("description").GetString());
    }
}
