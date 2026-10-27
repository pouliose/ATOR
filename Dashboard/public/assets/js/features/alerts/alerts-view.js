const dateFormat = new Intl.DateTimeFormat(undefined, {
  dateStyle: "medium",
  timeStyle: "medium",
});

export function renderAlerts(elements, alerts, updatedAt) {
  elements.alertCount.textContent = String(alerts.length);
  elements.zoneEntryCount.textContent = String(
    alerts.filter((alert) => alert.type === "zone_entry").length,
  );
  elements.transmissionLostCount.textContent = String(
    alerts.filter((alert) => alert.type === "transmission_lost").length,
  );
  elements.refreshStatus.textContent = `Updated ${formatDate(updatedAt)} · Newest first`;
  elements.alertsBody.replaceChildren();

  if (alerts.length === 0) {
    elements.alertsBody.append(
      createMessageRow(
        "No alerts have been recorded. Zone-entry alerts require a configured zone; transmission-lost alerts appear after a track exceeds its stale timeout.",
      ),
    );
    return;
  }

  elements.alertsBody.append(...alerts.map(createAlertRow));
}

export function setRefreshing(elements, isRefreshing) {
  elements.refreshButton.disabled = isRefreshing;
  elements.alertLimit.disabled = isRefreshing;
}

export function showAlertsError(elements, message) {
  elements.alertsError.textContent = message;
  elements.alertsError.hidden = false;
}

export function clearAlertsError(elements) {
  elements.alertsError.textContent = "";
  elements.alertsError.hidden = true;
}

function createAlertRow(alert) {
  const row = document.createElement("tr");
  const typeCell = document.createElement("td");
  const badge = document.createElement("span");
  badge.className = `alert-type-badge ${getAlertTypeClass(alert.type)}`;
  badge.textContent = getAlertTypeLabel(alert.type);
  typeCell.append(badge);
  row.append(typeCell);

  appendDateCell(row, alert.occurredAtUtc);

  const trackCell = document.createElement("td");
  trackCell.className = "track-name";
  const historyLink = document.createElement("a");
  historyLink.className = "track-history-link";
  historyLink.href = `/history.html?trackId=${encodeURIComponent(alert.trackId)}`;
  historyLink.textContent = alert.trackId;
  historyLink.setAttribute("aria-label", `View telemetry history for track ${alert.trackId}`);
  trackCell.append(historyLink);
  row.append(trackCell);

  appendCell(row, alert.sourceId);
  appendDateCell(row, alert.observedTimestampUtc);
  appendCell(row, alert.details, "alert-details");
  return row;
}

function getAlertTypeLabel(type) {
  switch (type) {
    case "zone_entry":
      return "Zone entry";
    case "transmission_lost":
      return "Transmission lost";
    default:
      return type.replaceAll("_", " ");
  }
}

function getAlertTypeClass(type) {
  switch (type) {
    case "zone_entry":
      return "alert-type-zone";
    case "transmission_lost":
      return "alert-type-transmission";
    default:
      return "alert-type-unknown";
  }
}

function appendDateCell(row, value) {
  const cell = document.createElement("td");
  const date = new Date(value);
  cell.textContent = formatDate(value);
  if (!Number.isNaN(date.getTime())) {
    cell.title = date.toISOString();
  }

  row.append(cell);
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
  cell.colSpan = 6;
  cell.textContent = message;
  row.append(cell);
  return row;
}

function formatDate(value) {
  const date = new Date(value);
  return Number.isNaN(date.getTime()) ? "Unknown" : dateFormat.format(date);
}
