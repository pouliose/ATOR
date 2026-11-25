const { expect, test } = require("@playwright/test");

const currentTracksUrl = "**/api/v1/tracks/current";
const tileUrl = "https://tile.openstreetmap.org/**";
const tileImage = Buffer.from(
  '<svg xmlns="http://www.w3.org/2000/svg" width="256" height="256"><rect width="256" height="256" fill="#e8e5dc"/></svg>',
);
const tracks = [
  createTrack("DRONE-001", 37.9838, 23.7275),
  createTrack("DRONE-002", 37.9938, 23.7425),
  createTrack("DRONE-003", 38.0038, 23.7575),
];

test("shows current tracks on a tiled map with OpenStreetMap attribution", async ({ page }) => {
  const requestedTiles = await mockMapTiles(page);
  await mockCurrentTracks(page, tracks);
  const response = await page.goto("/");

  expect(response.headers()["content-security-policy"])
    .toContain("https://tile.openstreetmap.org");
  await expect(page.getByRole("heading", { name: "Live map · Athens" })).toBeVisible();
  await expect(page.locator("#status-text")).toHaveText("Connected");
  await expect(page.locator("#track-count")).toHaveText("3");
  await expect(page.locator("#map-tracks .map-drone")).toHaveCount(3);
  await expect(page.locator("#map-tracks .map-marker-label")).toHaveText([
    "DRONE-001",
    "DRONE-002",
    "DRONE-003",
  ]);
  await expect(page.locator("#map-tracks a").first()).toHaveAttribute(
    "href",
    "/history.html?trackId=DRONE-001",
  );
  await expect(page.getByRole("link", { name: "OpenStreetMap contributors" }))
    .toHaveAttribute("href", "https://www.openstreetmap.org/copyright");
  await expect.poll(() => requestedTiles.length).toBeGreaterThan(0);
  expect(requestedTiles.every((url) => new URL(url).hostname === "tile.openstreetmap.org"))
    .toBe(true);
  expect(requestedTiles.every((url) => new URL(url).pathname.startsWith("/13/")))
    .toBe(true);
  await expect(page.locator("#map-tiles image").first()).toHaveAttribute(
    "href",
    /https:\/\/tile\.openstreetmap\.org\/13\/\d+\/\d+\.png/,
  );
});

test("zoom controls reload map tiles and keep track markers in sync", async ({ page }) => {
  const requestedTiles = await mockMapTiles(page);
  await mockCurrentTracks(page, tracks);
  await page.goto("/");

  await expect(page.locator("#map-tracks .map-drone")).toHaveCount(3);
  await expect(page.locator("#map-scale text")).toHaveText("1 km");
  await page.getByRole("button", { name: "Zoom in" }).click();

  await expect(page.locator("#map-scale text")).toHaveText("500 m");
  await expect.poll(() => requestedTiles.some((url) =>
    new URL(url).pathname.startsWith("/14/"),
  )).toBe(true);
  await expect.poll(async () => {
    const markerCount = await page.locator("#map-tracks .map-drone").count();
    const mapStatus = await page.locator("#map-status").textContent();
    return mapStatus.includes(`${markerCount} track`) &&
      mapStatus.includes("in Athens area");
  }).toBe(true);

  await page.getByRole("button", { name: "Zoom out" }).click();
  await expect(page.locator("#map-scale text")).toHaveText("1 km");
  await expect(page.locator("#map-tracks .map-drone")).toHaveCount(3);
});

test("shows a clear empty state when no tracks have been reported", async ({ page }) => {
  await mockMapTiles(page);
  await mockCurrentTracks(page, []);
  await page.goto("/");

  await expect(page.locator("#track-count")).toHaveText("0");
  await expect(page.locator("#map-tracks .map-drone")).toHaveCount(0);
  await expect(page.locator("#map-tracks")).toContainText(
    "No track positions are available",
  );
  await expect(page.locator("#map-status")).toContainText("0 tracks in Athens area");
  await expect(page.locator("#tracks-body")).toContainText(
    "No track positions have been reported yet.",
  );
});

test("reports tracks outside the current map view", async ({ page }) => {
  await mockMapTiles(page);
  await mockCurrentTracks(page, [createTrack("DRONE-FAR", 40, 25)]);
  await page.goto("/");

  await expect(page.locator("#map-tracks .map-drone")).toHaveCount(0);
  await expect(page.locator("#map-tracks")).toContainText(
    "Tracks are outside this map area",
  );
  await expect(page.locator("#map-status")).toHaveText(
    "0 tracks in Athens area · 1 outside this view",
  );
});

test("shows an API error without hiding the map", async ({ page }) => {
  await mockMapTiles(page);
  await page.route(currentTracksUrl, (route) =>
    route.fulfill({ status: 503, body: "Service unavailable" }),
  );
  await page.goto("/");

  await expect(page.locator("#status-text")).toHaveText("Connection issue");
  await expect(page.locator("#error-message")).toContainText(
    "Telemetry service returned HTTP 503.",
  );
  await expect(page.locator("#live-map")).toBeVisible();
  await expect(page.locator("#map-tracks .map-drone")).toHaveCount(0);
});

test("reports when OpenStreetMap tiles cannot load", async ({ page }) => {
  await page.route(tileUrl, (route) => route.abort());
  await mockCurrentTracks(page, tracks);
  await page.goto("/");

  await expect(page.locator("#map-tracks .map-drone")).toHaveCount(3);
  await expect(page.locator("#map-status")).toContainText(
    "map tiles unavailable; check your internet connection",
  );
});

async function mockMapTiles(page) {
  const requestedTiles = [];
  await page.route(tileUrl, async (route) => {
    requestedTiles.push(route.request().url());
    await route.fulfill({
      status: 200,
      contentType: "image/svg+xml",
      body: tileImage,
    });
  });
  return requestedTiles;
}

async function mockCurrentTracks(page, currentTracks) {
  await page.route(currentTracksUrl, (route) =>
    route.fulfill({
      status: 200,
      contentType: "application/json",
      body: JSON.stringify({
        generatedAtUtc: "2026-11-03T20:50:00Z",
        trackCount: currentTracks.length,
        tracks: currentTracks,
      }),
    }),
  );
}

function createTrack(trackId, latitudeDegrees, longitudeDegrees) {
  return {
    sourceId: "COLLECTOR-01",
    trackId,
    timestampUtc: "2026-11-03T20:50:00Z",
    latitudeDegrees,
    longitudeDegrees,
    altitudeMeters: 1000,
    groundSpeedMetersPerSecond: 60,
  };
}
