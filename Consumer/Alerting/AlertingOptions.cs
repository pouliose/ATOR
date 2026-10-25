namespace ATOR.Alerting;

public sealed class AlertingOptions
{
    public int StaleTimeoutSeconds { get; set; } = 60;

    public int CheckIntervalSeconds { get; set; } = 5;

    public List<RestrictedZoneOptions> Zones { get; set; } = [];
}

public sealed class RestrictedZoneOptions
{
    public string Id { get; set; } = string.Empty;

    public double MinimumLatitudeDegrees { get; set; }

    public double MaximumLatitudeDegrees { get; set; }

    public double MinimumLongitudeDegrees { get; set; }

    public double MaximumLongitudeDegrees { get; set; }
}

public static class AlertTypes
{
    public const string ZoneEntry = "zone_entry";

    public const string TransmissionLost = "transmission_lost";
}
