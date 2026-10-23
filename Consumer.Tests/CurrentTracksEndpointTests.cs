using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using ATOR.Api;
using ATOR.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace ATOR.Consumer.Tests;

public sealed class CurrentTracksEndpointTests
{
    [Fact]
    public async Task GetCurrent_ReturnsCurrentTracksAtVersionedRoute()
    {
        var generatedAt = new DateTimeOffset(2026, 10, 23, 12, 0, 0, TimeSpan.Zero);
        var expectedResponse = new CurrentTracksResponse(
            generatedAt,
            1,
            [new TrackPositionResponse("source-1", "track-1", generatedAt, 10, 20, 300, 40)]);

        await using var app = await CreateAppAsync(expectedResponse);
        using var client = app.GetTestClient();

        using var response = await client.GetAsync("/api/v1/tracks/current");
        var actualResponse = await response.Content.ReadFromJsonAsync<CurrentTracksResponse>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(actualResponse);
        Assert.Equal(expectedResponse.GeneratedAtUtc, actualResponse.GeneratedAtUtc);
        Assert.Equal(expectedResponse.TrackCount, actualResponse.TrackCount);
        Assert.Equal(expectedResponse.Tracks, actualResponse.Tracks);
    }

    [Fact]
    public async Task GetCurrent_ReturnsNotFoundForUnversionedRoute()
    {
        await using var app = await CreateAppAsync(
            new CurrentTracksResponse(DateTimeOffset.UtcNow, 0, []));
        using var client = app.GetTestClient();

        using var response = await client.GetAsync("/api/tracks/current");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task OpenApiDocument_IsServedAndDescribesVersionedCurrentTracksEndpoint()
    {
        await using var app = await CreateAppAsync(
            new CurrentTracksResponse(DateTimeOffset.UtcNow, 0, []),
            includeOpenApi: true);
        using var client = app.GetTestClient();

        using var documentResponse = await client.GetAsync("/openapi/v1.json");
        await using var documentStream = await documentResponse.Content.ReadAsStreamAsync();
        using var document = await JsonDocument.ParseAsync(documentStream);
        var root = document.RootElement;
        var operation = root
            .GetProperty("paths")
            .GetProperty("/api/v1/tracks/current")
            .GetProperty("get");

        Assert.Equal(HttpStatusCode.OK, documentResponse.StatusCode);
        Assert.Equal("application/json", documentResponse.Content.Headers.ContentType?.MediaType);
        Assert.True(root.TryGetProperty("openapi", out _));
        Assert.Equal(
            "Get the latest known position of every track",
            operation.GetProperty("summary").GetString());
        Assert.True(operation.GetProperty("responses").TryGetProperty("200", out _));
    }

    private static async Task<WebApplication> CreateAppAsync(
        CurrentTracksResponse response,
        bool includeOpenApi = false)
    {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        if (includeOpenApi)
        {
            builder.Services.AddOpenApi();
        }

        builder.Services.AddSingleton<ICurrentTracksService>(new FakeCurrentTracksService(response));

        var app = builder.Build();
        if (includeOpenApi)
        {
            app.MapOpenApi();
        }

        app.MapCurrentTracksEndpoints();
        await app.StartAsync();
        return app;
    }

    private sealed class FakeCurrentTracksService(CurrentTracksResponse response) : ICurrentTracksService
    {
        public Task<CurrentTracksResponse> GetCurrentAsync(CancellationToken cancellationToken) =>
            Task.FromResult(response);
    }
}
