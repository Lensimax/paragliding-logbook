# Build milestones

Tracks the steps in `plans/ARCHITECTURE.md` § Development milestones. Check a step when its commit lands on `dev` (and note the tag once merged to `main`).

- [x] **Step 1 — Skeleton.** Solution, three projects, Vite app, Docker Compose with PostgreSQL, CI running build and test on both. No features.
- [x] **Step 2 — Database and migrations.** `0001_initial_schema.sql` with the full schema. DbUp runner. Integration test using Testcontainers that applies migrations to a fresh database.
- [x] **Step 3 — Accounts.** Register, login, logout, session cookie, Argon2id, public ID generation with the Windows reserved-name guard. Unit tests for `PublicIdGenerator` and the validation rules. Frontend login and register forms. → tag `v0.1.0`
- [x] **Step 4 — App shell.** `AppShell`, `LogbookPanel`, `ViewStack`, resizer with persisted width, mobile breakpoint and bottom sheet. Empty states throughout. No data yet.
- [x] **Step 5 — Activities without tracks.** Full CRUD, list with infinite scroll, detail, create/edit forms, delete dialog, auto-select most recent. → tag `v0.2.0`
- [x] **Step 6 — Equipment.** CRUD, revision list, `auto_add` pre-selection in the activity form, usage aggregate, delete-vs-retire rule with the 409 path. → tag `v0.3.0`
- [x] **Step 7 — Track parsing (client only).** `lib/tracks` with GPX and IGC parsers, stats and simplification, covered by Vitest against real sample files. Auto-fill in the create form. Nothing uploaded yet.
- [x] **Step 8 — Track upload and map.** Blob store, upload and download endpoints, delete ordering, `FlightMap` with base layers and track rendering, fit-to-bounds. → tag `v0.4.0`
- [x] **Step 9 — Altitude profile.** uPlot integration, shared cursor with the map, empty-profile variant, elevation service and `elevation.json` caching. → tag `v0.5.0`
- [x] **Step 10 — Export and hardening.** Streamed ZIP export, orphan sweeper, rate limiting on auth endpoints, structured logging, health checks, Caddy config. → tag `v1.0.0`
