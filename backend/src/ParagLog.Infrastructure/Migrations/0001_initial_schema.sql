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
