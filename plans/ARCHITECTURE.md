# Architecture

Companion to `SPEC.md`. This document covers code structure, folder layout, layering rules and the build order.

# Repository layout

```
paraglog/
├── backend/
│   ├── ParagLog.sln
│   ├── src/
│   │   ├── ParagLog.Core/
│   │   ├── ParagLog.Infrastructure/
│   │   └── ParagLog.Api/
│   └── tests/
│       ├── ParagLog.UnitTests/
│       └── ParagLog.IntegrationTests/
├── viewer/
├── deploy/
│   ├── docker-compose.yml
│   ├── docker-compose.dev.yml
│   └── Caddyfile
├── .github/workflows/
│   ├── ci.yml
│   └── release.yml
├── SPEC.md
├── ARCHITECTURE.md
└── README.md
```

# Backend

## Layering

Three projects, with dependencies pointing inward only.

```
ParagLog.Api ──────► ParagLog.Core ◄────── ParagLog.Infrastructure
```

`Core` references nothing but the BCL. It holds domain models, the repository interfaces and the business rules. `Infrastructure` implements those interfaces against PostgreSQL and the filesystem. `Api` wires them together and exposes HTTP.

The rule that matters: **`Core` must not know that PostgreSQL or the filesystem exist.** If you find yourself adding `using Npgsql` to a `Core` file, the logic belongs in `Infrastructure` or the abstraction is wrong.

## Folder structure

```
backend/src/ParagLog.Core/
├── Abstractions/
│   ├── IUserRepository.cs
│   ├── ISessionRepository.cs
│   ├── IActivityRepository.cs
│   ├── IEquipmentRepository.cs
│   ├── IBlobStore.cs                 # get/put/delete a track file
│   ├── IElevationService.cs
│   ├── IPasswordHasher.cs
│   └── IUnitOfWork.cs
├── Activities/
│   ├── Activity.cs
│   ├── ActivityType.cs
│   ├── TrackReference.cs             # filename, format, size, sha256
│   └── ActivityService.cs            # create/update/delete orchestration
├── Equipment/
│   ├── Equipment.cs
│   ├── EquipmentType.cs
│   ├── EquipmentRevision.cs
│   ├── EquipmentUsage.cs
│   └── EquipmentService.cs           # incl. delete-vs-retire rule
├── Users/
│   ├── User.cs
│   ├── Session.cs
│   ├── UserService.cs
│   └── PublicIdGenerator.cs
├── Export/
│   └── ExportService.cs
└── Common/
    ├── Result.cs                     # Result<T> instead of exceptions for expected failures
    ├── DomainError.cs
    ├── PathSafety.cs                 # Windows reserved names, traversal guard
    └── Validation/
        ├── UsernameRules.cs
        ├── EmailRules.cs
        └── PasswordRules.cs
```

```
backend/src/ParagLog.Infrastructure/
├── Persistence/
│   ├── NpgsqlConnectionFactory.cs
│   ├── UnitOfWork.cs                 # wraps NpgsqlTransaction
│   ├── UserRepository.cs
│   ├── SessionRepository.cs
│   ├── ActivityRepository.cs
│   ├── EquipmentRepository.cs
│   ├── TypeHandlers/                 # Dapper handlers for enums, DateOnly
│   └── Sql/                          # one .sql per query, embedded resource
│       ├── Activities.List.sql
│       ├── Activities.GetById.sql
│       └── ...
├── Migrations/
│   ├── 0001_initial_schema.sql
│   ├── 0002_....sql
│   └── MigrationRunner.cs            # DbUp
├── Storage/
│   └── FileSystemBlobStore.cs        # atomic temp-file + move
├── Elevation/
│   ├── OpenTopoDataClient.cs
│   └── TrackDownsampler.cs
├── Security/
│   └── Argon2PasswordHasher.cs
└── DependencyInjection.cs            # AddInfrastructure(IServiceCollection)
```

```
backend/src/ParagLog.Api/
├── Program.cs                        # ~40 lines: builder, DI, middleware, MapEndpoints
├── Endpoints/
│   ├── AuthEndpoints.cs
│   ├── ActivityEndpoints.cs
│   ├── TrackEndpoints.cs
│   ├── EquipmentEndpoints.cs
│   └── ExportEndpoints.cs
├── Contracts/                        # request/response records; never expose Core models
│   ├── Activities/
│   ├── Equipment/
│   └── Auth/
├── Auth/
│   ├── SessionCookieAuthHandler.cs
│   ├── GoogleAuthSetup.cs
│   └── CurrentUser.cs                # scoped, resolved from the session cookie
├── Middleware/
│   ├── ExceptionHandler.cs           # → ProblemDetails
│   └── RequestLogging.cs
├── appsettings.json
└── Dockerfile
```

## Conventions

**Endpoints are thin.** An endpoint file maps routes and translates between `Contracts` and `Core`. No SQL, no business rules. If an endpoint method passes 15 lines, the logic belongs in a `*Service`.

```csharp
public static class ActivityEndpoints
{
    public static void MapActivityEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/activities").RequireAuthorization();

        group.MapGet("/", ListAsync);
        group.MapPost("/", CreateAsync);
        // ...
    }

    private static async Task<IResult> CreateAsync(
        CreateActivityRequest request,
        ICurrentUser user,
        ActivityService activities,
        CancellationToken ct)
    {
        var result = await activities.CreateAsync(user.Id, request.ToCommand(), ct);
        return result.IsSuccess
            ? Results.Created($"/api/activities/{result.Value.Id}", ActivityResponse.From(result.Value))
            : result.ToProblem();
    }
}
```

**One SQL file per query, embedded as a resource.** Keeps C# readable, makes SQL diffable, and lets you paste a query straight into `psql` when it misbehaves.

**`Result<T>` for expected failures**, exceptions for bugs. "Username taken" and "equipment in use" are outcomes, not exceptions.

**Every repository method takes `userId`.** Not as a courtesy — it goes into the `WHERE` clause of every statement, so a forgotten authorization check returns zero rows instead of another user's data.

**Migrations are append-only.** Never edit a shipped `.sql`; add a new numbered file. DbUp runs them on startup inside a transaction and records them in a `schema_versions` table.

# Viewer

## Folder structure

```
viewer/
├── index.html
├── vite.config.ts
├── tsconfig.json
├── package.json
├── Dockerfile
├── nginx.conf
└── src/
    ├── main.tsx
    ├── App.tsx
    ├── app/
    │   ├── providers.tsx             # QueryClient, auth, theme
    │   ├── queryClient.ts
    │   └── routes.tsx
    ├── features/
    │   ├── auth/
    │   │   ├── components/           LoginForm, RegisterForm, GoogleButton
    │   │   ├── api.ts
    │   │   ├── useAuth.ts
    │   │   └── types.ts
    │   ├── activities/
    │   │   ├── components/
    │   │   │   ├── ActivityList.tsx
    │   │   │   ├── ActivityListItem.tsx
    │   │   │   ├── ActivityDetail.tsx
    │   │   │   ├── ActivityForm.tsx
    │   │   │   ├── EquipmentPicker.tsx
    │   │   │   └── DeleteActivityDialog.tsx
    │   │   ├── api.ts
    │   │   ├── queries.ts            # useActivities, useActivity, mutations
    │   │   ├── useSelectedActivity.ts
    │   │   └── types.ts
    │   ├── equipment/
    │   │   ├── components/           List, Detail, Form, UsageSection, DeleteDialog
    │   │   ├── api.ts
    │   │   ├── queries.ts
    │   │   └── types.ts
    │   ├── settings/
    │   │   └── components/           UserSettingsView, UserInfoView
    │   ├── map/
    │   │   ├── FlightMap.tsx
    │   │   ├── TrackLayer.tsx
    │   │   ├── CursorMarker.tsx
    │   │   ├── baseLayers.ts
    │   │   └── useFitBounds.ts
    │   └── profile/
    │       ├── AltitudeProfile.tsx   # uPlot wrapper
    │       ├── EmptyProfile.tsx      # activities without a track
    │       └── profileOptions.ts
    ├── components/
    │   ├── layout/
    │   │   ├── AppShell.tsx          # decides desktop split vs mobile sheet
    │   │   ├── LogbookPanel.tsx
    │   │   ├── PanelHeader.tsx       # title, back arrow, gear menu
    │   │   ├── PanelResizer.tsx
    │   │   ├── MobileSheet.tsx
    │   │   └── MapStage.tsx
    │   └── ui/                       Button, Dialog, Dropdown, Field, Spinner, EmptyState
    ├── lib/
    │   ├── api/
    │   │   ├── client.ts             # fetch wrapper, credentials: 'include', 401 → login
    │   │   └── problem.ts            # ProblemDetails → field errors
    │   ├── tracks/
    │   │   ├── parseGpx.ts
    │   │   ├── parseIgc.ts
    │   │   ├── simplify.ts
    │   │   ├── stats.ts              # duration, gain, distance, max altitude
    │   │   └── types.ts              # TrackPoint, ParsedTrack
    │   ├── panel/
    │   │   ├── ViewStack.tsx         # push/pop, back arrow, browser history
    │   │   └── usePanelStack.ts
    │   ├── storage/
    │   │   └── localSettings.ts      # panel width, last base layer
    │   └── format/
    │       ├── duration.ts
    │       └── datetime.ts
    ├── hooks/
    │   ├── useMediaQuery.ts
    │   └── useSharedCursor.ts        # links map marker and profile cursor
    └── styles/
```

## Conventions

**Feature-first, not type-first.** Everything for equipment lives under `features/equipment/`. Adding a feature means adding a folder, not editing eight shared ones. The exception is `components/ui`, which is genuinely cross-cutting and holds no domain knowledge.

**Server state lives in TanStack Query, UI state in React state.** No Redux. The activity list, detail and equipment are queries with cache keys; the panel stack, selected activity and panel width are local state.

**`lib/tracks` is pure and dependency-free.** No React, no fetch — just functions from file text to `ParsedTrack`. That makes the parsing testable with Vitest and reusable if parsing ever moves server-side.

**The shared cursor is one hook.** `useSharedCursor` owns a single `{ index }` value; the map marker and profile cursor both read it. Two-way syncing between components without a shared owner is where this design usually breaks.

**Leaflet stays behind `features/map`.** No other feature imports `leaflet` directly, so swapping to MapLibre later touches one folder.

# Cross-cutting decisions

**Time.** Backend stores and returns `timestamptz` as ISO-8601 UTC. The viewer formats to local. `local_date` and `local_tz` come from the track's first fix when available, otherwise the browser's zone at creation time.

**Errors.** The API returns RFC 7807 `ProblemDetails` with an `errors` dictionary for field-level validation, which `lib/api/problem.ts` maps directly onto form fields.

**Validation lives twice, on purpose.** The client validates for feedback; the server validates for correctness. `Core/Common/Validation` is authoritative; the client copy is a convenience. Never trust the client copy.

**IDs.** Server-generated UUIDv7 for activities and equipment, so list ordering is roughly insertion-ordered even before sorting.

# Development milestones

Each step ends with a working application and a commit on `dev`. Merge to `main` at the tagged steps.

**Step 1 — Skeleton.** Solution, three projects, Vite app, Docker Compose with PostgreSQL, CI running build and test on both. No features. *Commit: scaffolding builds and runs in Docker.*

**Step 2 — Database and migrations.** `0001_initial_schema.sql` with the full schema. DbUp runner. Integration test using Testcontainers that applies migrations to a fresh database. *Commit: schema applies cleanly.*

**Step 3 — Accounts.** Register, login, logout, session cookie, Argon2id, public ID generation with the Windows reserved-name guard. Unit tests for `PublicIdGenerator` and the validation rules. Frontend login and register forms. *Commit: a user can register and stay logged in.* → tag `v0.1.0`

**Step 4 — App shell.** `AppShell`, `LogbookPanel`, `ViewStack`, resizer with persisted width, mobile breakpoint and bottom sheet. Empty states throughout. No data yet. *Commit: layout works on desktop and mobile.*

**Step 5 — Activities without tracks.** Full CRUD, list with infinite scroll, detail, create/edit forms, delete dialog, auto-select most recent. *Commit: usable logbook for manual entries.* → tag `v0.2.0`

**Step 6 — Equipment.** CRUD, revision list, `auto_add` pre-selection in the activity form, usage aggregate, delete-vs-retire rule with the 409 path. *Commit: equipment manageable and linked to activities.* → tag `v0.3.0`

**Step 7 — Track parsing (client only).** `lib/tracks` with GPX and IGC parsers, stats and simplification, covered by Vitest against real sample files. Auto-fill in the create form. Nothing uploaded yet. *Commit: parsing tested in isolation.*

**Step 8 — Track upload and map.** Blob store, upload and download endpoints, delete ordering, `FlightMap` with base layers and track rendering, fit-to-bounds. *Commit: tracks visible on the map.* → tag `v0.4.0`

**Step 9 — Altitude profile.** uPlot integration, shared cursor with the map, empty-profile variant, elevation service and `elevation.json` caching. *Commit: full visualization.* → tag `v0.5.0`

**Step 10 — Export and hardening.** Streamed ZIP export, orphan sweeper, rate limiting on auth endpoints, structured logging, health checks, Caddy config. *Commit: production ready.* → tag `v1.0.0`

Steps 7 and 8 are deliberately separate. Parsing is the part most likely to surprise you, and debugging it is far easier without an upload pipeline in the way.
