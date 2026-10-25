using System.Net;
using System.Net.Http.Json;
using ATOR.Api;
using ATOR.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace ATOR.Consumer.Tests;

public sealed class AlertsEndpointTests
{
    [Fact]
    public async Task GetAlerts_ReturnsRecentAlertsAndForwardsLimit()
    {
        var occurredAt = new DateTimeOffset(2026, 10, 25, 12, 0, 0, TimeSpan.Zero);
        var expected = new TrackAlertResponse(
            7,
            "zone_entry",
            "source-1",
            "track-1",
            occurredAt,
            occurredAt,
            "zone-1",
            "Track entered restricted zone 'zone-1'.");
        var service = new StubAlertsService([expected]);
        await using var app = await CreateAppAsync(service);
        using var client = app.GetTestClient();

        using var response = await client.GetAsync("/api/v1/alerts?limit=25");
        var alerts = await response.Content.ReadFromJsonAsync<TrackAlertResponse[]>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(25, service.PageSize);
        Assert.Equal(new[] { expected }, alerts);
    }

    [Theory]
    [InlineData("?limit=0")]
    [InlineData("?limit=501")]
    public async Task GetAlerts_ReturnsBadRequestForInvalidLimit(string query)
    {
        await using var app = await CreateAppAsync(new StubAlertsService([]));
        using var client = app.GetTestClient();

        using var response = await client.GetAsync($"/api/v1/alerts{query}");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
    }

    private static async Task<WebApplication> CreateAppAsync(StubAlertsService service)
    {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        builder.Services.AddSingleton<IAlertsService>(service);

        var app = builder.Build();
        app.MapAlertEndpoints();
        await app.StartAsync();
        return app;
    }

    private sealed class StubAlertsService(IReadOnlyList<TrackAlertResponse> alerts) : IAlertsService
    {
        public int PageSize { get; private set; }

        public Task<IReadOnlyList<TrackAlertResponse>> GetAlertsAsync(
            int pageSize,
            CancellationToken cancellationToken)
        {
            PageSize = pageSize;
            return Task.FromResult(alerts);
        }
    }
}
