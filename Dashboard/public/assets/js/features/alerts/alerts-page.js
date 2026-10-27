import { getRecentAlerts } from "./alerts-api.js";
import {
  clearAlertsError,
  renderAlerts,
  setRefreshing,
  showAlertsError,
} from "./alerts-view.js";

const REFRESH_INTERVAL_MS = 5_000;
const elements = {
  alertCount: document.querySelector("#alert-count"),
  alertLimit: document.querySelector("#alert-limit"),
  alertsBody: document.querySelector("#alerts-body"),
  alertsError: document.querySelector("#alerts-error"),
  refreshButton: document.querySelector("#refresh-button"),
  refreshStatus: document.querySelector("#refresh-status"),
  transmissionLostCount: document.querySelector("#transmission-lost-count"),
  zoneEntryCount: document.querySelector("#zone-entry-count"),
};

let refreshTimer;
let requestInProgress = false;
let hasLoadedAlerts = false;

async function refreshAlerts() {
  if (requestInProgress) {
    return;
  }

  requestInProgress = true;
  setRefreshing(elements, true);
  clearAlertsError(elements);

  try {
    const alerts = await getRecentAlerts(Number(elements.alertLimit.value));
    renderAlerts(elements, alerts, new Date());
    hasLoadedAlerts = true;
  } catch (error) {
    showAlertsError(
      elements,
      error instanceof Error ? error.message : "Unable to load alert history.",
    );
    if (!elements.refreshStatus.textContent.startsWith("Updated")) {
      elements.refreshStatus.textContent = "Alert history unavailable";
    }

    if (!hasLoadedAlerts) {
      const row = document.createElement("tr");
      const cell = document.createElement("td");
      cell.className = "table-placeholder";
      cell.colSpan = 6;
      cell.textContent = "Alert history is currently unavailable.";
      row.append(cell);
      elements.alertsBody.replaceChildren(row);
    }
  } finally {
    requestInProgress = false;
    setRefreshing(elements, false);
    window.clearTimeout(refreshTimer);
    refreshTimer = window.setTimeout(refreshAlerts, REFRESH_INTERVAL_MS);
  }
}

elements.refreshButton.addEventListener("click", refreshAlerts);
elements.alertLimit.addEventListener("change", refreshAlerts);
refreshAlerts();
