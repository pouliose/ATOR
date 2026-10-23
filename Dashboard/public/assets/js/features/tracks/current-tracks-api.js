const CURRENT_TRACKS_URL = "/api/v1/tracks/current";

export async function getCurrentTracks({ signal } = {}) {
  const response = await fetch(CURRENT_TRACKS_URL, {
    headers: { Accept: "application/json" },
    signal,
  });

  if (!response.ok) {
    throw new Error(`Telemetry service returned HTTP ${response.status}.`);
  }

  const report = await response.json();
  if (!report || !Array.isArray(report.tracks) || !Number.isInteger(report.trackCount)) {
    throw new Error("Telemetry service returned an invalid current-tracks response.");
  }

  return report;
}
