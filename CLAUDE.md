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

Backend integration tests need a real Postgres. Spin up a throwaway one before `dotnet test`:
```
docker run -d --rm --name paraglog-test-db -p 5434:5432 -e POSTGRES_DB=paraglog -e POSTGRES_USER=paraglog -e POSTGRES_PASSWORD=paraglog postgres:17-alpine
export ConnectionStrings__Default="Host=localhost;Port=5434;Database=paraglog;Username=paraglog;Password=paraglog"
```

## Rules

- **Layering**: `Core` has no dependency on Postgres/filesystem (no `Npgsql`, no file I/O in `Core`). `Infrastructure` implements `Core` interfaces. `Api` wires DI and exposes HTTP only — no SQL or business logic in endpoint methods.
- **Every repository method takes `userId`** and puts it in the `WHERE` clause — authorization is per-query, not a separate check.
- Track files (`.gpx`/`.igc`) are never stored in Postgres — only metadata and a filesystem path.
- Follow the branching/versioning model in `SPEC.md`: `dev` for work in progress, tag and merge to `main` at the milestones listed in `ARCHITECTURE.md`.
- Build milestones (Steps 1–10) are defined in `plans/ARCHITECTURE.md` — check current progress there before starting new work.

## Full-stack smoke testing

`docker compose up --build` in `deploy/` starts four services: `db`, `backend` (:8080, direct), `viewer` (:8081, **static nginx only — no `/api` proxy**), and `proxy` (Caddy, :80). Caddy is the real entry point: it routes `/api/*` to `backend` and everything else to `viewer`. Test end-to-end requests against `http://localhost` (port 80), not `:8081` — a POST to `/api/...` on the viewer's own port 405s, since nginx has no route for it.

## Known gotchas

- **Dapper + Postgres enums**: Dapper's parameter binder and its row deserializer both bypass a registered `SqlMapper.TypeHandler<TEnum>` for enum-typed properties/parameters (`Enum.Parse`/int-casting happens in generated code). Read enum columns as `text` (`column::text AS "Alias"`) into a row DTO and convert explicitly via `PgEnumTypeHandler<T>.FromLabel`/`ToLabel`; cast write-side parameters with `@Param::pg_enum_type` in SQL.
- **Viewer test setup**: jsdom polyfills (`matchMedia`, canvas 2D context, `ResizeObserver`) and cross-test singleton resets (`queryClient.clear()`, `resetSharedCursorForTests()`) all live in `viewer/src/test/setup.ts`'s global `afterEach`. Extend it — don't duplicate it — when a new module-level singleton or missing jsdom API shows up.
- **Streaming a `ZipArchive` to the HTTP response** (`GET /api/export`): `ZipArchive.Dispose()` writes its central directory synchronously, and Kestrel disallows synchronous response writes by default — see the `IHttpBodyControlFeature.AllowSynchronousIO` opt-in in `ExportEndpoints.cs`.
- **Rate limiting**: `AddFixedWindowLimiter` with no partitioning shares one counter across *every* request to that policy. The auth limiter partitions by client IP (`RateLimitPartition.GetFixedWindowLimiter`) so concurrent tests/users don't share a quota.
- **Viewer theming**: `--accent-bg` is a translucent tint meant for backgrounds, not a text color. Use `--accent-text` for text sitting on a solid `--accent` background (e.g. primary buttons).
