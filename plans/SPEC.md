# Purpose

The purpose of this website is to store for each user a logbook of paragliding flights. It also tracks ground handling sessions. The website is also able to visualize flight GPS data (altitude) on a base map (Leaflet).

The system has a viewer (web client) and a backend program.

# Vocabulary

| Term | Meaning |
|---|---|
| **Activity** | Any logbook entry. Either a **flight** or a **ground handling** session. |
| **Track** | The uploaded GPS file (`.gpx` or `.igc`) attached to a flight. |
| **Logbook Panel** | The vertical panel holding the activity list, activity detail, forms and user settings. Called "right panel" in earlier drafts. |
| **Map Stage** | The map plus altitude profile filling the rest of the screen on desktop. |

> **Naming note.** "Right panel" was renamed to **Logbook Panel** because the panel is not always on the right: on mobile it takes the full width and the map moves behind it. The name now describes what it contains rather than where it sits.

# Architecture

| Component | Technology | Role |
|---|---|---|
| Viewer | Vite + TypeScript + React + Leaflet + uPlot | Map, altitude profile, forms. Parses GPX/IGC client-side. |
| Backend | ASP.NET Core 9 Minimal API | REST API, auth, metadata CRUD, blob storage, export. |
| Database | PostgreSQL 17 | All metadata: users, activities, equipment. |

Track files are **not** stored in the database. They live on disk; the database holds a reference to them.

See `ARCHITECTURE.md` for the code structure, folder layout and build milestones.

# Network

The viewer communicates with the backend over a REST API.

Viewer and API are served from the same origin behind a single reverse proxy, so authentication uses cookies rather than tokens in browser storage.

# Accounts

The user creates an account with an email, a username and a password. Google Sign-In is also supported.

## Validation

- Username: `^[a-zA-Z0-9_-]{3,20}$`, case-insensitive uniqueness
- Email: RFC-valid, case-insensitive uniqueness (`citext` column)
- Password: minimum 12 characters, checked against a common-password list, confirmed with a repeat field

## Public ID

A public ID is derived at account creation from the username and a hash of the creation datetime. It is used as the blob folder name and in URLs. It is immutable.

```
public_id = slug(lowercase(username)) + "-" + base32(sha256(username + created_at_ticks + server_salt))[0..5]
```

Constraints, because the same ID must be a valid directory name on both Windows and Linux:

- Lowercase only. `Bob` and `bob` are the same folder on Windows but different on Linux.
- Reject Windows reserved device names: `CON`, `PRN`, `AUX`, `NUL`, `COM1`–`COM9`, `LPT1`–`LPT9`.
- Reject trailing dots and trailing spaces.

The internal primary key is a separate `uuid`. Foreign keys reference the UUID, never the public ID.

## Sessions

Server-side sessions stored in the database. The cookie carries only an opaque token.

- `HttpOnly`, `Secure`, `SameSite=Lax`
- Default lifetime 12 hours
- "Stay connected" sets a 30-day sliding expiration

Passwords are hashed with Argon2id.

## Personal information

No personal information is required beyond email. Google Sign-In stores only the Google subject identifier (`sub`) and the email.

# Viewer

## Interface layout

Two regions: the **Logbook Panel** and the **Map Stage**.

### Desktop

```
┌────────────────────────────────────────────┬──────────────────┐
│                                            │  username    [◉] │
│                                            ├──────────────────┤
│                  MAP STAGE                 │                  │
│                                            │  LOGBOOK PANEL   │
│                                            │  (view stack)    │
│                                            │                  │
├────────────────────────────────────────────┤                  │
│           ALTITUDE PROFILE                 │  [Create/import] │
└────────────────────────────────────────────┴──────────────────┘
                                             ↑ resizable
```

- The Logbook Panel sits on the right, full height, default width **15%** of the viewport.
- Its width is resizable by dragging its left edge, clamped to a **320 px minimum** and 40% maximum. Below 320 px the forms become unusable, so the percentage alone is not a sufficient rule.
- The chosen width is persisted in `localStorage` per device.
- The header shows the username on the left and a user icon on the right, which opens **User Settings**.

### Mobile

- The Logbook Panel takes the full width and is layered over the Map Stage.
- Selecting an activity collapses the panel to a bottom sheet at roughly 40% height, revealing the map and profile above it. The sheet can be dragged to full height or down to a peek handle.
- Resizing is disabled; the drag handle is not rendered.
- Breakpoint: 768 px.

### View stack

The Logbook Panel is a navigation stack, not a set of independent screens. Each view pushes onto the stack and the back arrow at the top left pops it.

```
Activity List
 ├─ Activity Detail ──── Activity Edit
 ├─ Activity Create
 └─ User Settings
     ├─ User Information
     └─ Equipment List
         ├─ Equipment Detail ──── Equipment Edit
         └─ Equipment Create
```

Modelling this as an explicit stack is what keeps the back arrow, the mobile sheet behaviour and browser history consistent. Treating each view as a standalone route leads to back-button bugs on mobile.

## Startup behaviour

On load, the viewer fetches the activity list and **auto-selects the most recent activity**. If the user has no activities, the list shows an empty state pointing at "Create/import".

## Activity list

- Sorted by start datetime, most recent first
- Each row: name, start datetime, duration, and a small icon distinguishing flight from ground handling
- Scrollable area with infinite scroll, 50 activities per page
- "Create/import" button pinned at the bottom, outside the scroll area

## Base map

Layer switcher with:

- Satellite: Esri World Imagery
- Topographic: OpenTopoMap
- (Optional, France) IGN Géoportail WMTS

Leaflet is initialised with `preferCanvas: true`. An SVG-rendered polyline of 10,000 points is unusably slow; canvas is not.

When an activity with a track is selected, the map fits the track bounds with padding. The polyline is simplified for display with Douglas-Peucker; full resolution is retained in memory for the profile and readout.

The viewer parses GPX and IGC client-side. GPX with `DOMParser`; IGC with `igc-parser`.

> **Format note:** earlier drafts said "IPC files". The vario format is **IGC** (International Gliding Commission). This document uses IGC throughout.

## Altitude visualization

A timeline at the bottom of the Map Stage, like an elevation profile in trail running, spanning the full activity from start to end:

- Flight altitude, when a track is present
- Ground elevation underneath it

Rendered with **uPlot**, which handles 10,000+ points without effort.

Hovering or clicking shows:

- Time since start
- Datetime at that moment
- Altitude
- Ground elevation

A shared cursor links the views: hovering the profile moves a marker along the polyline, hovering the map scrubs the profile.

**When the activity has no track**, the profile still renders its time axis from start to end datetime, with an empty series and a message. This keeps the layout stable between selections instead of collapsing and reflowing the map.

### Ground elevation

Not present in GPX or IGC and resolved separately. On upload the backend:

1. Downsamples the track to 300–500 points
2. Queries an elevation service in batch (OpenTopoData, or self-hosted `open-elevation`)
3. Writes `elevation.json` into the activity's blob folder

Computed **once per activity**. Resolving it per page view would be slow and rate-limited.

# User stories

## Register

The unauthenticated user clicks "Register" and provides username, email, password and password confirmation. Uniqueness of username and email is checked server-side; the client shows field-level errors. On success the user is logged in and lands on the empty activity list.

## Sign in with Google

The user clicks the Google button on the sign-in form. If the returned email matches an existing password account, the Google identity is linked to it rather than creating a duplicate. If the account is new, a username is proposed from the email local part and the user confirms it, since the username is immutable and drives the public ID.

## Sign in with email and password

The user enters email and password and clicks "Connect". A "Stay connected" checkbox controls session lifetime.

## View an activity

The user selects a row in the list. The panel pushes the detail view showing name, start datetime, duration and equipment. The map draws the track if present; the profile draws altitude. The back arrow at the top left returns to the list.

## Create an activity without flight data

"Create/import" at the bottom of the list opens the create form:

- Activity name (not an identifier; duplicates are allowed)
- Start datetime
- End datetime
- Takeoff location (optional)
- Landing location (optional)
- Equipment list, possibly empty. Equipment flagged **"Add automatically"** is pre-selected; the user can remove any entry before saving.
- User comment (optional)

The flight file input stays empty. On "Create", the list updates and the new activity becomes the selected one.

## Create an activity with flight data

Same form, with a file provided in "Flight file". The file is parsed **in the browser** and auto-fills:

- Name, from the file name without extension
- Start datetime, from the first fix
- End datetime, from the last fix

Every field remains editable. Parsing client-side means the user sees the fill and any parse error instantly, and the backend stays free of track parsing.

Upload is a two-step flow: `POST /api/activities` creates the row, then `POST /api/activities/{id}/track` uploads the file. If the second call fails, the activity exists without a track and the UI offers a retry, rather than losing the metadata the user just typed.

## Edit an activity

With an activity selected, the gear icon at the top right opens a dropdown with "Edit" and "Delete". "Edit" pushes the form pre-filled; "Save" returns to the detail view with updated information.

## Delete an activity

Gear icon → "Delete" opens a confirmation dialog. "Abort" returns to the detail view. "Delete" removes the row from the database and then removes the activity's blob folder from the server. The panel pops to the list with no selection.

## View equipment

User Settings → Equipment shows the list ordered by creation date, each row showing name and type. Selecting one shows all stored information, and at the bottom a **Usage** section:

- Number of hours used
- Number of activities

## Create equipment

From the equipment list, the create button opens a form:

- Display name
- Type: wing, harness, rescue parachute, helmet, radio, variometer
- Brand and model
- Purchase date
- Revision dates (a list, may be empty)
- Next revision date
- "Add automatically to new activities" toggle

## Edit equipment

Select equipment → gear icon → "Edit". All fields are editable. "Save" validates.

## Delete equipment

Select equipment → gear icon → "Delete". The confirmation dialog **shows the usage count**.

- Usage is zero: hard delete.
- Usage is non-zero: hard delete is refused. The dialog offers **Retire** instead, which hides the item from pickers while leaving past activities intact.

This is deliberate. Cascading the delete would silently rewrite the logbook, and a logbook whose history changes is not a logbook. The database enforces it with `ON DELETE RESTRICT`.

# Storage

## PostgreSQL — all metadata

Users, sessions, equipment, revisions, activities, and the links between them.

## Plain files — track data only

```
/data/blobs/
  {user_public_id}/
    activities/
      {activity_id}/
        track.gpx           original uploaded file, unmodified
        elevation.json      cached ground elevation profile
```

`{activity_id}` is the UUID from the database. No user-supplied string ever appears in a path.

The original file is kept byte-for-byte. Parsers improve, and for IGC the original is the record of the flight.

## Why tracks are not in the database

A track is 100 KB to 5 MB of text that is always read whole and never queried into. As a `bytea` column it would bloat every backup, break streaming downloads, and gain nothing. The database stores the path, format, size and SHA-256.

## Write and delete ordering

This is what keeps the two stores consistent without distributed transactions.

**Create:** write the blob first, then commit the row. A crash in between leaves an orphan file — harmless.

**Delete:** delete the row first, then the file. The reverse leaves a live row pointing at a missing file, which breaks the viewer.

The invariant: *an orphan file is acceptable, a dangling reference is not.*

## Backup

1. `pg_dump` the database
2. Copy the blob directory

In that order. Any activity created between the two steps then appears as an orphan file rather than a dangling row. A weekly sweeper compares blob folders against `activities.id` and reports orphans.

# Data Model

## Design decision: one `activities` table

Flights and ground handling sessions share name, start, end, location, equipment and comment. The UI presents them in one list, with one create form and one detail view. Two tables would mean two repositories, two sets of endpoints, and a `UNION ALL` on every list query for a difference of four columns.

They are stored in a single table with a `type` discriminator and CHECK constraints ensuring flight-only columns stay NULL for ground handling.

## SQL schema

```sql
CREATE EXTENSION IF NOT EXISTS citext;

-- ---------------------------------------------------------------- users

CREATE TABLE users (
    id              uuid        PRIMARY KEY DEFAULT gen_random_uuid(),
    public_id       text        NOT NULL UNIQUE,
    username        citext      NOT NULL UNIQUE,
    email           citext      NOT NULL UNIQUE,
    password_hash   text,                        -- NULL for Google-only accounts
    google_sub      text        UNIQUE,          -- NULL for password-only accounts
    created_at      timestamptz NOT NULL DEFAULT now(),
    CONSTRAINT users_has_credential
        CHECK (password_hash IS NOT NULL OR google_sub IS NOT NULL)
);

-- ------------------------------------------------------------- sessions

CREATE TABLE sessions (
    id           uuid        PRIMARY KEY DEFAULT gen_random_uuid(),
    user_id      uuid        NOT NULL REFERENCES users(id) ON DELETE CASCADE,
    token_hash   text        NOT NULL UNIQUE,    -- SHA-256 of the cookie value
    created_at   timestamptz NOT NULL DEFAULT now(),
    expires_at   timestamptz NOT NULL,
    persistent   boolean     NOT NULL DEFAULT false,
    user_agent   text,
    ip_hash      text
);

CREATE INDEX sessions_user_idx    ON sessions(user_id);
CREATE INDEX sessions_expires_idx ON sessions(expires_at);

-- ------------------------------------------------------------ equipment

CREATE TYPE equipment_type AS ENUM
    ('wing', 'harness', 'reserve', 'helmet', 'radio', 'variometer', 'other');

CREATE TABLE equipment (
    id                 uuid           PRIMARY KEY DEFAULT gen_random_uuid(),
    user_id            uuid           NOT NULL REFERENCES users(id) ON DELETE CASCADE,
    display_name       text           NOT NULL,
    type               equipment_type NOT NULL,
    brand              text,
    model              text,
    purchase_date      date,
    next_revision_date date,
    auto_add           boolean        NOT NULL DEFAULT false,
    retired            boolean        NOT NULL DEFAULT false,
    created_at         timestamptz    NOT NULL DEFAULT now(),
    UNIQUE (user_id, display_name)
);

CREATE INDEX equipment_user_created_idx ON equipment(user_id, created_at);
CREATE INDEX equipment_auto_add_idx     ON equipment(user_id)
    WHERE auto_add = true AND retired = false;

-- Revision history is a list, so it is its own table.
-- "Last revision date" is MAX(revision_date), not a stored column.
CREATE TABLE equipment_revisions (
    id            uuid        PRIMARY KEY DEFAULT gen_random_uuid(),
    equipment_id  uuid        NOT NULL REFERENCES equipment(id) ON DELETE CASCADE,
    revision_date date        NOT NULL,
    comment       text,
    created_at    timestamptz NOT NULL DEFAULT now()
);

CREATE INDEX equipment_revisions_idx
    ON equipment_revisions(equipment_id, revision_date DESC);

-- ----------------------------------------------------------- activities

CREATE TYPE activity_type AS ENUM ('flight', 'ground_handling');
CREATE TYPE track_format  AS ENUM ('gpx', 'igc');

CREATE TABLE activities (
    id                uuid          PRIMARY KEY DEFAULT gen_random_uuid(),
    user_id           uuid          NOT NULL REFERENCES users(id) ON DELETE CASCADE,
    type              activity_type NOT NULL,

    name              text          NOT NULL,
    started_at        timestamptz   NOT NULL,
    ended_at          timestamptz,
    local_date        date          NOT NULL,   -- local calendar date at the site
    local_tz          text,                     -- IANA name, e.g. 'Europe/Paris'

    takeoff_location  text,
    landing_location  text,
    takeoff_lat       double precision,
    takeoff_lon       double precision,

    comment           text,

    -- derived from the track
    max_altitude_m    integer,
    altitude_gain_m   integer,
    distance_km       double precision,

    -- reference to the track file on disk; all NULL when no GPS data
    track_filename    text,
    track_fmt         track_format,
    track_size_bytes  bigint,
    track_sha256      text,
    has_elevation     boolean       NOT NULL DEFAULT false,

    -- ground handling
    wind_speed_kmh    integer,
    wind_direction    integer       CHECK (wind_direction BETWEEN 0 AND 359),

    created_at        timestamptz   NOT NULL DEFAULT now(),
    updated_at        timestamptz   NOT NULL DEFAULT now(),

    duration_seconds  integer GENERATED ALWAYS AS (
        CASE WHEN ended_at IS NOT NULL
             THEN EXTRACT(EPOCH FROM (ended_at - started_at))::integer
        END
    ) STORED,

    CONSTRAINT activities_chronology
        CHECK (ended_at IS NULL OR ended_at > started_at),

    CONSTRAINT activities_track_all_or_none CHECK (
        (track_filename IS NULL AND track_fmt IS NULL
         AND track_size_bytes IS NULL AND track_sha256 IS NULL)
        OR
        (track_filename IS NOT NULL AND track_fmt IS NOT NULL
         AND track_size_bytes IS NOT NULL AND track_sha256 IS NOT NULL)
    ),

    CONSTRAINT activities_ground_handling_has_no_track CHECK (
        type <> 'ground_handling'
        OR (track_filename IS NULL AND distance_km IS NULL
            AND max_altitude_m IS NULL AND altitude_gain_m IS NULL)
    )
);

CREATE INDEX activities_user_started_idx ON activities(user_id, started_at DESC);
CREATE INDEX activities_user_type_idx    ON activities(user_id, type, started_at DESC);

CREATE TABLE activity_equipment (
    activity_id  uuid NOT NULL REFERENCES activities(id) ON DELETE CASCADE,
    equipment_id uuid NOT NULL REFERENCES equipment(id)  ON DELETE RESTRICT,
    PRIMARY KEY (activity_id, equipment_id)
);

CREATE INDEX activity_equipment_eq_idx ON activity_equipment(equipment_id);
```

## Equipment usage query

Backs the Usage section of the equipment detail view.

```sql
SELECT e.id,
       COUNT(ae.activity_id)                                     AS activity_count,
       COALESCE(SUM(a.duration_seconds), 0) / 3600.0             AS hours
FROM equipment e
LEFT JOIN activity_equipment ae ON ae.equipment_id = e.id
LEFT JOIN activities        a  ON a.id = ae.activity_id
WHERE e.id = @equipmentId AND e.user_id = @userId
GROUP BY e.id;
```

The same aggregate, run without the `WHERE e.id` filter, drives the delete-confirmation count.

## Notes on schema choices

**`ON DELETE RESTRICT` on `activity_equipment`.** Deleting a wing used by 80 activities fails rather than erasing history. The UI surfaces this as Retire.

**`local_date` separate from `started_at`.** A flight taking off at 23:40 local time is stored in UTC and may land on the next calendar day. The logbook must group by local date, so it is stored explicitly with the IANA zone.

**`duration_seconds` generated.** Computed by the database, always consistent, directly sortable.

**`activities_track_all_or_none`.** Prevents half-populated track references, the most likely source of viewer crashes.

**`track_sha256`.** Enables duplicate-upload detection and verifies backup integrity.

**Equipment revisions as rows.** The spec asks for a list of revision dates. Storing them as rows makes "when was this last checked" a `MAX`, and leaves room for a per-revision comment.

# Auto-filled fields

Derived in the browser from the uploaded track and sent as ordinary field values the user can override:

- `started_at`, `ended_at` — first and last fix
- `takeoff_lat`, `takeoff_lon` — first fix
- `max_altitude_m`, `altitude_gain_m` — from the altitude series
- `distance_km` — cumulative track distance
- `name` — the file name without extension

For IGC, B-records carry both pressure and GPS altitude. Use pressure altitude for gain and climb rate; use GPS altitude when comparing against terrain. GPS altitude is noisy and must be smoothed before computing vertical speed, or reported climb rates will be absurd.

Sample rates vary between instruments (1 s to 10 s), so never assume a fixed interval when integrating.

# REST API

```
POST   /api/auth/register
POST   /api/auth/login
POST   /api/auth/logout
GET    /api/auth/google            → redirect
GET    /api/auth/google/callback
GET    /api/me

GET    /api/activities?cursor=&limit=50&type=
POST   /api/activities
GET    /api/activities/{id}
PUT    /api/activities/{id}
DELETE /api/activities/{id}
POST   /api/activities/{id}/track          multipart, replaces existing
DELETE /api/activities/{id}/track
GET    /api/activities/{id}/track          streamed, ETag + immutable
GET    /api/activities/{id}/elevation

GET    /api/equipment
POST   /api/equipment
GET    /api/equipment/{id}                 includes usage aggregate
PUT    /api/equipment/{id}
DELETE /api/equipment/{id}                 409 when in use
POST   /api/equipment/{id}/retire

GET    /api/export                         streamed ZIP
```

Every handler scopes its query by the session's `user_id`. Authorization is a `WHERE user_id = @me` on every statement, not a separate check — that way a missed check is a missed row, not a leak.

# Data import/export

`GET /api/export` returns a ZIP:

- `activities.csv` — one line per activity, joined with equipment names
- `equipment.csv`
- `tracks/{activity_id}.{gpx|igc}`

Produced by a single SQL query streamed through `CsvHelper`, zipped with `ZipArchive`, so memory stays flat regardless of logbook size.

# Performance

The viewer may be medium or heavy CPU on the client side. The backend stays lightweight: no track parsing beyond the one-time elevation pass, and track files served as static content with `ETag` and `Cache-Control: immutable` (safe, since tracks are never modified).

Native AOT or a trimmed self-contained build yields a 25–40 MB image running in roughly 30 MB RSS.

# Build

Buildable on a Windows machine.

> Native AOT cannot cross-compile from Windows to Linux. The Linux image must be built inside a Linux container or in the GitHub Action, not directly on a Windows dev machine.

# Run

Runs on Windows and Linux (Docker). Testable without Docker against a local PostgreSQL instance or a Testcontainers-managed one.

# Docker

```yaml
services:
  db:
    image: postgres:17-alpine
    environment:
      POSTGRES_DB: paraglog
      POSTGRES_USER: paraglog
      POSTGRES_PASSWORD_FILE: /run/secrets/db_password
    volumes:
      - pgdata:/var/lib/postgresql/data
    healthcheck:
      test: ["CMD-SHELL", "pg_isready -U paraglog"]
      interval: 10s

  backend:
    image: ghcr.io/<owner>/paraglog-backend:latest
    depends_on:
      db:
        condition: service_healthy
    volumes:
      - blobs:/data/blobs
    user: "1000:1000"

  viewer:
    image: ghcr.io/<owner>/paraglog-viewer:latest

  proxy:
    image: caddy:alpine
    ports: ["80:80", "443:443"]

volumes:
  pgdata:
  blobs:
```

- Backend image: `mcr.microsoft.com/dotnet/sdk:9.0` → `runtime-deps:9.0-alpine`
- Viewer image: `node:22-alpine` → `nginx:alpine`, roughly 25 MB
- Backend runs as a non-root user owning `/data/blobs`

# Continuous integration

`main` holds the last released version, `dev` holds working releases. Every commit to `dev` and `main` is built by GitHub Actions.

The test job starts a PostgreSQL service container and runs migrations against it, so schema changes are verified on every commit.

# Continuous deployment

Images are pushed to GitHub Packages (`ghcr.io`) with `docker/build-push-action`, `cache-from: type=gha`, `permissions: packages: write`.

- `main` → `latest` plus a semver tag
- `dev` → `dev`
