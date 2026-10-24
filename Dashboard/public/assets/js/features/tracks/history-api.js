const TRACK_HISTORY_URL = "/api/v1/tracks";

export async function getTrackHistory(trackId, limit, cursor, { signal } = {}) {
  const query = new URLSearchParams({ limit: String(limit) });
  if (cursor) {
    query.set("beforeTimestampUtc", cursor.beforeTimestampUtc);
    query.set("beforeSourceId", cursor.beforeSourceId);
  }

  const response = await fetch(
    `${TRACK_HISTORY_URL}/${encodeURIComponent(trackId)}/history?${query}`,
    {
      headers: { Accept: "application/json" },
      signal,
    },
  );

  if (!response.ok) {
    throw new Error(`Telemetry service returned HTTP ${response.status}.`);
  }

  const page = await response.json();
  if (
    !page ||
    page.trackId !== trackId ||
    !Number.isInteger(page.reportCount) ||
    !Array.isArray(page.reports) ||
    typeof page.hasMore !== "boolean" ||
    page.reportCount !== page.reports.length ||
    (page.hasMore &&
      (!page.nextCursor ||
        typeof page.nextCursor.beforeTimestampUtc !== "string" ||
        typeof page.nextCursor.beforeSourceId !== "string"))
  ) {
    throw new Error("Telemetry service returned an invalid track-history response.");
  }

  return page;
}
