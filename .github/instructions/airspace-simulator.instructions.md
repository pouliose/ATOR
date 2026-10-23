---
description: "Use when developing the ATOR simulated airspace situational-awareness project. Follow its staged C# implementation roadmap and keep all telemetry simulation-only."
applyTo: "**"
---
# Airspace Simulator Project Instructions

Build this project incrementally as a civilian, simulation-only airspace situational-awareness system. Do not connect it to real aircraft, operational networks, or live telemetry sources.

## Project scope and engineering practices

- This is a civilian, simulation-only project. Generate data locally; never connect to real aircraft, operational networks, or live telemetry.
- Keep telemetry fields consistent: track ID, UTC timestamp, latitude, longitude, altitude, and ground speed.
- Keep each step small, testable, and independently runnable; update `TODO.md` as the agreed scope or progress changes.
- Prefer clear interfaces and explicit validation/errors over silent fallback behavior.
- Run the narrowest relevant build and tests after each implementation change.
- Avoid adding frameworks or services before a milestone needs them.
