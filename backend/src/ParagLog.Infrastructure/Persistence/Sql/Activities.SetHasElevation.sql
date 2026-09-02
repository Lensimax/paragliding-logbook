UPDATE activities
SET has_elevation = @HasElevation,
    updated_at = @UpdatedAt
WHERE id = @Id AND user_id = @UserId;
