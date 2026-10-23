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
                    ICurrentTracksService currentTracksService,
                    CancellationToken cancellationToken) =>
                    TypedResults.Ok(await currentTracksService.GetCurrentAsync(cancellationToken)))
            .WithName("GetCurrentTrackPositions")
            .WithTags("Tracks")
            .WithSummary("Get the latest known position of every track")
            .WithDescription("Returns the latest stored telemetry position for each source and track.")
            .Produces<CurrentTracksResponse>(StatusCodes.Status200OK);

        return app;
    }
}
