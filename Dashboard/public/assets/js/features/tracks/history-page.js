import { getTrackHistory } from "./history-api.js";
import {
  beginHistorySearch,
  clearHistoryError,
  renderHistoryPage,
  setHistoryLoading,
  showHistoryError,
} from "./history-view.js";

const elements = {
  historyBody: document.querySelector("#history-body"),
  historyError: document.querySelector("#history-error"),
  historyForm: document.querySelector("#history-form"),
  historyPageInfo: document.querySelector("#history-page-info"),
  historyStatus: document.querySelector("#history-status"),
  loadMoreButton: document.querySelector("#load-more-button"),
  loadMoreRow: document.querySelector("#load-more-row"),
  pageLimit: document.querySelector("#page-limit"),
  reportsLoaded: document.querySelector("#reports-loaded"),
  searchButton: document.querySelector("#search-button"),
  sourceCount: document.querySelector("#source-count"),
  trackId: document.querySelector("#track-id"),
  newestReport: document.querySelector("#newest-report"),
};

let cursor;
let currentTrackId = "";
let loadedCount = 0;
let requestInProgress = false;
let loadedReports = [];

const initialTrackId = new URLSearchParams(window.location.search).get("trackId");
if (initialTrackId) {
  elements.trackId.value = initialTrackId;
  loadHistory(false);
}

elements.historyForm.addEventListener("submit", (event) => {
  event.preventDefault();
  loadHistory(false);
});

elements.loadMoreButton.addEventListener("click", () => loadHistory(true));

async function loadHistory(append) {
  if (requestInProgress) {
    return;
  }

  if (!append) {
    currentTrackId = elements.trackId.value.trim();
    cursor = undefined;
    loadedCount = 0;
    loadedReports = [];
  }

  if (!currentTrackId) {
    showHistoryError(elements, "Enter a track ID to load its history.");
    return;
  }

  requestInProgress = true;
  clearHistoryError(elements);
  if (!append) {
    beginHistorySearch(elements, currentTrackId);
  }

  setHistoryLoading(elements, true);

  try {
    const page = await getTrackHistory(
      currentTrackId,
      Number(elements.pageLimit.value),
      append ? cursor : undefined,
    );
    loadedCount += page.reportCount;
    loadedReports = loadedReports.concat(page.reports);
    cursor = page.nextCursor;
    renderHistoryPage(elements, page, { append, loadedCount, loadedReports });
  } catch (error) {
    if (!append) {
      elements.historyStatus.textContent = "History unavailable";
    }

    showHistoryError(
      elements,
      error instanceof Error ? error.message : "Unable to load track history.",
    );
  } finally {
    requestInProgress = false;
    setHistoryLoading(elements, false);
  }
}
