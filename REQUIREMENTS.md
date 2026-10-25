# Airspace Situational Awareness Simulator Requirements

## Purpose and scope

Build a C# learning project that combines **locally generated, civilian simulation telemetry** from multiple sources, displays current simulated tracks on a live map, and raises configurable alerts.

The application must not connect to real aircraft, operational networks, or live telemetry sources. Network communication is restricted to the local simulator and receiver used for development and testing.

## Initial architecture

- A separately deployable .NET producer app generates sample track reports and sends them periodically over UDP.
- A separately deployable, long-running .NET consumer app accepts one report per UDP datagram and emits a JSON report of the latest tracks after each accepted update.
- The producer and consumer share the telemetry model through a non-executable .NET class library.
- The receiver binds to `127.0.0.1` only and uses port `5000` by default; override it with the `Telemetry:UdpPort` configuration setting.
- JSON and XML adapters validate and normalize reports into one domain model.
- The service maintains the latest report for each `(sourceId, trackId)` pair and detects duplicate or stale reports.
- PostgreSQL stores current tracks and accepted telemetry history.
- An ASP.NET Core REST API exposes tracks, alerts, and health.
- A browser dashboard displays simulated tracks and alerts.

The initial dashboard mapping library remains an implementation choice. Redis, gRPC, and a separate frontend framework are optional extensions, not MVP requirements.

## Telemetry contract

Every report contains:

| Field | Type | Requirement |
|---|---|---|
| `sourceId` | string | Required, non-empty identifier for the simulated source. |
| `trackId` | string | Required, non-empty identifier unique within its source. |
| `timestampUtc` | timestamp | Required ISO 8601 UTC timestamp, represented with `Z` or a zero UTC offset. |
| `latitudeDegrees` | number | Finite number from -90 through 90, inclusive. |
| `longitudeDegrees` | number | Finite number from -180 through 180, inclusive. |
| `altitudeMeters` | number | Finite, non-negative number. |
| `groundSpeedMetersPerSecond` | number | Finite, non-negative number. |

The logical JSON property names are camelCase. XML uses a `TrackReport` root element and PascalCase child elements. Both formats represent the same fields and normalize to the same C# model.

### JSON example

```json
{
  "sourceId": "COLLECTOR-01",
  "trackId": "DRONE-001",
  "timestampUtc": "2026-10-22T15:00:00Z",
  "latitudeDegrees": 37.9838,
  "longitudeDegrees": 23.7275,
  "altitudeMeters": 1000,
  "groundSpeedMetersPerSecond": 60
}
```

### XML example

```xml
<TrackReport>
  <SourceId>COLLECTOR-01</SourceId>
  <TrackId>DRONE-001</TrackId>
  <TimestampUtc>2026-10-22T15:00:00Z</TimestampUtc>
  <LatitudeDegrees>37.9838</LatitudeDegrees>
  <LongitudeDegrees>23.7275</LongitudeDegrees>
  <AltitudeMeters>1000</AltitudeMeters>
  <GroundSpeedMetersPerSecond>60</GroundSpeedMetersPerSecond>
</TrackReport>
```

## Data handling and alerts

- Track identity is the `(sourceId, trackId)` pair; separate sources may use the same track ID.
- A report with the same identity and timestamp as one already accepted is a duplicate and must not create a second history entry or alert.
- A report older than the latest accepted timestamp for its identity is stale and must not replace the current track.
- Invalid or unsupported payloads are rejected with an explicit error/result and must not stop the receiver from processing later reports.
- Restricted test zones are configurable geographic areas. An alert is raised when an accepted track enters a configured zone.
- Start with no restricted zones configured. Users will manage zones through a dedicated REST API; until that API is implemented, zones can be configured in the consumer's `Alerting:Zones` application settings.
- A track is considered silent after its configured stale timeout; the timeout must be configurable rather than embedded in alert logic.
- Alert history records the alert type, track identity, occurrence time, and relevant zone or timeout details. `ObservedTimestampUtc` identifies the telemetry report used to evaluate the alert. `OccurredAtUtc` identifies when the alert condition began; for transmission loss it is the last report timestamp plus the configured stale timeout, not when the periodic checker later detected it. For zone entry, both timestamps are the accepted report timestamp. Repeated checks do not create duplicate transmission-lost alerts for the same last report.

## Acceptance criteria

1. The simulator can generate reports for multiple tracks and identify the source that generated them.
2. JSON and XML examples above parse to equivalent domain values; missing fields, malformed payloads, invalid coordinates, and negative altitude or speed are rejected explicitly.
3. Repeated reports and out-of-order timestamps do not duplicate history or roll back a current track.
4. The hosted application remains active while listening for local UDP reports, shuts down cleanly, and continues after a rejected datagram.
5. Configured zone-entry and transmission-timeout conditions produce queryable, persisted alerts.
6. The REST API exposes current tracks, paginated track history, recent alert history, and service health; automated tests cover success and failure cases.
7. The dashboard shows current simulated positions and alerts and updates as new reports arrive.
8. The project can be built and its automated tests run on Windows and Linux using documented steps.
9. Performance testing records throughput, processing latency, and memory use for a repeatable simulated load; results include the test environment and load profile.

## Running the two applications

Set the PostgreSQL connection string before starting the consumer. For example, with a PostgreSQL instance running locally:

```powershell
$env:ConnectionStrings__Telemetry = "Host=localhost;Port=5432;Database=ator;Username=your_user;Password=your_password"
```

Start the consumer first:

```powershell
dotnet run --project .\Consumer\ATOR.Consumer.csproj
```

In a second console, start the producer:

```powershell
dotnet run --project .\Producer\ATOR.Producer.csproj
```

Both apps are separate deployable projects and use UDP port `5000` by default. Configure the port with `Telemetry__UdpPort`. The producer also supports `Telemetry__IntervalSeconds`, `Telemetry__TrackCount`, and `Telemetry__SourceId`; use different source IDs when running multiple local producers.

For separate Docker containers and Docker Compose instructions, see [DOCKER.md](./DOCKER.md).

## Out of scope

- Real aircraft/drone interfaces, operational command/control, or connection to external telemetry.
- Operational mission planning, targeting, or control of unmanned systems.
- Production deployment, authentication, and high-availability design in the initial learning-project milestones.
