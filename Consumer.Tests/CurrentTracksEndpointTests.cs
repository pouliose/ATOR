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
    public async Task GetHistory_ReturnsHistoryForTrackIdAtVersionedRoute()
    {
        var timestamp = new DateTimeOffset(2026, 10, 23, 12, 0, 0, TimeSpan.Zero);
        var expectedResponse = new TrackHistoryResponse(
            "track-1",
            2,
            false,
            null,
            [
                new TrackPositionResponse("source-1", "track-1", timestamp, 10, 20, 300, 40),
                new TrackPositionResponse("source-2", "track-1", timestamp.AddMinutes(1), 11, 21, 310, 41)
            ]);

        await using var app = await CreateAppAsync(
            new CurrentTracksResponse(timestamp, 0, []),
            expectedResponse);
        using var client = app.GetTestClient();

        using var response = await client.GetAsync(
            "/api/v1/tracks/track-1/history?limit=2" +
            "&beforeTimestampUtc=2026-10-23T12%3A00%3A00Z&beforeSourceId=source-0");
        var actualResponse = await response.Content.ReadFromJsonAsync<TrackHistoryResponse>();
        var service = Assert.IsType<StubTracksService>(
            app.Services.GetRequiredService<ITracksService>());

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(actualResponse);
        Assert.Equal(expectedResponse.TrackId, actualResponse.TrackId);
        Assert.Equal(expectedResponse.ReportCount, actualResponse.ReportCount);
        Assert.Equal(expectedResponse.HasMore, actualResponse.HasMore);
        Assert.Equal(expectedResponse.NextCursor, actualResponse.NextCursor);
        Assert.Equal(expectedResponse.Reports, actualResponse.Reports);
        Assert.Equal(2, service.PageSize);
        Assert.Equal(timestamp, service.BeforeTimestampUtc);
        Assert.Equal("source-0", service.BeforeSourceId);
    }

    [Theory]
    [InlineData("?limit=0")]
    [InlineData("?limit=501")]
    [InlineData("?beforeTimestampUtc=2026-10-23T12%3A00%3A00Z")]
    [InlineData("?beforeSourceId=source-1")]
    public async Task GetHistory_ReturnsBadRequestForInvalidPagination(string query)
    {
        await using var app = await CreateAppAsync(
            new CurrentTracksResponse(DateTimeOffset.UtcNow, 0, []));
        using var client = app.GetTestClient();

        using var response = await client.GetAsync($"/api/v1/tracks/track-1/history{query}");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal(
            "application/problem+json",
            response.Content.Headers.ContentType?.MediaType);
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
        var historyOperation = root
            .GetProperty("paths")
            .GetProperty("/api/v1/tracks/{trackId}/history")
            .GetProperty("get");

        Assert.Equal(HttpStatusCode.OK, documentResponse.StatusCode);
        Assert.Equal("application/json", documentResponse.Content.Headers.ContentType?.MediaType);
        Assert.True(root.TryGetProperty("openapi", out _));
        Assert.Equal(
            "Get the latest known position of every track",
            operation.GetProperty("summary").GetString());
        Assert.True(operation.GetProperty("responses").TryGetProperty("200", out _));
        Assert.Equal("Get telemetry history for a track ID", historyOperation.GetProperty("summary").GetString());
        Assert.True(historyOperation.GetProperty("responses").TryGetProperty("200", out _));
    }

    private static async Task<WebApplication> CreateAppAsync(
        CurrentTracksResponse response,
        TrackHistoryResponse? historyResponse = null,
        bool includeOpenApi = false)
    {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        if (includeOpenApi)
        {
            builder.Services.AddOpenApi();
        }

        builder.Services.AddSingleton<ITracksService>(
            new StubTracksService(response, historyResponse));

        var app = builder.Build();
        if (includeOpenApi)
        {
            app.MapOpenApi();
        }

        app.MapCurrentTracksEndpoints();
        await app.StartAsync();
        return app;
    }

    private sealed class StubTracksService(
        CurrentTracksResponse response,
        TrackHistoryResponse? historyResponse) : ITracksService
    {
        public int PageSize { get; private set; }

        public DateTimeOffset? BeforeTimestampUtc { get; private set; }

        public string? BeforeSourceId { get; private set; }

        public Task<CurrentTracksResponse> GetCurrentAsync(CancellationToken cancellationToken) =>
            Task.FromResult(response);

        public Task<TrackHistoryResponse> GetHistoryAsync(
            string trackId,
            int pageSize,
            DateTimeOffset? beforeTimestampUtc,
            string? beforeSourceId,
            CancellationToken cancellationToken)
        {
            PageSize = pageSize;
            BeforeTimestampUtc = beforeTimestampUtc;
            BeforeSourceId = beforeSourceId;
            return Task.FromResult(historyResponse ?? new TrackHistoryResponse(trackId, 0, false, null, []));
        }
    }
}
