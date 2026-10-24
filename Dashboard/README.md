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
        features/
          tracks/
            current-tracks-api.js
            current-tracks-view.js
            history-api.js
            history-page.js
            history-view.js
```

- `app.js` wires page events, polling, and loading/error states.
- `features/tracks/current-tracks-api.js` owns HTTP access and response checks.
- `features/tracks/current-tracks-view.js` renders track data using DOM APIs.
- `history.html` provides track history search and cursor-based pagination;
  select a track ID from the current positions table to open its history.
- `features/tracks/history-api.js` requests and validates history pages.
- `features/tracks/history-page.js` manages history search and load-more events.
- `features/tracks/history-view.js` renders paginated history using DOM APIs.
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
