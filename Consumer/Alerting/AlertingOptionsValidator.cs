namespace ATOR.Alerting;

public static class AlertingOptionsValidator
{
    public static string? GetValidationError(AlertingOptions options)
    {
        if (options.StaleTimeoutSeconds <= 0)
        {
            return "Alerting:StaleTimeoutSeconds must be greater than zero.";
        }

        if (options.CheckIntervalSeconds <= 0)
        {
            return "Alerting:CheckIntervalSeconds must be greater than zero.";
        }

        if (options.Zones is null)
        {
            return "Alerting:Zones must be a list of configured zones.";
        }

        var zoneIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var zone in options.Zones)
        {
            if (zone is null || string.IsNullOrWhiteSpace(zone.Id))
            {
                return "Each configured alert zone must have a non-empty Id.";
            }

            if (!zoneIds.Add(zone.Id))
            {
                return $"Alerting zone Id '{zone.Id}' must be unique.";
            }

            if (!double.IsFinite(zone.MinimumLatitudeDegrees) ||
                !double.IsFinite(zone.MaximumLatitudeDegrees) ||
                zone.MinimumLatitudeDegrees is < -90 or > 90 ||
                zone.MaximumLatitudeDegrees is < -90 or > 90 ||
                zone.MinimumLatitudeDegrees > zone.MaximumLatitudeDegrees)
            {
                return $"Alerting zone '{zone.Id}' must have valid minimum and maximum latitudes.";
            }

            if (!double.IsFinite(zone.MinimumLongitudeDegrees) ||
                !double.IsFinite(zone.MaximumLongitudeDegrees) ||
                zone.MinimumLongitudeDegrees is < -180 or > 180 ||
                zone.MaximumLongitudeDegrees is < -180 or > 180 ||
                zone.MinimumLongitudeDegrees > zone.MaximumLongitudeDegrees)
            {
                return $"Alerting zone '{zone.Id}' must have valid minimum and maximum longitudes.";
            }
        }

        return null;
    }
}
