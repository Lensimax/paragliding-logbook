INSERT INTO sessions (id, user_id, token_hash, created_at, expires_at, persistent, user_agent, ip_hash)
VALUES (@Id, @UserId, @TokenHash, @CreatedAt, @ExpiresAt, @Persistent, @UserAgent, @IpHash);
