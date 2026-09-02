UPDATE activities
SET track_filename = @Filename,
    track_fmt = @Format::track_format,
    track_size_bytes = @SizeBytes,
    track_sha256 = @Sha256,
    updated_at = @UpdatedAt
WHERE id = @Id AND user_id = @UserId;
