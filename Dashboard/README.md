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
```

- `app.js` wires page events, polling, and loading/error states.
- `features/tracks/current-tracks-api.js` owns HTTP access and response checks.
- `features/tracks/current-tracks-view.js` renders track data using DOM APIs.
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
