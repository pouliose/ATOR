import { getCurrentTracks } from "./features/tracks/current-tracks-api.js";
import {
  clearError,
  renderCurrentTracks,
  setConnectionStatus,
  showError,
} from "./features/tracks/current-tracks-view.js";

const REFRESH_INTERVAL_MS = 5_000;
const elements = {
  errorMessage: document.querySelector("#error-message"),
  refreshButton: document.querySelector("#refresh-button"),
  statusIndicator: document.querySelector("#status-indicator"),
  statusText: document.querySelector("#status-text"),
  trackCount: document.querySelector("#track-count"),
  tracksBody: document.querySelector("#tracks-body"),
  updatedAt: document.querySelector("#updated-at"),
};

let refreshTimer;
let requestInProgress = false;
let hasLoadedData = false;

async function refreshCurrentTracks() {
  if (requestInProgress) {
    return;
  }

  requestInProgress = true;
  elements.refreshButton.disabled = true;
  clearError(elements);
  if (!hasLoadedData) {
    setConnectionStatus(elements, "Loading current positions", "loading");
  }

  try {
    const report = await getCurrentTracks();
    renderCurrentTracks(elements, report);
    hasLoadedData = true;
    setConnectionStatus(elements, "Connected", "connected");
  } catch (error) {
    setConnectionStatus(elements, "Connection issue", "error");
    showError(
      elements,
      error instanceof Error ? error.message : "Unable to load current positions.",
    );
  } finally {
    requestInProgress = false;
    elements.refreshButton.disabled = false;
    window.clearTimeout(refreshTimer);
    refreshTimer = window.setTimeout(refreshCurrentTracks, REFRESH_INTERVAL_MS);
  }
}

elements.refreshButton.addEventListener("click", refreshCurrentTracks);
refreshCurrentTracks();
