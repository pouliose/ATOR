using ATOR.Api;
using ATOR.Models;
using ATOR.Reports;
using ATOR.Services;
using Xunit;

namespace ATOR.Consumer.Tests;

public sealed class CurrentTracksServiceTests
{
    [Fact]
    public async Task GetCurrentAsync_ReturnsLatestPositionsAndForwardsCancellationToken()
    {
        var generatedAt = new DateTimeOffset(2026, 10, 23, 12, 0, 0, TimeSpan.Zero);
        var timestamp = generatedAt.AddMinutes(-1);
        var expectedTrack = new TrackReport("source-1", "track-1", timestamp, 10, 20, 300, 40);
        using var cancellationSource = new CancellationTokenSource();
        var reader = new FakeCurrentTracksReader(
            new CurrentTracksReport(generatedAt, 1, [expectedTrack]));
        var service = new CurrentTracksService(reader);

        var response = await service.GetCurrentAsync(cancellationSource.Token);

        Assert.Equal(generatedAt, response.GeneratedAtUtc);
        Assert.Equal(1, response.TrackCount);
        Assert.Equal(cancellationSource.Token, reader.CancellationToken);
        Assert.Equal(
            new TrackPositionResponse("source-1", "track-1", timestamp, 10, 20, 300, 40),
            Assert.Single(response.Tracks));
    }

    private sealed class FakeCurrentTracksReader(CurrentTracksReport report) : ICurrentTracksReader
    {
        public CancellationToken CancellationToken { get; private set; }

        public Task<CurrentTracksReport> CreateReportAsync(CancellationToken cancellationToken)
        {
            CancellationToken = cancellationToken;
            return Task.FromResult(report);
        }
    }
}
