DELETE FROM sessions
WHERE id = @SessionId AND user_id = @UserId;
