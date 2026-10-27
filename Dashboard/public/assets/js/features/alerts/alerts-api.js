const ALERTS_URL = "/api/v1/alerts";

export async function getRecentAlerts(limit, { signal } = {}) {
  const query = new URLSearchParams({ limit: String(limit) });
  const response = await fetch(`${ALERTS_URL}?${query}`, {
    headers: { Accept: "application/json" },
    signal,
  });

  if (!response.ok) {
    throw new Error(`Telemetry service returned HTTP ${response.status}.`);
  }

  const alerts = await response.json();
  if (!Array.isArray(alerts) || !alerts.every(isTrackAlert)) {
    throw new Error("Telemetry service returned an invalid alert-history response.");
  }

  return alerts;
}

function isTrackAlert(alert) {
  return (
    alert !== null &&
    typeof alert === "object" &&
    Number.isInteger(alert.id) &&
    typeof alert.type === "string" &&
    typeof alert.sourceId === "string" &&
    typeof alert.trackId === "string" &&
    isTimestamp(alert.occurredAtUtc) &&
    isTimestamp(alert.observedTimestampUtc) &&
    (alert.zoneId === null || typeof alert.zoneId === "string") &&
    typeof alert.details === "string"
  );
}

function isTimestamp(value) {
  return typeof value === "string" && !Number.isNaN(Date.parse(value));
}
