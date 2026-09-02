SELECT id AS "Id", user_id AS "UserId", type::text AS "Type", name AS "Name",
       started_at AS "StartedAt", ended_at AS "EndedAt", local_date AS "LocalDate", local_tz AS "LocalTz",
       takeoff_location AS "TakeoffLocation", landing_location AS "LandingLocation",
       takeoff_lat AS "TakeoffLat", takeoff_lon AS "TakeoffLon", comment AS "Comment",
       max_altitude_m AS "MaxAltitudeM", altitude_gain_m AS "AltitudeGainM", distance_km AS "DistanceKm",
       track_filename AS "TrackFilename", track_fmt::text AS "TrackFormat",
       track_size_bytes AS "TrackSizeBytes", track_sha256 AS "TrackSha256", has_elevation AS "HasElevation",
       wind_speed_kmh AS "WindSpeedKmh", wind_direction AS "WindDirection",
       created_at AS "CreatedAt", updated_at AS "UpdatedAt", duration_seconds AS "DurationSeconds"
FROM activities
WHERE user_id = @UserId
  AND (@Type::text IS NULL OR type::text = @Type::text)
  AND (@BeforeStartedAt::timestamptz IS NULL
       OR (started_at, id) < (@BeforeStartedAt::timestamptz, @BeforeId::uuid))
ORDER BY started_at DESC, id DESC
LIMIT @Limit;
