const numberFormat = new Intl.NumberFormat(undefined, {
  maximumFractionDigits: 4,
});

const dateFormat = new Intl.DateTimeFormat(undefined, {
  dateStyle: "medium",
  timeStyle: "medium",
});

export function renderCurrentTracks(elements, report) {
  elements.trackCount.textContent = String(report.trackCount);
  elements.updatedAt.textContent = `Last response: ${formatDate(report.generatedAtUtc)}`;
  elements.tracksBody.replaceChildren();

  if (report.tracks.length === 0) {
    elements.tracksBody.append(createMessageRow("No track positions have been reported yet."));
    return;
  }

  const rows = report.tracks.map((track) => {
    const row = document.createElement("tr");
    appendCell(row, track.trackId, "track-name");
    appendCell(row, track.sourceId);
    appendCell(row, formatNumber(track.latitudeDegrees));
    appendCell(row, formatNumber(track.longitudeDegrees));
    appendCell(row, `${formatNumber(track.altitudeMeters)} m`);
    appendCell(row, `${formatNumber(track.groundSpeedMetersPerSecond)} m/s`);
    appendCell(row, formatDate(track.timestampUtc));
    return row;
  });

  elements.tracksBody.append(...rows);
}

export function setConnectionStatus(elements, message, state) {
  elements.statusText.textContent = message;
  elements.statusIndicator.dataset.state = state;
}

export function showError(elements, message) {
  elements.errorMessage.textContent = message;
  elements.errorMessage.hidden = false;
}

export function clearError(elements) {
  elements.errorMessage.textContent = "";
  elements.errorMessage.hidden = true;
}

function appendCell(row, value, className) {
  const cell = document.createElement("td");
  cell.textContent = value;
  if (className) {
    cell.className = className;
  }

  row.append(cell);
}

function createMessageRow(message) {
  const row = document.createElement("tr");
  const cell = document.createElement("td");
  cell.className = "table-placeholder";
  cell.colSpan = 7;
  cell.textContent = message;
  row.append(cell);
  return row;
}

function formatNumber(value) {
  return Number.isFinite(value) ? numberFormat.format(value) : "—";
}

function formatDate(value) {
  const date = new Date(value);
  return Number.isNaN(date.getTime()) ? "Unknown" : dateFormat.format(date);
}
