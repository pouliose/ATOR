const numberFormat = new Intl.NumberFormat(undefined, {
  maximumFractionDigits: 4,
});

const dateFormat = new Intl.DateTimeFormat(undefined, {
  dateStyle: "medium",
  timeStyle: "medium",
});

export function renderHistoryPage(elements, page, { append, loadedCount, loadedReports }) {
  if (!append) {
    elements.historyBody.replaceChildren();
  }

  if (page.reports.length === 0 && !append) {
    elements.historyBody.append(createMessageRow("No reports have been recorded for this track ID."));
  } else {
    const rows = page.reports.map(createReportRow);
    elements.historyBody.append(...rows);
  }

  elements.historyStatus.textContent = `Showing ${loadedCount} report${loadedCount === 1 ? "" : "s"} for ${page.trackId}`;
  elements.historyPageInfo.textContent = page.hasMore
    ? "More reports are available"
    : "End of history";
  elements.reportsLoaded.textContent = String(loadedCount);
  elements.sourceCount.textContent = String(new Set(loadedReports.map((report) => report.sourceId)).size);
  elements.newestReport.textContent = loadedReports.length > 0
    ? formatDate(loadedReports[0].timestampUtc)
    : "—";
  elements.loadMoreRow.hidden = !page.hasMore;
}

export function beginHistorySearch(elements, trackId) {
  elements.historyBody.replaceChildren(createMessageRow(`Loading history for ${trackId}…`));
  elements.historyStatus.textContent = `Loading history for ${trackId}`;
  elements.historyPageInfo.textContent = "";
  elements.reportsLoaded.textContent = "0";
  elements.sourceCount.textContent = "—";
  elements.newestReport.textContent = "—";
  elements.loadMoreRow.hidden = true;
}

export function setHistoryLoading(elements, isLoading) {
  elements.searchButton.disabled = isLoading;
  elements.loadMoreButton.disabled = isLoading;
  elements.loadMoreButton.textContent = isLoading ? "Loading…" : "Load older reports";
}

export function showHistoryError(elements, message) {
  elements.historyError.textContent = message;
  elements.historyError.hidden = false;
}

export function clearHistoryError(elements) {
  elements.historyError.textContent = "";
  elements.historyError.hidden = true;
}

function createReportRow(report) {
  const row = document.createElement("tr");
  appendCell(row, formatDate(report.timestampUtc));
  appendCell(row, report.sourceId);
  appendCell(row, report.trackId, "track-name");
  appendCell(row, formatNumber(report.latitudeDegrees));
  appendCell(row, formatNumber(report.longitudeDegrees));
  appendCell(row, `${formatNumber(report.altitudeMeters)} m`);
  appendCell(row, `${formatNumber(report.groundSpeedMetersPerSecond)} m/s`);
  return row;
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
