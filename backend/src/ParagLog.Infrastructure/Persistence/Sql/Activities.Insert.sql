INSERT INTO activities (
    id, user_id, type, name, started_at, ended_at, local_date, local_tz,
    takeoff_location, landing_location, takeoff_lat, takeoff_lon, comment,
    max_altitude_m, altitude_gain_m, distance_km,
    wind_speed_kmh, wind_direction, created_at, updated_at
) VALUES (
    @Id, @UserId, @Type::activity_type, @Name, @StartedAt, @EndedAt, @LocalDate, @LocalTz,
    @TakeoffLocation, @LandingLocation, @TakeoffLat, @TakeoffLon, @Comment,
    @MaxAltitudeM, @AltitudeGainM, @DistanceKm,
    @WindSpeedKmh, @WindDirection, @CreatedAt, @UpdatedAt
);
