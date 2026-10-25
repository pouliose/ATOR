using ATOR.Services;
using Microsoft.AspNetCore.Http.HttpResults;

namespace ATOR.Api;

public static class AlertsEndpoints
{
    public static WebApplication MapAlertEndpoints(this WebApplication app)
    {
        app.MapGet(
                "/api/v1/alerts",
                async Task<Results<Ok<IReadOnlyList<TrackAlertResponse>>, ProblemHttpResult>> (
                    int? limit,
                    IAlertsService alertsService,
                    CancellationToken cancellationToken) =>
                {
                    var pageSize = limit ?? 100;
                    if (pageSize is < 1 or > 500)
                    {
                        return TypedResults.Problem(
                            statusCode: StatusCodes.Status400BadRequest,
                            title: "Invalid alert page size",
                            detail: "The limit must be between 1 and 500.");
                    }

                    return TypedResults.Ok(
                        await alertsService.GetAlertsAsync(pageSize, cancellationToken));
                })
            .WithName("GetAlerts")
            .WithTags("Alerts")
            .WithSummary("Get recent track alerts")
            .WithDescription("Returns the newest persisted alerts first, with a maximum of 500 per request.")
            .Produces<IReadOnlyList<TrackAlertResponse>>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest);

        return app;
    }
}
