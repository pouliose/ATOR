using ATOR.Api;
using ATOR.Models;
using ATOR.Reports;
using ATOR.Services;
using Xunit;

namespace ATOR.Consumer.Tests;

public sealed class TracksServiceTests
{
    [Fact]
    public async Task GetCurrentAsync_ReturnsLatestPositionsAndForwardsCancellationToken()
    {
        var generatedAt = new DateTimeOffset(2026, 10, 23, 12, 0, 0, TimeSpan.Zero);
        var timestamp = generatedAt.AddMinutes(-1);
        var expectedTrack = new TrackReport("source-1", "track-1", timestamp, 10, 20, 300, 40);
        using var cancellationSource = new CancellationTokenSource();
        var reader = new StubCurrentTracksReader(
            new CurrentTracksReport(generatedAt, 1, [expectedTrack]));
        var service = new TracksService(reader);

        var response = await service.GetCurrentAsync(cancellationSource.Token);

        Assert.Equal(generatedAt, response.GeneratedAtUtc);
        Assert.Equal(1, response.TrackCount);
        Assert.Equal(cancellationSource.Token, reader.CancellationToken);
        Assert.Equal(
            new TrackPositionResponse("source-1", "track-1", timestamp, 10, 20, 300, 40),
            Assert.Single(response.Tracks));
    }

    [Fact]
    public async Task GetHistoryAsync_MapsReportsAndForwardsCancellationToken()
    {
        var timestamp = new DateTimeOffset(2026, 10, 23, 12, 0, 0, TimeSpan.Zero);
        var expectedReports = new[]
        {
            new TrackReport("source-1", "track-1", timestamp, 10, 20, 300, 40),
            new TrackReport("source-2", "track-1", timestamp.AddMinutes(1), 11, 21, 310, 41)
        };
        var beforeTimestampUtc = timestamp.AddMinutes(-1);
        using var cancellationSource = new CancellationTokenSource();
        var reader = new StubCurrentTracksReader(
            new CurrentTracksReport(timestamp, 0, []),
            new TrackHistoryPage(expectedReports, true));
        var service = new TracksService(reader);

        var response = await service.GetHistoryAsync(
            "track-1",
            2,
            beforeTimestampUtc,
            "source-0",
            cancellationSource.Token);

        Assert.Equal("track-1", response.TrackId);
        Assert.Equal(2, response.ReportCount);
        Assert.True(response.HasMore);
        Assert.Equal(
            new TrackHistoryCursor(expectedReports[^1].TimestampUtc, expectedReports[^1].SourceId),
            response.NextCursor);
        Assert.Equal(cancellationSource.Token, reader.CancellationToken);
        Assert.Equal("track-1", reader.HistoryTrackId);
        Assert.Equal(2, reader.PageSize);
        Assert.Equal(beforeTimestampUtc, reader.BeforeTimestampUtc);
        Assert.Equal("source-0", reader.BeforeSourceId);
        Assert.Equal(
            expectedReports.Select(report => new TrackPositionResponse(
                report.SourceId,
                report.TrackId,
                report.TimestampUtc,
                report.LatitudeDegrees,
                report.LongitudeDegrees,
                report.AltitudeMeters,
                report.GroundSpeedMetersPerSecond)),
            response.Reports);
    }

    private sealed class StubCurrentTracksReader(
        CurrentTracksReport report,
        TrackHistoryPage? history = null) : ICurrentTracksReader
    {
        public CancellationToken CancellationToken { get; private set; }

        public string? HistoryTrackId { get; private set; }

        public int PageSize { get; private set; }

        public DateTimeOffset? BeforeTimestampUtc { get; private set; }

        public string? BeforeSourceId { get; private set; }

        public Task<CurrentTracksReport> CreateReportAsync(CancellationToken cancellationToken)
        {
            CancellationToken = cancellationToken;
            return Task.FromResult(report);
        }

        public Task<TrackHistoryPage> GetHistoryAsync(
            string trackId,
            int pageSize,
            DateTimeOffset? beforeTimestampUtc,
            string? beforeSourceId,
            CancellationToken cancellationToken)
        {
            CancellationToken = cancellationToken;
            HistoryTrackId = trackId;
            PageSize = pageSize;
            BeforeTimestampUtc = beforeTimestampUtc;
            BeforeSourceId = beforeSourceId;
            return Task.FromResult(history ?? new TrackHistoryPage([], false));
        }
    }
}
