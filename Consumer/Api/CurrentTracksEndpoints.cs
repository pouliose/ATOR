using ATOR.Services;
using Microsoft.AspNetCore.Http.HttpResults;

namespace ATOR.Api;

public static class CurrentTracksEndpoints
{
    public static WebApplication MapCurrentTracksEndpoints(this WebApplication app)
    {
        app.MapGet(
                "/api/v1/tracks/current",
                async Task<Ok<CurrentTracksResponse>> (
                    ITracksService tracksService,
                    CancellationToken cancellationToken) =>
                    TypedResults.Ok(await tracksService.GetCurrentAsync(cancellationToken)))
            .WithName("GetCurrentTrackPositions")
            .WithTags("Tracks")
            .WithSummary("Get the latest known position of every track")
            .WithDescription("Returns the latest stored telemetry position for each source and track.")
            .Produces<CurrentTracksResponse>(StatusCodes.Status200OK);

        app.MapGet(
            "/api/v1/tracks/{trackId}/history",
            async Task<Results<Ok<TrackHistoryResponse>, ProblemHttpResult>> (
                string trackId,
                int? limit,
                DateTimeOffset? beforeTimestampUtc,
                string? beforeSourceId,
                ITracksService tracksService,
                CancellationToken cancellationToken) =>
            {
                var pageSize = limit ?? 100;
                if (pageSize is < 1 or > 500)
                {
                    return TypedResults.Problem(
                        statusCode: StatusCodes.Status400BadRequest,
                        title: "Invalid history page size",
                        detail: "The limit must be between 1 and 500.");
                }

                if ((beforeTimestampUtc is null) != (beforeSourceId is null) ||
                    (beforeSourceId is not null && string.IsNullOrWhiteSpace(beforeSourceId)))
                {
                    return TypedResults.Problem(
                        statusCode: StatusCodes.Status400BadRequest,
                        title: "Invalid history cursor",
                        detail: "Provide both beforeTimestampUtc and beforeSourceId to fetch the next page.");
                }

                var history = await tracksService.GetHistoryAsync(
                    trackId,
                    pageSize,
                    beforeTimestampUtc,
                    beforeSourceId,
                    cancellationToken);
                return TypedResults.Ok(history);
            })
            .WithName("GetTrackHistory")
            .WithTags("Tracks")
            .WithSummary("Get telemetry history for a track ID")
            .WithDescription(
                "Returns the newest accepted telemetry reports first, with a maximum of 500 per page. " +
                "Use the returned nextCursor values as beforeTimestampUtc and beforeSourceId to fetch older reports. " +
                "If multiple sources use the same track ID, reports from all matching sources are included.")
            .Produces<TrackHistoryResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest);

        return app;
    }
}
