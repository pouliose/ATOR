# Project Todo

Build a C# application that receives simulated aircraft or drone telemetry from several sources, combines it into a live map, and raises configurable alerts. 

## Current status

**Completed so far**

- Requirements, message examples, validation rules, and acceptance criteria are documented in `REQUIREMENTS.md`.
- The solution has two independently runnable .NET apps: `Consumer/` and `Producer/`.
- `Contracts/` contains the shared telemetry model used by both apps.
- The producer sends simulated JSON telemetry over local UDP.
- The consumer receives UDP datagrams, validates JSON reports, ignores duplicate and stale updates, and logs a current-tracks JSON report.
- The consumer persists accepted telemetry history and the latest track state in PostgreSQL; Docker Compose includes a persistent PostgreSQL service.
- Producer and consumer can be built as separate Docker images and run together with Docker Compose; container configuration, networking, startup order, logs, and Windows/Linux instructions are documented in `DOCKER.md`.
- Both apps and the complete solution build successfully.

**Current next step:** add automated tests for JSON parsing, validation, duplicate/stale handling, alerting, and simulator behavior. XML parsing is deferred.

## Completed milestones

- [x] Define requirements, acceptance criteria, and message examples; use UTC timestamps and a shared track-report model.
- [x] Create separately deployable .NET producer and consumer projects with a shared telemetry-contract library.
- [x] Producer generates sample JSON telemetry over local UDP; consumer remains active, validates it, tracks latest reports, and logs current-track reports.
- [x] Receive simulated telemetry over local UDP; handle malformed, delayed, duplicate, and out-of-order messages.
- [x] Keep the consumer host running as a `BackgroundService` and listen for local UDP datagrams.
- [x] Parse and validate JSON datagrams; continue receiving after malformed messages.
- [x] Ignore duplicate and stale reports without replacing a newer current track.

## Next implementation milestones

- [ ] Add XML telemetry parsing alongside the consumer's existing JSON parsing and validation (deferred).
- [ ] Add automated tests for JSON parsing, validation boundaries, duplicate/stale handling, alerting, and simulator behavior.
- [x] Add configurable alerts for restricted test-zone entry and transmission loss; persist and expose recent alert history through the REST API.
- [ ] Add REST API endpoints for users to create, update, list, and remove restricted zones; start with no zones configured.
- [x] Persist accepted reports, current track state, and alert history in PostgreSQL.
- [x] Add EF Core migrations for managed PostgreSQL schema changes.
- [ ] Expose tracks and service health through a REST API with structured logs and clear errors (alert history is exposed).
- [ ] Add end-to-end integration tests for producer, consumer, persistence, alerts, and API behavior.
- [x] Build a live map dashboard for simulated tracks; the recent alert-history page is implemented.

## Later work

- [x] Deploy the producer and consumer as separate Docker images/containers, with Docker Compose for local orchestration.
- [x] Document container configuration, networking, startup order, logs, and Windows/Linux run instructions.
- [ ] Measure throughput, processing latency, and memory use for repeatable simulated loads.

## Suggested stack

- .NET, ASP.NET Core, and `BackgroundService` for the receiver and simulator
- `System.Text.Json` and an XML serializer for message formats
- Entity Framework Core with PostgreSQL; explicit SQL for PostgreSQL-specific operations
- xUnit for tests
- Docker and Docker Compose for later separate deployment

## Optional extensions

- [x] Add PostgreSQL support for consumer telemetry persistence.
- [ ] Explore Redis, gRPC, or a React/Angular/Vue dashboard if justified by project needs.
