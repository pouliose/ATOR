# Dashboard

The Dashboard is a standalone, framework-free JavaScript frontend. Nginx serves
the static files and proxies API requests to the Consumer over the private
Docker Compose network. The browser therefore uses same-origin `/api` URLs and
does not need CORS configuration.

## Structure

```text
Dashboard/
  Dockerfile
  nginx.conf
  public/
    index.html
    assets/
      css/
        styles.css
      js/
        app.js
        shared/
          site-header.js
        features/
          tracks/
            current-tracks-api.js
            current-tracks-view.js
            history-api.js
            history-page.js
            history-view.js
          alerts/
            alerts-api.js
            alerts-page.js
            alerts-view.js
```

- `app.js` wires page events, polling, and loading/error states.
- `features` pages use the shared `ator-site-header` component for consistent
  navigation and active-page indication.
- `features/tracks/current-tracks-api.js` owns HTTP access and response checks.
- `features/tracks/current-tracks-view.js` renders track data using DOM APIs.
- `history.html` provides track history search and cursor-based pagination;
  select a track ID from the current positions table to open its history.
- `features/tracks/history-api.js` requests and validates history pages.
- `features/tracks/history-page.js` manages history search and load-more events.
- `features/tracks/history-view.js` renders paginated history using DOM APIs.
- `alerts.html` and `features/alerts/` display recent persisted alerts from
  `GET /api/v1/alerts`, including alert type, track/source, details, and both
  timestamps. The page refreshes every five seconds and lets users choose a
  page size between 20 and 500.
- `nginx.conf` serves the frontend and proxies `/api/` and `/openapi/` to the
  Consumer.

Keep feature-specific behavior under `features/<feature>/` as the dashboard
grows. Shared, genuinely cross-feature helpers can be added under a `shared/`
directory rather than coupling unrelated features.

## Run with Docker Compose

From the `ATOR` directory:

```powershell
docker compose up --build
```

Open `http://localhost:5173`. The dashboard refreshes current track positions
every five seconds. The host port can be changed with
`DASHBOARD_HTTP_HOST_PORT`.

Open `http://localhost:5173/history.html` to search by track ID, or select a
track ID in the current positions table. History displays newest reports first;
use **Load older reports** to request the next page.

Open `http://localhost:5173/alerts.html` to review recent alerts. The page
explains that `Occurred` is when the alert condition began (for transmission
loss, last report time plus the configured timeout), while `Observed report` is
the telemetry timestamp used to evaluate the alert. Zone-entry alerts require a
configured zone; the application starts with none.
