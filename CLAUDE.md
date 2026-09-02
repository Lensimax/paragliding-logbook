# ParagLog

Paragliding logbook + flight track viewer. Full spec: `plans/SPEC.md`. Code structure, layering rules and build milestones: `plans/ARCHITECTURE.md`. Read both before making non-trivial changes.

## Layout

- `backend/` — ASP.NET Core (`net10.0`) solution `ParagLog.sln`: `ParagLog.Core`, `ParagLog.Infrastructure`, `ParagLog.Api`, plus `ParagLog.UnitTests`/`ParagLog.IntegrationTests` under `backend/tests/`.
- `viewer/` — Vite + React + TypeScript.
- `deploy/` — Docker Compose (`docker-compose.yml`, `docker-compose.dev.yml`, `Caddyfile`).
- `.github/workflows/ci.yml` — builds and tests both backend and viewer.

## Commands

Backend:
```
cd backend
dotnet build ParagLog.sln
dotnet test ParagLog.sln
```

Viewer:
```
cd viewer
npm install
npm run build
npm run test
```

Full stack:
```
cd deploy
docker compose up --build
```

## Rules

- **Layering**: `Core` has no dependency on Postgres/filesystem (no `Npgsql`, no file I/O in `Core`). `Infrastructure` implements `Core` interfaces. `Api` wires DI and exposes HTTP only — no SQL or business logic in endpoint methods.
- **Every repository method takes `userId`** and puts it in the `WHERE` clause — authorization is per-query, not a separate check.
- Track files (`.gpx`/`.igc`) are never stored in Postgres — only metadata and a filesystem path.
- Follow the branching/versioning model in `SPEC.md`: `dev` for work in progress, tag and merge to `main` at the milestones listed in `ARCHITECTURE.md`.
- Build milestones (Steps 1–10) are defined in `plans/ARCHITECTURE.md` — check current progress there before starting new work.
