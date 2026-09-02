UPDATE sessions
SET expires_at = @ExpiresAt
WHERE id = @SessionId AND user_id = @UserId;
