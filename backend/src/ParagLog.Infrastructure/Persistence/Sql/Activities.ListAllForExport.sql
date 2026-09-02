SELECT a.id AS "Id", a.user_id AS "UserId", a.type::text AS "Type", a.name AS "Name",
       a.started_at AS "StartedAt", a.ended_at AS "EndedAt", a.local_date AS "LocalDate", a.local_tz AS "LocalTz",
       a.takeoff_location AS "TakeoffLocation", a.landing_location AS "LandingLocation",
       a.takeoff_lat AS "TakeoffLat", a.takeoff_lon AS "TakeoffLon", a.comment AS "Comment",
       a.max_altitude_m AS "MaxAltitudeM", a.altitude_gain_m AS "AltitudeGainM", a.distance_km AS "DistanceKm",
       a.track_filename AS "TrackFilename", a.track_fmt::text AS "TrackFormat",
       a.track_size_bytes AS "TrackSizeBytes", a.track_sha256 AS "TrackSha256", a.has_elevation AS "HasElevation",
       a.wind_speed_kmh AS "WindSpeedKmh", a.wind_direction AS "WindDirection",
       a.created_at AS "CreatedAt", a.updated_at AS "UpdatedAt", a.duration_seconds AS "DurationSeconds",
       COALESCE(
           (SELECT string_agg(e.display_name, '; ' ORDER BY e.display_name)
            FROM activity_equipment ae
            JOIN equipment e ON e.id = ae.equipment_id
            WHERE ae.activity_id = a.id),
           ''
       ) AS "EquipmentNames"
FROM activities a
WHERE a.user_id = @UserId
ORDER BY a.started_at ASC, a.id ASC;
