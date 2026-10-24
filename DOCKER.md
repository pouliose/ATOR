# Docker and Docker Compose

The Compose setup builds and runs the producer and consumer as separate .NET 9 containers and stores telemetry in PostgreSQL. It is intended for local, simulation-only use.

## Prerequisites

- Docker Engine with the Docker Compose plugin on Linux, or Docker Desktop using Linux containers on Windows.
- Run the commands below from the `ATOR` project directory, where `compose.yaml` is located.

## Start and stop

Build the images and start both apps:

```console
docker compose up --build
```

Compose waits for PostgreSQL's health check before starting the consumer, then starts the producer. The producer sends simulated reports repeatedly, so if an initial UDP packet is lost during startup, a later update will follow. UDP itself does not guarantee delivery. PostgreSQL data is kept in the named `postgres_data` volume across container restarts and `docker compose down`.

Stop the apps with `Ctrl+C`, or from another terminal run:

```console
docker compose down
```

To start the services in the background, add `-d` to `docker compose up --build`.

## Configuration

Compose passes settings to .NET using the double-underscore environment variable format:

| Environment variable | Default | Purpose |
|---|---:|---|
| `TELEMETRY_UDP_PORT` | `5000` | UDP port used by the consumer and producer. |
| `TELEMETRY_INTERVAL_SECONDS` | `1` | Number of seconds between producer batches. |
| `TELEMETRY_TRACK_COUNT` | `3` | Simulated tracks in each producer batch. |
| `TELEMETRY_SOURCE_ID` | `COLLECTOR-01` | Identifier attached to producer reports. |
| `CONSUMER_HTTP_HOST_PORT` | `5100` | Host port for the Consumer HTTP API. Bound to `127.0.0.1` only. |
| `DASHBOARD_HTTP_HOST_PORT` | `5173` | Host port for the Dashboard frontend. Bound to `127.0.0.1` only. |
| `POSTGRES_HOST_PORT` | `5433` | Host port for local PostgreSQL clients and EF CLI tools. Bound to `127.0.0.1` only. |
| `POSTGRES_PASSWORD` | `ator_dev_password` | Local PostgreSQL password used by the database and consumer. Change this for local environments where the default is unsuitable. |

When running the producer directly, configure its defaults in `Producer/appsettings.json`. Environment variables override that file; Compose sets the container's receiver host to `consumer` and the Compose variables above override the other settings. Set variables before running Compose to override defaults. For example, in PowerShell:

```powershell
$env:TELEMETRY_UDP_PORT = "5100"
$env:TELEMETRY_SOURCE_ID = "COLLECTOR-02"
docker compose up --build
```

In a Linux shell:

```sh
TELEMETRY_UDP_PORT=5100 TELEMETRY_SOURCE_ID=COLLECTOR-02 docker compose up --build
```

## Networking

Compose places its services on the default private bridge network. The dashboard is served at `http://localhost:5173` by default and proxies `/api/` and `/openapi/` requests to the consumer over the Compose network. The Consumer API is also available directly at `http://localhost:5100/api/v1/tracks/current`, and its OpenAPI document at `http://localhost:5100/openapi/v1.json`; both host ports are bound to `127.0.0.1` only. The consumer connects to PostgreSQL using the Compose DNS name `postgres` and port `5432`; host-side tools such as `dotnet ef` can use `localhost:5433`. PostgreSQL's host port is also bound to `127.0.0.1`. The consumer listens on `0.0.0.0` inside its container, and the producer sends to the Compose DNS service name `consumer`. The UDP port is exposed to other Compose services but is not published to the host. This keeps the sample receiver local to the Compose network and avoids connecting to external telemetry sources.

The receiver's configured bind address defaults to `127.0.0.1` when running outside Docker; Compose overrides it because container loopback is private to each container. Likewise, the producer's receiver host defaults to `127.0.0.1` for local non-container runs and Compose sets it to `consumer`.

## Logs

The foreground `docker compose up` command streams both services' logs. To inspect logs later:

```console
docker compose logs
docker compose logs -f consumer
docker compose logs -f producer
docker compose logs -f dashboard
```

## Windows and Linux

The same Compose commands and configuration work on both platforms. On Windows, start Docker Desktop and ensure it is using Linux containers; on Linux, ensure the Docker daemon is running and your account has permission to use it. The examples use PowerShell syntax for Windows environment variables and POSIX shell syntax for Linux.

## Running outside containers

The existing `dotnet run` instructions in [REQUIREMENTS.md](./REQUIREMENTS.md) remain valid for the producer. Start PostgreSQL with `docker compose up -d postgres` before running EF CLI commands or the consumer on the host. Consumer appsettings defaults connect to the Compose database at `localhost:5433`; override `ConnectionStrings__Telemetry` if you changed the Compose password or host port. The consumer applies pending migrations at startup. Both apps default to loopback when run directly; the consumer supports `Telemetry__ListenAddress`, and the producer supports `Telemetry__ReceiverHost` if the apps need to communicate across hosts or containers.

The consumer uses EF Core with the Npgsql provider and applies pending migrations at startup. The migration history is tracked in `__EFMigrationsHistory`; the conditional PostgreSQL upsert remains explicit SQL because it atomically prevents stale updates. Each accepted, newer report is stored in `telemetry_reports`, and the latest report per `(source_id, track_id)` is maintained in `latest_tracks`. Duplicate and stale reports do not create history entries. Both tables survive restarts through the Compose volume.

The repository pins the EF CLI to version `9.0.3`. From the `ATOR` folder, restore the local tool before using EF commands:

```powershell
dotnet tool restore
```

Create a migration after changing the EF model:

```powershell
dotnet ef migrations add DescribeSchemaChange --project .\Consumer\ATOR.Consumer.csproj
```

Review and commit the generated migration and model snapshot. Check migration status with:

```powershell
dotnet ef migrations list --project .\Consumer\ATOR.Consumer.csproj
```

These host-side EF commands connect to the Compose database through `localhost:5433`. Start it first with `docker compose up -d postgres`.

The consumer checks for and applies pending migrations at startup, and logs whether any were applied. The initial migration uses `CREATE TABLE IF NOT EXISTS` so it can establish EF's migration history for a database created by the earlier `EnsureCreated` implementation without replacing its existing tables. Back up existing data before upgrading, and verify the existing tables match the initial schema.
