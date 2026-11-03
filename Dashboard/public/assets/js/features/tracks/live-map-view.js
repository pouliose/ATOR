const SVG_NAMESPACE = "http://www.w3.org/2000/svg";
const TILE_SIZE = 256;
const TILE_URL = "https://tile.openstreetmap.org";
const MAP_BOUNDS = {
  left: 0,
  right: 1000,
  top: 0,
  bottom: 500,
  centerLatitude: 37.9838,
  centerLongitude: 23.7275,
};
const MIN_ZOOM = 11;
const MAX_ZOOM = 17;
const TRACK_COLORS = ["mint", "blue", "amber", "violet", "coral", "cyan"];
const mapStates = new WeakMap();
const coordinateFormat = new Intl.NumberFormat(undefined, {
  maximumFractionDigits: 4,
});
const detailFormat = new Intl.NumberFormat(undefined, {
  maximumFractionDigits: 2,
});
const dateFormat = new Intl.DateTimeFormat(undefined, {
  dateStyle: "medium",
  timeStyle: "medium",
});

export function initializeLiveMap(map, mapStatus) {
  if (mapStates.has(map)) {
    return;
  }

  const state = {
    map,
    mapStatus,
    tracks: [],
    zoom: 13,
    tileStatus: "loading",
    pendingTiles: 0,
    failedTiles: false,
    tileGeneration: 0,
  };
  mapStates.set(map, state);

  const mapFrame = map.closest(".map-frame");
  mapFrame.querySelector("#map-zoom-in").addEventListener("click", () => {
    changeZoom(state, 1);
  });
  mapFrame.querySelector("#map-zoom-out").addEventListener("click", () => {
    changeZoom(state, -1);
  });
  drawTiles(state);
  updateMapStatus(state);
}

export function renderLiveMap(map, mapStatus, tracks) {
  initializeLiveMap(map, mapStatus);
  const state = mapStates.get(map);
  state.tracks = tracks.filter(isMappableTrack);
  drawMarkers(state);
  updateMapStatus(state);
}

function changeZoom(state, amount) {
  const zoom = Math.max(MIN_ZOOM, Math.min(MAX_ZOOM, state.zoom + amount));
  if (zoom === state.zoom) {
    return;
  }

  state.zoom = zoom;
  drawTiles(state);
  drawMarkers(state);
  updateMapStatus(state);
}

function drawTiles(state) {
  const tiles = state.map.querySelector("#map-tiles");
  tiles.replaceChildren();

  const center = projectCoordinate(
    MAP_BOUNDS.centerLatitude,
    MAP_BOUNDS.centerLongitude,
    state.zoom,
  );
  const viewWidth = MAP_BOUNDS.right - MAP_BOUNDS.left;
  const viewHeight = MAP_BOUNDS.bottom - MAP_BOUNDS.top;
  const worldLeft = center.x - viewWidth / 2;
  const worldTop = center.y - viewHeight / 2;
  const worldRight = center.x + viewWidth / 2;
  const worldBottom = center.y + viewHeight / 2;
  const firstTileX = Math.floor(worldLeft / TILE_SIZE);
  const lastTileX = Math.floor((worldRight - Number.EPSILON) / TILE_SIZE);
  const firstTileY = Math.max(0, Math.floor(worldTop / TILE_SIZE));
  const lastTileY = Math.min(
    2 ** state.zoom - 1,
    Math.floor((worldBottom - Number.EPSILON) / TILE_SIZE),
  );
  const tileCount = 2 ** state.zoom;
  const tileGeneration = ++state.tileGeneration;
  const tileCoordinates = [];

  for (let tileY = firstTileY; tileY <= lastTileY; tileY += 1) {
    for (let tileX = firstTileX; tileX <= lastTileX; tileX += 1) {
      tileCoordinates.push({
        x: ((tileX % tileCount) + tileCount) % tileCount,
        y: tileY,
        screenX: (MAP_BOUNDS.left + center.x - worldLeft) -
          center.x + tileX * TILE_SIZE,
        screenY: (MAP_BOUNDS.top + center.y - worldTop) -
          center.y + tileY * TILE_SIZE,
      });
    }
  }

  state.pendingTiles = tileCoordinates.length;
  state.failedTiles = false;
  state.tileStatus = tileCoordinates.length === 0 ? "error" : "loading";
  renderScale(state);

  for (const tile of tileCoordinates) {
    const image = createSvgElement("image", {
      x: tile.screenX,
      y: tile.screenY,
      width: TILE_SIZE,
      height: TILE_SIZE,
      href: `${TILE_URL}/${state.zoom}/${tile.x}/${tile.y}.png`,
      class: "map-tile",
      preserveAspectRatio: "none",
    });
    image.addEventListener(
      "load",
      () => finishTileRequest(state, tileGeneration, false),
      { once: true },
    );
    image.addEventListener(
      "error",
      () => finishTileRequest(state, tileGeneration, true),
      { once: true },
    );
    tiles.append(image);
  }
}

function finishTileRequest(state, tileGeneration, failed) {
  if (tileGeneration !== state.tileGeneration) {
    return;
  }

  if (failed) {
    state.failedTiles = true;
  }

  state.pendingTiles -= 1;
  if (state.pendingTiles === 0) {
    state.tileStatus = state.failedTiles ? "error" : "ready";
    updateMapStatus(state);
  }
}

function drawMarkers(state) {
  const markers = state.map.querySelector("#map-tracks");
  markers.replaceChildren();
  const visibleTracks = state.tracks.filter((track) => isInsideMap(track, state.zoom));

  for (const track of visibleTracks) {
    const index = state.tracks.indexOf(track);
    markers.append(createMarker(track, index, state.zoom));
  }

  if (visibleTracks.length === 0) {
    const emptyLabel = createSvgElement("text", {
      x: (MAP_BOUNDS.left + MAP_BOUNDS.right) / 2,
      y: (MAP_BOUNDS.top + MAP_BOUNDS.bottom) / 2,
      class: "map-empty-label",
      "text-anchor": "middle",
    });
    emptyLabel.textContent = state.tracks.length === 0
      ? "No track positions are available"
      : "Tracks are outside this map area";
    markers.append(emptyLabel);
  }
}

function updateMapStatus(state) {
  const visibleCount = state.tracks.filter((track) => isInsideMap(track, state.zoom)).length;
  const shownTracks = `${visibleCount} track${visibleCount === 1 ? "" : "s"} in Athens area`;
  const outsideCount = state.tracks.length - visibleCount;
  const outsideMessage = outsideCount > 0
    ? ` · ${outsideCount} outside this view`
    : "";
  const tileMessage = state.tileStatus === "loading"
    ? " · loading map tiles"
    : state.tileStatus === "error"
      ? " · map tiles unavailable; check your internet connection"
      : "";
  state.mapStatus.textContent = `${shownTracks}${outsideMessage}${tileMessage}`;
  state.map.closest(".map-frame").querySelector("#map-zoom-in").disabled =
    state.zoom >= MAX_ZOOM;
  state.map.closest(".map-frame").querySelector("#map-zoom-out").disabled =
    state.zoom <= MIN_ZOOM;
}

function renderScale(state) {
  const scale = state.map.querySelector("#map-scale");
  scale.replaceChildren();
  const metersPerPixel = Math.cos(MAP_BOUNDS.centerLatitude * Math.PI / 180) *
    (2 * Math.PI * 6_378_137) / (TILE_SIZE * 2 ** state.zoom);
  const targetMeters = metersPerPixel * 100;
  const magnitude = 10 ** Math.floor(Math.log10(targetMeters));
  const leadingDigit = targetMeters / magnitude;
  const roundedDigit = leadingDigit >= 5 ? 5 : leadingDigit >= 2 ? 2 : 1;
  const distanceMeters = roundedDigit * magnitude;
  const width = distanceMeters / metersPerPixel;
  const x = MAP_BOUNDS.right - 125;
  const y = MAP_BOUNDS.bottom - 24;
  scale.append(
    createSvgElement("path", {
      d: `M${x} ${y - 8}V${y}H${x + width}V${y - 8}`,
      class: "map-scale-line",
    }),
    createSvgElement("text", {
      x: x + width / 2,
      y: y - 12,
      class: "map-scale-label",
      "text-anchor": "middle",
    }),
  );
  scale.querySelector("text").textContent = distanceMeters >= 1000
    ? `${detailFormat.format(distanceMeters / 1000)} km`
    : `${Math.round(distanceMeters)} m`;
}

function isMappableTrack(track) {
  return Number.isFinite(track.latitudeDegrees) &&
    track.latitudeDegrees >= -90 &&
    track.latitudeDegrees <= 90 &&
    Number.isFinite(track.longitudeDegrees) &&
    track.longitudeDegrees >= -180 &&
    track.longitudeDegrees <= 180;
}

function isInsideMap(track, zoom) {
  const position = projectCoordinate(track.latitudeDegrees, track.longitudeDegrees, zoom);
  const center = projectCoordinate(
    MAP_BOUNDS.centerLatitude,
    MAP_BOUNDS.centerLongitude,
    zoom,
  );
  const x = (MAP_BOUNDS.left + MAP_BOUNDS.right) / 2 + position.x - center.x;
  const y = (MAP_BOUNDS.top + MAP_BOUNDS.bottom) / 2 + position.y - center.y;
  return x >= MAP_BOUNDS.left &&
    x <= MAP_BOUNDS.right &&
    y >= MAP_BOUNDS.top &&
    y <= MAP_BOUNDS.bottom;
}

function createMarker(track, index, zoom) {
  const center = projectCoordinate(
    MAP_BOUNDS.centerLatitude,
    MAP_BOUNDS.centerLongitude,
    zoom,
  );
  const position = projectCoordinate(track.latitudeDegrees, track.longitudeDegrees, zoom);
  const x = (MAP_BOUNDS.left + MAP_BOUNDS.right) / 2 + position.x - center.x;
  const y = (MAP_BOUNDS.top + MAP_BOUNDS.bottom) / 2 + position.y - center.y;
  const link = createSvgElement("a", {
    href: `/history.html?trackId=${encodeURIComponent(track.trackId)}`,
    class: "map-marker-link",
    "aria-label": `View history for ${track.trackId} at ${formatCoordinate(track.latitudeDegrees, "latitude")}, ${formatCoordinate(track.longitudeDegrees, "longitude")}`,
  });
  const title = createSvgElement("title");
  title.textContent = `${track.trackId} · ${track.sourceId} · ` +
    `${detailFormat.format(track.altitudeMeters)} m · ` +
    `${detailFormat.format(track.groundSpeedMetersPerSecond)} m/s · ` +
    `${formatDate(track.timestampUtc)}`;
  const marker = createSvgElement("g", {
    transform: `translate(${x} ${y})`,
    class: `map-drone map-drone-${TRACK_COLORS[index % TRACK_COLORS.length]}`,
  });
  marker.append(createSvgElement("circle", {
    r: 17,
    class: "map-drone-halo",
  }));
  for (const [rotorX, rotorY] of [[-11, -11], [11, -11], [-11, 11], [11, 11]]) {
    marker.append(
      createSvgElement("line", {
        x1: 0,
        y1: 0,
        x2: rotorX,
        y2: rotorY,
        class: "map-drone-arm",
      }),
      createSvgElement("circle", {
        cx: rotorX,
        cy: rotorY,
        r: 3.5,
        class: "map-drone-rotor",
      }),
    );
  }
  marker.append(createSvgElement("polygon", {
    points: "0,-5 5,0 0,5 -5,0",
    class: "map-drone-body",
  }));
  const labelPosition = [
    { x: x - 14, y: y + 34, anchor: "end" },
    { x: x + 18, y: y + 32, anchor: "start" },
    { x: x + 18, y: y - 24, anchor: "start" },
    { x: x - 14, y: y - 24, anchor: "end" },
  ][index % 4];
  const label = createSvgElement("text", {
    x: labelPosition.x,
    y: labelPosition.y,
    class: "map-marker-label",
    "text-anchor": labelPosition.anchor,
  });
  label.textContent = track.trackId;
  link.append(title, marker, label);
  return link;
}

function projectCoordinate(latitude, longitude, zoom) {
  const worldSize = TILE_SIZE * 2 ** zoom;
  const clampedLatitude = Math.max(-85.05112878, Math.min(85.05112878, latitude));
  const sinLatitude = Math.sin(clampedLatitude * Math.PI / 180);
  return {
    x: (longitude + 180) / 360 * worldSize,
    y: (0.5 - Math.log((1 + sinLatitude) / (1 - sinLatitude)) / (4 * Math.PI)) *
      worldSize,
  };
}

function createSvgElement(name, attributes = {}) {
  const element = document.createElementNS(SVG_NAMESPACE, name);
  for (const [attribute, value] of Object.entries(attributes)) {
    element.setAttribute(attribute, String(value));
  }
  return element;
}

function formatCoordinate(value, axis) {
  const direction = axis === "latitude"
    ? value < 0 ? "S" : "N"
    : value < 0 ? "W" : "E";
  return `${coordinateFormat.format(Math.abs(value))}° ${direction}`;
}

function formatDate(value) {
  const date = new Date(value);
  return Number.isNaN(date.getTime()) ? "Unknown" : dateFormat.format(date);
}
