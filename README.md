# ParagLog

Paragliding logbook: flight and ground-handling tracking with GPS track visualization.

See `plans/SPEC.md` and `plans/ARCHITECTURE.md` for the product spec and code layout.

## Features

- **Accounts** — register/login with email + password (Argon2id), session cookies, "stay connected."
- **Logbook** — create, edit, and delete flights and ground-handling sessions; infinite-scroll list with the most recent activity auto-selected.
- **Equipment** — wings, harnesses, reserves, etc. with revision history, usage counts, and a delete-vs-retire rule so past activities never silently lose their gear.
- **Track upload** — drop in a GPX or IGC file; distance, altitude gain, max altitude, and takeoff point are auto-filled client-side. The original file is kept byte-for-byte.
- **Map + altitude profile** — the track renders on a Leaflet map with switchable base layers, synced to a uPlot altitude-vs-time chart via a shared hover cursor. Tracks without timestamps (some GPS apps omit them) still render on the map; the profile falls back to an explanatory message since its x-axis is time-based.
- **Ground elevation** — a one-time lookup against OpenTopoData caches a ground-elevation profile alongside the flight altitude, so climb/glide is visible against real terrain.
- **Export** — download the whole logbook as a ZIP (`activities.csv`, `equipment.csv`, every track file) from User Settings.
- **Production hardening** — rate limiting on auth endpoints, structured JSON logging, a real `/health` check backed by the database, a weekly orphaned-blob sweep, and Caddy fronting the stack.

## What's new

- Streamed logbook export (ZIP) and a weekly orphan-blob sweeper.
- Rate limiting on register/login, structured logging, and a database-backed health check wired into Docker.
- Caddy now proxies the full stack on port 80 (`deploy/docker-compose.yml`).
- Fixed: the map no longer disappears for tracks with no timestamps — only the altitude profile needs them.
- Fixed: primary button text (e.g. "Save", "Create and import") was unreadable against its own background.
- Lowered the minimum password length from 12 to 8 characters.

## Development

Backend (ASP.NET Core, targets `net10.0`):

```
cd backend
dotnet build ParagLog.sln
dotnet test ParagLog.sln
```

Viewer (Vite + React + TypeScript):

```
cd viewer
npm install
npm run dev
npm run test
```

Full stack via Docker Compose:

```
cd deploy
docker compose up --build
```
