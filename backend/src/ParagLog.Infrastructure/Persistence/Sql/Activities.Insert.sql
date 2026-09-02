INSERT INTO activities (
    id, user_id, type, name, started_at, ended_at, local_date, local_tz,
    takeoff_location, landing_location, comment, wind_speed_kmh, wind_direction,
    created_at, updated_at
) VALUES (
    @Id, @UserId, @Type::activity_type, @Name, @StartedAt, @EndedAt, @LocalDate, @LocalTz,
    @TakeoffLocation, @LandingLocation, @Comment, @WindSpeedKmh, @WindDirection,
    @CreatedAt, @UpdatedAt
);
