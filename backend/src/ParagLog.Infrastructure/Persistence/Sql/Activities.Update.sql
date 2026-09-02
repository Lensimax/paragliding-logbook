UPDATE activities
SET type = @Type::activity_type,
    name = @Name,
    started_at = @StartedAt,
    ended_at = @EndedAt,
    local_date = @LocalDate,
    local_tz = @LocalTz,
    takeoff_location = @TakeoffLocation,
    landing_location = @LandingLocation,
    comment = @Comment,
    wind_speed_kmh = @WindSpeedKmh,
    wind_direction = @WindDirection,
    updated_at = @UpdatedAt
WHERE id = @Id AND user_id = @UserId;
